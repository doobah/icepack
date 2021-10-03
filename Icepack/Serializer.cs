using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Linq.Expressions;
using System.IO;

namespace Icepack
{
    /// <summary> Serializes/deserializes objects. </summary>
    public class Serializer
    {
        /// <summary>
        /// Serializers with the same compatibility version are guaranteed to be interoperable.
        /// </summary>
        public const ushort CompatibilityVersion = 2;

        private readonly TypeRegistry typeRegistry;

        /// <summary> Creates a new serializer. </summary>
        public Serializer()
        {
            typeRegistry = new TypeRegistry();

            // Pre-register all 'basic' types, along with the Type type.
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
        /// <returns> The serialized object graph. </returns>
        public void Serialize(object rootObj, Stream outputStream)
        {
            var objectDataStream = new MemoryStream();
            var objectDataWriter = new BinaryWriter(objectDataStream, Encoding.Unicode, true);
            var context = new SerializationContext(typeRegistry);

            context.RegisterObject(rootObj);

            int currentObjId = 0;
            while (currentObjId < context.ObjectsInOrder.Count)
            {
                ObjectMetadata objToSerialize = context.ObjectsInOrder[currentObjId];
                SerializationOperationFactory.SerializeReferenceType(objToSerialize, context, objectDataWriter);

                currentObjId++;
            }

            var metadataWriter = new BinaryWriter(outputStream, Encoding.Unicode, true);
            metadataWriter.Write(CompatibilityVersion);
            SerializeTypeMetadata(context, metadataWriter);
            SerializeObjectMetadata(context, metadataWriter);

            objectDataStream.Position = 0;
            objectDataStream.CopyTo(outputStream);

            objectDataStream.Close();
            objectDataWriter.Close();
            metadataWriter.Close();
        }

        private static void SerializeObjectMetadata(SerializationContext context, BinaryWriter writer)
        {
            writer.Write(context.Objects.Count);

            for (int objectIdx = 0; objectIdx < context.ObjectsInOrder.Count; objectIdx++)
            {
                ObjectMetadata objectMetadata = context.ObjectsInOrder[objectIdx];
                TypeMetadata typeMetadata = objectMetadata.Type;

                writer.Write(typeMetadata.Id);

                switch (typeMetadata.CategoryId)
                {
                    case TypeCategory.Basic:
                        typeMetadata.SerializeBasic(objectMetadata.Value, context, writer);
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
                        typeMetadata.EnumUnderlyingTypeMetadata.SerializeBasic(objectMetadata.Value, context, writer);
                        break;
                    case TypeCategory.Type:
                        writer.Write(context.GetTypeMetadata((Type)objectMetadata.Value).Id);
                        break;
                    default:
                        throw new IcepackException($"Invalid category ID: {objectMetadata.Type.CategoryId}");
                }
            }
        }

        private static void SerializeTypeMetadata(SerializationContext context, BinaryWriter writer)
        {
            writer.Write(context.Types.Count);
            for (int typeIdx = 0; typeIdx < context.TypesInOrder.Count; typeIdx++)
            {
                TypeMetadata typeMetadata = context.TypesInOrder[typeIdx];
                writer.Write(typeMetadata.Type.AssemblyQualifiedName);
                writer.Write((byte)typeMetadata.CategoryId);
                switch (typeMetadata.CategoryId)
                {
                    case TypeCategory.Basic:
                        break;
                    case TypeCategory.Array:
                    case TypeCategory.List:
                        writer.Write(typeMetadata.ItemSize);
                        break;
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
                        throw new IcepackException($"Invalid category ID: {typeMetadata.CategoryId}");
                }
            }
        }

        public T Deserialize<T>(Stream inputStream)
        {
            // Initialize

            inputStream.Position = 0;

            var context = new DeserializationContext(inputStream);

            ushort compatibilityVersion = context.Reader.ReadUInt16();
            if (compatibilityVersion != CompatibilityVersion)
                throw new IcepackException($"Expected compatibility version {CompatibilityVersion}, received {compatibilityVersion}");

            DeserializeTypeMetadata(context);
            DeserializeObjectMetadata(context);

            // Deserialize objects

            for (int i = 0; i < context.Objects.Length; i++)
            {
                ObjectMetadata classObjMetadata = context.Objects[i];
                DeserializationOperationFactory.DeserializeReferenceType(classObjMetadata, context);
            }

            T rootObject = (T)context.Objects[0].Value;

            // Clean up

            context.Dispose();

            return rootObject;
        }

        private static void DeserializeObjectMetadata(DeserializationContext context)
        {
            int numberOfObjects = context.Reader.ReadInt32();

            context.Objects = new ObjectMetadata[numberOfObjects];

            for (int i = 0; i < numberOfObjects; i++)
            {
                uint typeId = context.Reader.ReadUInt32();
                TypeMetadata objectTypeMetadata = context.Types[typeId - 1];
                Type objectType = objectTypeMetadata.Type;
                int length = 0;

                object obj;
                switch (objectTypeMetadata.CategoryId)
                {
                    case TypeCategory.Basic:
                        obj = objectTypeMetadata.DeserializeBasic(context);
                        break;
                    case TypeCategory.Array:
                        {
                            length = context.Reader.ReadInt32();
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
                            length = context.Reader.ReadInt32();
                            if (objectType == null)
                                obj = null;
                            else
                                obj = Activator.CreateInstance(objectType, length);
                            break;
                        }
                    case TypeCategory.Struct:
                        if (objectType == null)
                            obj = null;
                        else
                            obj = Activator.CreateInstance(objectType);
                        break;
                    case TypeCategory.Class:
                        if (objectType == null)
                            obj = null;
                        else
                            obj = Activator.CreateInstance(objectType);
                        break;
                    case TypeCategory.Enum:
                        object underlyingValue = objectTypeMetadata.EnumUnderlyingTypeMetadata.DeserializeBasic(context);
                        if (objectType == null)
                            obj = null;
                        else
                            obj = Enum.ToObject(objectType, underlyingValue);
                        break;
                    case TypeCategory.Type:
                        uint value = context.Reader.ReadUInt32();
                        obj = context.Types[value - 1].Type;
                        break;
                    default:
                        throw new IcepackException($"Invalid category ID: {objectTypeMetadata.CategoryId}");
                }

                context.Objects[i] = new ObjectMetadata((uint)i + 1, objectTypeMetadata, length, obj);
            }
        }

        private void DeserializeTypeMetadata(DeserializationContext context)
        {
            int numberOfTypes = context.Reader.ReadInt32();
            context.Types = new TypeMetadata[numberOfTypes];

            for (int t = 0; t < numberOfTypes; t++)
            {
                string typeName = context.Reader.ReadString();
                TypeMetadata registeredTypeMetadata = typeRegistry.GetTypeMetadata(typeName);

                int itemSize = 0;
                int keySize = 0;
                int instanceSize = 0;
                bool hasParent = false;
                List<string> fieldNames = null;
                List<int> fieldSizes = null;
                TypeMetadata enumUnderlyingTypeMetadata = null;

                TypeCategory categoryId = (TypeCategory)context.Reader.ReadByte();
                switch (categoryId)
                {
                    case TypeCategory.Basic:
                        break;
                    case TypeCategory.Array:
                    case TypeCategory.List:
                    case TypeCategory.HashSet:
                        itemSize = context.Reader.ReadInt32();
                        break;
                    case TypeCategory.Dictionary:
                        keySize = context.Reader.ReadInt32();
                        itemSize = context.Reader.ReadInt32();
                        break;
                    case TypeCategory.Struct:
                        {
                            instanceSize = context.Reader.ReadInt32();

                            int numberOfFields = context.Reader.ReadInt32();
                            fieldNames = new List<string>(numberOfFields);
                            fieldSizes = new List<int>(numberOfFields);
                            for (int f = 0; f < numberOfFields; f++)
                            {
                                fieldNames.Add(context.Reader.ReadString());
                                fieldSizes.Add(context.Reader.ReadInt32());
                            }
                            break;
                        }
                    case TypeCategory.Class:
                        {
                            instanceSize = context.Reader.ReadInt32();
                            hasParent = context.Reader.ReadBoolean();

                            int numberOfFields = context.Reader.ReadInt32();
                            fieldNames = new List<string>(numberOfFields);
                            fieldSizes = new List<int>(numberOfFields);
                            for (int f = 0; f < numberOfFields; f++)
                            {
                                fieldNames.Add(context.Reader.ReadString());
                                fieldSizes.Add(context.Reader.ReadInt32());
                            }
                            break;
                        }
                    case TypeCategory.Enum:
                        uint underlyingTypeId = context.Reader.ReadUInt32();
                        enumUnderlyingTypeMetadata = context.Types[underlyingTypeId - 1];
                        break;
                    case TypeCategory.Type:
                        break;
                    default:
                        throw new IcepackException($"Invalid category ID: {categoryId}");
                }

                var typeMetadata = new TypeMetadata(registeredTypeMetadata, fieldNames, fieldSizes, (uint)(t + 1),
                    hasParent, categoryId, itemSize, keySize, instanceSize, enumUnderlyingTypeMetadata);
                context.Types[t] = typeMetadata;
            }
        }
    }
}
