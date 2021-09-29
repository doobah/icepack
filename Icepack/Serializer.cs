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

        private TypeRegistry typeRegistry;

        public Serializer()
        {
            typeRegistry = new TypeRegistry();

            // string type is automatically registered
            RegisterType(typeof(string));
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

            MemoryStream objectStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(outputStream, Encoding.Unicode, true);
            SerializationContext context = new SerializationContext(typeRegistry, objectStream);

            writer.Write(CompatibilityVersion);

            // Serialize objects

            bool rootObjectIsValueType;
            if (rootObj.GetType().IsClass)
            {
                rootObjectIsValueType = false;
                context.RegisterObject(rootObj);
                SerializationOperationFactory.SerializeClass(rootObj, context);
            }
            else
            {
                rootObjectIsValueType = true;
                var serializationOperation = SerializationOperationFactory.GetOperation(rootObj.GetType());
                serializationOperation(rootObj, context);
            }

            while (context.ObjectsToSerialize.Count > 0)
            {
                object objToSerialize = context.ObjectsToSerialize.Dequeue();
                SerializationOperationFactory.SerializeClass(objToSerialize, context);
            }

            SerializeTypeMetadata(writer, context);
            SerializeObjectMetadata(writer, context);

            writer.Write(rootObjectIsValueType);

            objectStream.Position = 0;
            objectStream.CopyTo(outputStream);

            // Clean up

            context.Dispose();
            objectStream.Close();
            writer.Close();
        }

        private void SerializeObjectMetadata(BinaryWriter writer, SerializationContext context)
        {
            writer.Write(context.Objects.Count);

            for (int objectIdx = 0; objectIdx < context.ObjectsInOrder.Count; objectIdx++)
            {
                ObjectMetadata objectMetadata = context.ObjectsInOrder[objectIdx];
                writer.Write(objectMetadata.Type.Id);

                switch (objectMetadata.Type.CategoryId)
                {
                    case 0: // String
                        writer.Write((string)objectMetadata.Value);
                        break;
                    case 1: // Array
                    case 2: // List
                    case 3: // HashSet
                    case 4: // Dictionary
                        writer.Write(objectMetadata.Length);
                        break;
                }
            }
        }

        private void SerializeTypeMetadata(BinaryWriter writer, SerializationContext context)
        {
            writer.Write(context.Types.Count);
            for (int typeIdx = 0; typeIdx < context.TypesInOrder.Count; typeIdx++)
            {
                TypeMetadata typeMetadata = context.TypesInOrder[typeIdx];
                writer.Write(typeMetadata.Type.AssemblyQualifiedName);
                writer.Write(typeMetadata.CategoryId);
                switch (typeMetadata.CategoryId)
                {
                    case 0: // String
                        break;
                    case 1: // Array
                    case 2: // List
                        writer.Write(typeMetadata.ItemSize);
                        break;
                    case 3: // HashSet
                        writer.Write(typeMetadata.ItemSize);
                        break;
                    case 4: // Dictionary
                        writer.Write(typeMetadata.KeySize);
                        writer.Write(typeMetadata.ItemSize);
                        break;
                    case 5: // Struct
                        writer.Write(typeMetadata.InstanceSize);
                        writer.Write(typeMetadata.Fields.Count);
                        for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields.Count; fieldIdx++)
                        {
                            FieldMetadata fieldMetadata = typeMetadata.Fields.Values[fieldIdx];
                            writer.Write(fieldMetadata.FieldInfo.Name);
                            writer.Write(fieldMetadata.Size);
                        }
                        break;
                    case 6: // Class
                        writer.Write(typeMetadata.InstanceSize);
                        writer.Write(typeMetadata.HasParent);
                        writer.Write(typeMetadata.Fields.Count);
                        for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields.Count; fieldIdx++)
                        {
                            FieldMetadata fieldMetadata = typeMetadata.Fields.Values[fieldIdx];
                            writer.Write(fieldMetadata.FieldInfo.Name);
                            writer.Write(fieldMetadata.Size);
                        }
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

            DeserializationContext context = new DeserializationContext(inputStream);

            ushort compatibilityVersion = context.Reader.ReadUInt16();
            if (compatibilityVersion != CompatibilityVersion)
                throw new IcepackException($"Expected compatibility version {CompatibilityVersion}, received {compatibilityVersion}");

            DeserializeTypeMetadata(context);
            DeserializeObjectMetadata(context);

            // Deserialize objects

            bool rootObjectIsValueType = context.Reader.ReadBoolean();

            T rootObject;
            if (rootObjectIsValueType)
            {
                var deserializationOperation = DeserializationOperationFactory.GetOperation(typeof(T));
                rootObject = (T)deserializationOperation(context);
            }
            else
                rootObject = (T)context.Objects[0].Value;

            for (int i = 0; i < context.Objects.Length; i++)
            {
                ObjectMetadata classObjMetadata = context.Objects[i];
                DeserializationOperationFactory.DeserializeClass(classObjMetadata, context);
            }

            // Clean up

            context.Dispose();

            return rootObject;
        }

        private void DeserializeObjectMetadata(DeserializationContext context)
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
                    case 0: // String
                        obj = context.Reader.ReadString();
                        break;
                    case 1: // Array
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
                    case 2: // List
                    case 3: // HashSet
                    case 4: // Dictionary
                        {
                            length = context.Reader.ReadInt32();
                            if (objectType == null)
                                obj = null;
                            else
                                obj = Activator.CreateInstance(objectType, length);
                            break;
                        }
                    case 5: // Struct
                        throw new IcepackException($"Unexpected category ID: {objectTypeMetadata.CategoryId}");
                    case 6: // Class
                        if (objectType == null)
                            obj = null;
                        else
                            obj = Activator.CreateInstance(objectType);
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

                byte categoryId = context.Reader.ReadByte();
                switch (categoryId)
                {
                    case 0: // String
                        break;
                    case 1: // Array
                    case 2: // List
                    case 3: // HashSet
                        itemSize = context.Reader.ReadInt32();
                        break;
                    case 4: // Dictionary
                        keySize = context.Reader.ReadInt32();
                        itemSize = context.Reader.ReadInt32();
                        break;
                    case 5: // Struct
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
                    case 6: // Class
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
                    default:
                        throw new IcepackException($"Invalid category ID: {categoryId}");
                }

                TypeMetadata typeMetadata = new TypeMetadata(registeredTypeMetadata, fieldNames, fieldSizes, (uint)(t + 1),
                    hasParent, categoryId, itemSize, keySize, instanceSize);
                context.Types[t] = typeMetadata;
            }
        }
    }
}
