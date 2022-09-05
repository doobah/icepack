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
    public sealed class Serializer
    {
        /// <summary> Serializers with the same compatibility version are guaranteed to be interoperable. </summary>
        public const ushort CompatibilityVersion = 5;

        /// <summary> Keeps track of type information. </summary>
        private readonly TypeRegistry typeRegistry;

        /// <summary> Settings for the serializer. </summary>
        private readonly SerializerSettings settings;

        /// <summary> Creates a new serializer with default settings. </summary>
        public Serializer() : this(new SerializerSettings()) { }

        /// <summary> Creates a new serializer. </summary>
        public Serializer(SerializerSettings settings)
        {
            if (settings == null)
                settings = new SerializerSettings();

            typeRegistry = new TypeRegistry(settings);
            this.settings = settings;

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
            if (Utils.IsUnsupportedType(type))
                throw new IcepackException($"Unsupported type: {type}");

            typeRegistry.GetTypeMetadata(type, true);
        }

        /// <summary> Serializes an object graph to a stream. </summary>
        /// <param name="rootObject"> The root object to be serialized. </param>
        /// <param name="outputStream"> The stream to output the serialized data to. </param>
        public void Serialize(object? rootObject, Stream outputStream)
        {
            var context = new SerializationContext(typeRegistry, settings);

            context.RegisterObject(rootObject);

            using (var objectDataStream = new MemoryStream())
            {
                using (var objectDataWriter = new BinaryWriter(objectDataStream, Encoding.Unicode, true))
                {
                    // Each time an object reference is encountered, a new object will be added to the list.
                    // Iterate through the growing list until there are no more objects to serialize.
                    int currentObjId = 0;
                    while (currentObjId < context.ObjectsInOrder.Count)
                    {
                        context.ObjectsInOrder[currentObjId].SerializeValue(context, objectDataWriter);
                        currentObjId++;
                    }
                }

                using (var metadataWriter = new BinaryWriter(outputStream, Encoding.Unicode, true))
                {
                    metadataWriter.Write(CompatibilityVersion);

                    // Serialize type metadata
                    metadataWriter.Write(context.Types.Count);
                    for (int typeIdx = 0; typeIdx < context.TypesInOrder.Count; typeIdx++)
                        context.TypesInOrder[typeIdx].SerializeMetadata(context, metadataWriter);

                    // Serialize object metadata
                    metadataWriter.Write(context.ObjectsInOrder.Count);
                    for (int objectIdx = 0; objectIdx < context.ObjectsInOrder.Count; objectIdx++)
                        context.ObjectsInOrder[objectIdx].SerializeMetadata(context, metadataWriter);
                }

                // The object data needs to go after the metadata in the output stream
                objectDataStream.Position = 0;
                objectDataStream.CopyTo(outputStream);
            }
        }

        /// <summary> Deserializes a data stream as an object of the specified type. </summary>
        /// <typeparam name="T"> The type of the object being deserialized. </typeparam>
        /// <param name="inputStream"> The stream containing the data to deserialize. </param>
        /// <returns> The deserialized object. </returns>
        public T? Deserialize<T>(Stream inputStream)
        {
            using (var reader = new BinaryReader(inputStream, Encoding.Unicode, true))
            {
                ushort compatibilityVersion = reader.ReadUInt16();
                if (compatibilityVersion != CompatibilityVersion)
                    throw new IcepackException($"Expected compatibility version {CompatibilityVersion}, received {compatibilityVersion}");

                var context = new DeserializationContext();

                DeserializeTypeMetadata(context, reader);
                DeserializeObjectMetadata(context, reader);

                for (int i = 0; i < context.Objects!.Length; i++)
                {
                    ObjectMetadata classObjMetadata = context.Objects[i];
                    classObjMetadata.TypeMetadata.DeserializeReferenceType!(classObjMetadata, context, reader);
                }

                if (context.Objects.Length == 0)
                    return default(T);
                else
                {
                    object? rootObj = context.Objects[0].Value;
                    if (rootObj == null)
                        return default(T);
                    else
                        return (T)rootObj;
                }
            }
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
                TypeMetadata objectTypeMetadata = context.Types![typeId - 1];
                Type? objectType = objectTypeMetadata.Type;
                int length = 0;

                object? obj;
                switch (objectTypeMetadata.Category)
                {
                    case TypeCategory.Immutable:
                        obj = objectTypeMetadata.DeserializeImmutable!(context, reader);
                        break;
                    case TypeCategory.Array:
                        {
                            length = reader.ReadInt32();

                            if (objectType == null)
                                obj = null;
                            else
                            {
                                Type elementType = objectType.GetElementType()!;
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
                                obj = objectTypeMetadata.CreateCollection!(length);
                            break;
                        }
                    case TypeCategory.Struct:
                    case TypeCategory.Class:
                        if (objectType == null)
                            obj = null;
                        else
                            obj = objectTypeMetadata.CreateClassOrStruct!();
                        break;
                    case TypeCategory.Enum:
                        // Need to deserialize regardless of whether the type still exists, to advance the stream past the value serialized in metadata.
                        object underlyingValue = objectTypeMetadata.EnumUnderlyingTypeMetadata!.DeserializeImmutable!(context, reader)!;

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

                context.Objects[i] = new ObjectMetadata((uint)i + 1, objectTypeMetadata, length, obj, 0);
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
                TypeMetadata? registeredTypeMetadata = typeRegistry.GetTypeMetadata(typeName);
                int itemSize = 0;
                int keySize = 0;
                int instanceSize = 0;
                TypeMetadata? parentTypeMetadata = null;
                List<string>? fieldNames = null;
                List<int>? fieldSizes = null;
                TypeMetadata? enumUnderlyingTypeMetadata = null;

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
                            
                            uint parentTypeId = reader.ReadUInt32();
                            if (parentTypeId != 0)
                                parentTypeMetadata = context.Types[parentTypeId - 1];

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
                    parentTypeMetadata, category, itemSize, keySize, instanceSize, enumUnderlyingTypeMetadata);
                context.Types[t] = typeMetadata;
            }
        }
    }
}
