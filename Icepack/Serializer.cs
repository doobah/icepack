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
    /// <summary> Contains methods for serializing/deserializing objects using the Icepack format. </summary>
    public class Serializer
    {
        public const ushort CompatibilityVersion = 1;

        private readonly TypeRegistry typeRegistry;

        public Serializer()
        {
            typeRegistry = new TypeRegistry();

            // Pre-register all 'basic' types
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
            // Initialize

            var objectStream = new MemoryStream();
            var writer = new BinaryWriter(outputStream, Encoding.Unicode, true);
            var context = new SerializationContext(typeRegistry, objectStream);

            writer.Write(CompatibilityVersion);

            // Serialize objects

            context.RegisterObject(rootObj);
            SerializationOperationFactory.SerializeReferenceType(rootObj, context);
            while (context.ObjectsToSerialize.Count > 0)
            {
                object objToSerialize = context.ObjectsToSerialize.Dequeue();
                SerializationOperationFactory.SerializeReferenceType(objToSerialize, context);
            }

            SerializeTypeMetadata(writer, context);
            SerializeObjectMetadata(writer, context);

            objectStream.Position = 0;
            objectStream.CopyTo(outputStream);

            // Clean up

            context.Dispose();
            objectStream.Close();
            writer.Close();
        }

        private static void SerializeObjectMetadata(BinaryWriter writer, SerializationContext context)
        {
            writer.Write(context.Objects.Count);

            for (int objectIdx = 0; objectIdx < context.ObjectsInOrder.Count; objectIdx++)
            {
                ObjectMetadata objectMetadata = context.ObjectsInOrder[objectIdx];
                writer.Write(objectMetadata.Type.Id);

                switch (objectMetadata.Type.CategoryId)
                {
                    case Category.Basic:
                        objectMetadata.Type.SerializeBasic(objectMetadata.Value, writer, context);
                        break;
                    case Category.Array:
                    case Category.List:
                    case Category.HashSet:
                    case Category.Dictionary:
                        writer.Write(objectMetadata.Length);
                        break;
                    case Category.Struct:
                    case Category.Class:
                        break;
                    case Category.Enum:
                        objectMetadata.Type.EnumUnderlyingTypeMetadata.SerializeBasic(objectMetadata.Value, writer, context);
                        break;
                    case Category.Type:
                        writer.Write(context.GetTypeMetadata((Type)objectMetadata.Value).Id);
                        break;
                    default:
                        throw new IcepackException($"Invalid category ID: {objectMetadata.Type.CategoryId}");
                }
            }
        }

        private static void SerializeTypeMetadata(BinaryWriter writer, SerializationContext context)
        {
            writer.Write(context.Types.Count);
            for (int typeIdx = 0; typeIdx < context.TypesInOrder.Count; typeIdx++)
            {
                TypeMetadata typeMetadata = context.TypesInOrder[typeIdx];
                writer.Write(typeMetadata.Type.AssemblyQualifiedName);
                writer.Write((byte)typeMetadata.CategoryId);
                switch (typeMetadata.CategoryId)
                {
                    case Category.Basic:
                        break;
                    case Category.Array:
                    case Category.List:
                        writer.Write(typeMetadata.ItemSize);
                        break;
                    case Category.HashSet:
                        writer.Write(typeMetadata.ItemSize);
                        break;
                    case Category.Dictionary:
                        writer.Write(typeMetadata.KeySize);
                        writer.Write(typeMetadata.ItemSize);
                        break;
                    case Category.Struct:
                        writer.Write(typeMetadata.InstanceSize);
                        writer.Write(typeMetadata.Fields.Count);
                        for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields.Count; fieldIdx++)
                        {
                            FieldMetadata fieldMetadata = typeMetadata.Fields[fieldIdx];
                            writer.Write(fieldMetadata.FieldInfo.Name);
                            writer.Write(fieldMetadata.Size);
                        }
                        break;
                    case Category.Class:
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
                    case Category.Enum:
                        writer.Write(typeMetadata.EnumUnderlyingTypeMetadata.Id);
                        break;
                    case Category.Type:
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
                    case Category.Basic:
                        obj = objectTypeMetadata.DeserializeBasic(context);
                        break;
                    case Category.Array:
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
                    case Category.List:
                    case Category.HashSet:
                    case Category.Dictionary:
                        {
                            length = context.Reader.ReadInt32();
                            if (objectType == null)
                                obj = null;
                            else
                                obj = Activator.CreateInstance(objectType, length);
                            break;
                        }
                    case Category.Struct:
                        if (objectType == null)
                            obj = null;
                        else
                            obj = Activator.CreateInstance(objectType);
                        break;
                    case Category.Class:
                        if (objectType == null)
                            obj = null;
                        else
                            obj = Activator.CreateInstance(objectType);
                        break;
                    case Category.Enum:
                        object underlyingValue = objectTypeMetadata.EnumUnderlyingTypeMetadata.DeserializeBasic(context);
                        if (objectType == null)
                            obj = null;
                        else
                            obj = Enum.ToObject(objectType, underlyingValue);
                        break;
                    case Category.Type:
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

                Category categoryId = (Category)context.Reader.ReadByte();
                switch (categoryId)
                {
                    case Category.Basic:
                        break;
                    case Category.Array:
                    case Category.List:
                    case Category.HashSet:
                        itemSize = context.Reader.ReadInt32();
                        break;
                    case Category.Dictionary:
                        keySize = context.Reader.ReadInt32();
                        itemSize = context.Reader.ReadInt32();
                        break;
                    case Category.Struct:
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
                    case Category.Class:
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
                    case Category.Enum:
                        uint underlyingTypeId = context.Reader.ReadUInt32();
                        enumUnderlyingTypeMetadata = context.Types[underlyingTypeId - 1];
                        break;
                    case Category.Type:
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
