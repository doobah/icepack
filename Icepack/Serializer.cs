using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.IO;

namespace Icepack
{
    /// <summary> Serializes/deserializes objects. </summary>
    public class Serializer
    {
        /// <summary> Serializers with the same compatibility version are guaranteed to be interoperable. </summary>
        public const ushort CompatibilityVersion = 3;

        private readonly TypeRegistry typeRegistry;

        /// <summary> Creates a new serializer. </summary>
        public Serializer()
        {
            typeRegistry = new TypeRegistry();

            // Pre-register all immutable types, along with the Type type.
            RegisterType(typeof(string));
            RegisterType(typeof(byte));
            RegisterType(typeof(sbyte));
            RegisterType(typeof(char));
            RegisterType(typeof(bool));
            RegisterType(typeof(int));
            RegisterType(typeof(uint));
            RegisterType(typeof(short));
            RegisterType(typeof(ushort));
            RegisterType(typeof(long));
            RegisterType(typeof(ulong));
            RegisterType(typeof(decimal));
            RegisterType(typeof(float));
            RegisterType(typeof(double));
            RegisterType(typeof(Type));
        }

        /// <summary> Registers a type as serializable. </summary>
        /// <param name="type"> The type to register. </param>
        public void RegisterType(Type type)
        {
            typeRegistry.RegisterType(type);
        }

        /// <summary> Serializes an object graph as a string. </summary>
        /// <param name="rootObj"> The root object to be serialized. </param>
        /// <param name="outputStream"> The stream to output the serialized data to. </param>
        public void Serialize(object rootObj, Stream outputStream)
        {
            var objectDataStream = new MemoryStream();
            var objectDataWriter = new BinaryWriter(objectDataStream, Encoding.Unicode, true);
            var context = new SerializationContext(typeRegistry);

            context.RegisterObject(rootObj);

            // Each time an object reference is encountered, a new object will be added to the list.
            // Iterate through the growing list until there are no more objects to serialize.
            int currentObjId = 0;
            while (currentObjId < context.ObjectsInOrder.Count)
            {
                ObjectMetadata objToSerialize = context.ObjectsInOrder[currentObjId];
                TypeMetadata typeMetadata = objToSerialize.Type;
                typeMetadata.SerializeReferenceType(objToSerialize, context, objectDataWriter);

                currentObjId++;
            }

            var metadataWriter = new BinaryWriter(outputStream, Encoding.Unicode, true);
            metadataWriter.Write(CompatibilityVersion);
            SerializeTypeMetadata(context, metadataWriter);
            SerializeObjectMetadata(context, metadataWriter);

            // The object data needs to go after the metadata in the output stream
            objectDataStream.Position = 0;
            objectDataStream.CopyTo(outputStream);

            objectDataStream.Close();
            objectDataWriter.Close();
            metadataWriter.Close();
        }

        /// <summary> Serializes the object metadata. </summary>
        /// <param name="context"> The current serialization context. </param>
        /// <param name="writer"> Writes the metadata to a stream. </param>
        private static void SerializeObjectMetadata(SerializationContext context, BinaryWriter writer)
        {
            writer.Write(context.Objects.Count);

            for (int objectIdx = 0; objectIdx < context.ObjectsInOrder.Count; objectIdx++)
            {
                ObjectMetadata objectMetadata = context.ObjectsInOrder[objectIdx];
                TypeMetadata typeMetadata = objectMetadata.Type;

                writer.Write(typeMetadata.Id);

                switch (typeMetadata.Category)
                {
                    case TypeCategory.Immutable:
                        // "Boxed" immutable values are serialized entirely as metadata since they are unable to be
                        // pre-instantiated and updated later like mutable structs and classes, and the final value
                        // must be present when resolving references to these values.
                        typeMetadata.SerializeImmutable(objectMetadata.Value, context, writer);
                        break;
                    case TypeCategory.Array:
                    case TypeCategory.List:
                    case TypeCategory.HashSet:
                    case TypeCategory.Dictionary:
                        writer.Write(objectMetadata.Length);
                        break;
                    case TypeCategory.Struct:
                    case TypeCategory.Class:
                        break;
                    case TypeCategory.Enum:
                        typeMetadata.EnumUnderlyingTypeMetadata.SerializeImmutable(objectMetadata.Value, context, writer);
                        break;
                    case TypeCategory.Type:
                        // Type objects are serialized as an ID of a registered type, as metadata so that references
                        // to a type object can be immediately resolved to a type.
                        TypeMetadata valueTypeMetadata = context.GetTypeMetadata((Type)objectMetadata.Value);
                        writer.Write(valueTypeMetadata.Id);
                        break;
                    default:
                        throw new IcepackException($"Invalid type category: {objectMetadata.Type.Category}");
                }
            }
        }

        /// <summary> Serializes the type metadata. </summary>
        /// <param name="context"> The current serialization context. </param>
        /// <param name="writer"> Writes the metadata to a stream. </param>
        private static void SerializeTypeMetadata(SerializationContext context, BinaryWriter writer)
        {
            writer.Write(context.Types.Count);

            for (int typeIdx = 0; typeIdx < context.TypesInOrder.Count; typeIdx++)
            {
                TypeMetadata typeMetadata = context.TypesInOrder[typeIdx];

                writer.Write(typeMetadata.Type.AssemblyQualifiedName);
                writer.Write((byte)typeMetadata.Category);
                
                switch (typeMetadata.Category)
                {
                    case TypeCategory.Immutable:
                        break;
                    case TypeCategory.Array:
                    case TypeCategory.List:
                    case TypeCategory.HashSet:
                        writer.Write(typeMetadata.ItemSize);
                        break;
                    case TypeCategory.Dictionary:
                        writer.Write(typeMetadata.KeySize);
                        writer.Write(typeMetadata.ItemSize);
                        break;
                    case TypeCategory.Struct:
                        writer.Write(typeMetadata.InstanceSize);
                        writer.Write(typeMetadata.Fields.Count);

                        for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields.Count; fieldIdx++)
                        {
                            FieldMetadata fieldMetadata = typeMetadata.Fields[fieldIdx];

                            writer.Write(fieldMetadata.FieldInfo.Name);
                            writer.Write(fieldMetadata.Size);
                        }
                        break;
                    case TypeCategory.Class:
                        writer.Write(typeMetadata.InstanceSize);
                        writer.Write(typeMetadata.HasParent);
                        writer.Write(typeMetadata.Fields.Count);

                        for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields.Count; fieldIdx++)
                        {
                            FieldMetadata fieldMetadata = typeMetadata.Fields[fieldIdx];

                            writer.Write(fieldMetadata.FieldInfo.Name);
                            writer.Write(fieldMetadata.Size);
                        }
                        break;
                    case TypeCategory.Enum:
                        writer.Write(typeMetadata.EnumUnderlyingTypeMetadata.Id);
                        break;
                    case TypeCategory.Type:
                        break;
                    default:
                        throw new IcepackException($"Invalid category ID: {typeMetadata.Category}");
                }
            }
        }

        /// <summary> Deserializes a data stream as an object of the specified type. </summary>
        /// <typeparam name="T"> The type of the object being deserialized. </typeparam>
        /// <param name="inputStream"> The stream containing the data to deserialize. </param>
        /// <returns> The deserialized object. </returns>
        public T Deserialize<T>(Stream inputStream)
        {
            var reader = new BinaryReader(inputStream, Encoding.Unicode, true);
            var context = new DeserializationContext();

            ushort compatibilityVersion = reader.ReadUInt16();

            if (compatibilityVersion != CompatibilityVersion)
                throw new IcepackException($"Expected compatibility version {CompatibilityVersion}, received {compatibilityVersion}");

            DeserializeTypeMetadata(context, reader);
            DeserializeObjectMetadata(context, reader);

            for (int i = 0; i < context.Objects.Length; i++)
            {
                ObjectMetadata classObjMetadata = context.Objects[i];
                TypeMetadata typeMetadata = classObjMetadata.Type;
                typeMetadata.DeserializeReferenceType(classObjMetadata, context, reader);
            }

            T rootObject = (T)context.Objects[0].Value;

            reader.Dispose();

            return rootObject;
        }

        /// <summary> Deserializes the object metadata. </summary>
        /// <param name="context"> The current deserialization context. </param>
        /// <param name="reader"> Reads the metadata from the stream. </param>
        private static void DeserializeObjectMetadata(DeserializationContext context, BinaryReader reader)
        {
            int numberOfObjects = reader.ReadInt32();
            context.Objects = new ObjectMetadata[numberOfObjects];

            for (int i = 0; i < numberOfObjects; i++)
            {
                uint typeId = reader.ReadUInt32();
                TypeMetadata objectTypeMetadata = context.Types[typeId - 1];
                Type objectType = objectTypeMetadata.Type;
                int length = 0;

                object obj;
                switch (objectTypeMetadata.Category)
                {
                    case TypeCategory.Immutable:
                        obj = objectTypeMetadata.DeserializeImmutable(context, reader);
                        break;
                    case TypeCategory.Array:
                        {
                            length = reader.ReadInt32();

                            if (objectType == null)
                                obj = null;
                            else
                            {
                                Type elementType = objectType.GetElementType();
                                obj = Array.CreateInstance(elementType, length);
                            }
                            break;
                        }
                    case TypeCategory.List:
                    case TypeCategory.HashSet:
                    case TypeCategory.Dictionary:
                        {
                            length = reader.ReadInt32();

                            if (objectType == null)
                                obj = null;
                            else
                                obj = Activator.CreateInstance(objectType, length);
                            break;
                        }
                    case TypeCategory.Struct:
                    case TypeCategory.Class:
                        if (objectType == null)
                            obj = null;
                        else
                            obj = Activator.CreateInstance(objectType);
                        break;
                    case TypeCategory.Enum:
                        object underlyingValue = objectTypeMetadata.EnumUnderlyingTypeMetadata.DeserializeImmutable(context, reader);
                        if (objectType == null)
                            obj = null;
                        else
                            obj = Enum.ToObject(objectType, underlyingValue);
                        break;
                    case TypeCategory.Type:
                        uint value = reader.ReadUInt32();
                        obj = context.Types[value - 1].Type;
                        break;
                    default:
                        throw new IcepackException($"Invalid category: {objectTypeMetadata.Category}");
                }

                context.Objects[i] = new ObjectMetadata((uint)i + 1, objectTypeMetadata, length, obj);
            }
        }

        /// <summary> Deserializes the type metadata. </summary>
        /// <param name="context"> The current deserialization context. </param>
        /// <param name="reader"> Reads the metadata from the stream. </param>
        private void DeserializeTypeMetadata(DeserializationContext context, BinaryReader reader)
        {
            int numberOfTypes = reader.ReadInt32();
            context.Types = new TypeMetadata[numberOfTypes];

            for (int t = 0; t < numberOfTypes; t++)
            {
                string typeName = reader.ReadString();
                TypeMetadata registeredTypeMetadata = typeRegistry.GetTypeMetadata(typeName);
                int itemSize = 0;
                int keySize = 0;
                int instanceSize = 0;
                bool hasParent = false;
                List<string> fieldNames = null;
                List<int> fieldSizes = null;
                TypeMetadata enumUnderlyingTypeMetadata = null;

                TypeCategory category = (TypeCategory)reader.ReadByte();
                switch (category)
                {
                    case TypeCategory.Immutable:
                        break;
                    case TypeCategory.Array:
                    case TypeCategory.List:
                    case TypeCategory.HashSet:
                        itemSize = reader.ReadInt32();
                        break;
                    case TypeCategory.Dictionary:
                        keySize = reader.ReadInt32();
                        itemSize = reader.ReadInt32();
                        break;
                    case TypeCategory.Struct:
                        {
                            instanceSize = reader.ReadInt32();

                            int numberOfFields = reader.ReadInt32();
                            fieldNames = new List<string>(numberOfFields);
                            fieldSizes = new List<int>(numberOfFields);
                            for (int f = 0; f < numberOfFields; f++)
                            {
                                fieldNames.Add(reader.ReadString());
                                fieldSizes.Add(reader.ReadInt32());
                            }
                            break;
                        }
                    case TypeCategory.Class:
                        {
                            instanceSize = reader.ReadInt32();
                            hasParent = reader.ReadBoolean();

                            int numberOfFields = reader.ReadInt32();
                            fieldNames = new List<string>(numberOfFields);
                            fieldSizes = new List<int>(numberOfFields);
                            for (int f = 0; f < numberOfFields; f++)
                            {
                                fieldNames.Add(reader.ReadString());
                                fieldSizes.Add(reader.ReadInt32());
                            }
                            break;
                        }
                    case TypeCategory.Enum:
                        uint underlyingTypeId = reader.ReadUInt32();
                        enumUnderlyingTypeMetadata = context.Types[underlyingTypeId - 1];
                        break;
                    case TypeCategory.Type:
                        break;
                    default:
                        throw new IcepackException($"Invalid category: {category}");
                }

                var typeMetadata = new TypeMetadata(registeredTypeMetadata, fieldNames, fieldSizes, (uint)(t + 1),
                    hasParent, category, itemSize, keySize, instanceSize, enumUnderlyingTypeMetadata);
                context.Types[t] = typeMetadata;
            }
        }
    }
}
