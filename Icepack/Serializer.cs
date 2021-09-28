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

            bool rootObjectIsReferenceType = false;
            if (rootObj.GetType().IsClass)
            {
                context.RegisterObject(rootObj);
                rootObjectIsReferenceType = true;
                SerializationOperationFactory.SerializeClass(rootObj, context);
            }
            else
            {
                var serializationOperation = SerializationOperationFactory.GetOperation(rootObj.GetType());
                serializationOperation(rootObj, context);
            }

            while (context.ObjectsToSerialize.Count > 0)
            {
                object objToSerialize = context.ObjectsToSerialize.Dequeue();
                SerializationOperationFactory.SerializeClass(objToSerialize, context);
            }

            // Write type data

            writer.Write(context.Types.Count);
            for (int typeIdx = 0; typeIdx < context.TypesInOrder.Count; typeIdx++)
            {
                TypeMetadata typeMetadata = context.TypesInOrder[typeIdx];
                writer.Write(typeMetadata.Type.AssemblyQualifiedName);
                writer.Write(typeMetadata.HasParent);

                Type type = typeMetadata.Type;
                if (type == typeof(string)) { }
                else if (type.IsArray)
                    writer.Write(typeMetadata.ItemSize);
                else if (type.IsGenericType)
                {
                    Type genericTypeDef = type.GetGenericTypeDefinition();
                    if (genericTypeDef == typeof(Dictionary<,>))
                    {
                        writer.Write(typeMetadata.KeySize);
                        writer.Write(typeMetadata.ItemSize);
                    }
                    else if (genericTypeDef == typeof(List<>) ||
                             genericTypeDef == typeof(HashSet<>))
                    {
                        writer.Write(typeMetadata.ItemSize);
                    }
                    else
                    {
                        writer.Write(typeMetadata.Size);
                    }
                }
                else
                {
                    writer.Write(typeMetadata.Size);
                }

                writer.Write(typeMetadata.Fields.Count);
                for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields.Count; fieldIdx++)
                {
                    FieldMetadata fieldMetadata = typeMetadata.Fields.Values[fieldIdx];
                    writer.Write(fieldMetadata.FieldInfo.Name);
                    writer.Write(fieldMetadata.Size);
                }
            }

            // Write object data

            writer.Write(context.Objects.Count);
            writer.Write(rootObjectIsReferenceType);

            for (int objectIdx = 0; objectIdx < context.ObjectsInOrder.Count; objectIdx++)
            {
                ObjectMetadata objectMetadata = context.ObjectsInOrder[objectIdx];
                writer.Write(objectMetadata.Type.Id);
                if (objectMetadata.Type.Type == typeof(string))
                {
                    writer.Write((string)objectMetadata.Value);
                }
                else if (objectMetadata.Type.Type.IsArray ||
                    objectMetadata.Type.Type.IsGenericType && (
                        objectMetadata.Type.Type.GetGenericTypeDefinition() == typeof(List<>) ||
                        objectMetadata.Type.Type.GetGenericTypeDefinition() == typeof(HashSet<>) ||
                        objectMetadata.Type.Type.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                    ))
                {
                    writer.Write(objectMetadata.Length);
                }
            }

            objectStream.Position = 0;
            objectStream.CopyTo(outputStream);

            // Clean up

            context.Dispose();
            objectStream.Close();
            writer.Close();
        }

        public T Deserialize<T>(Stream inputStream)
        {
            // Initialize

            inputStream.Position = 0;

            DeserializationContext context = new DeserializationContext(inputStream);

            ushort compatibilityVersion = context.Reader.ReadUInt16();
            if (compatibilityVersion != CompatibilityVersion)
                throw new IcepackException($"Expected compatibility version {CompatibilityVersion}, received {compatibilityVersion}");

            // Deserialize types

            int numberOfTypes = context.Reader.ReadInt32();
            context.Types = new TypeMetadata[numberOfTypes];

            for (int t = 0; t < numberOfTypes; t++)
            {
                string typeName = context.Reader.ReadString();
                TypeMetadata registeredTypeMetadata = typeRegistry.GetTypeMetadata(typeName);

                bool hasParent = context.Reader.ReadBoolean();

                int size = 0;
                int keySize = 0;
                int itemSize = 0;
                Type type = registeredTypeMetadata.Type;
                if (type == typeof(string)) { }
                else if (type.IsArray)
                    itemSize = context.Reader.ReadInt32();
                else if (type.IsGenericType)
                {
                    Type genericTypeDef = type.GetGenericTypeDefinition();
                    if (genericTypeDef == typeof(Dictionary<,>))
                    {
                        keySize = context.Reader.ReadInt32();
                        itemSize = context.Reader.ReadInt32();
                    }
                    else if (genericTypeDef == typeof(List<>) ||
                             genericTypeDef == typeof(HashSet<>))
                    {
                        itemSize = context.Reader.ReadInt32();
                    }
                    else
                    {
                        size = context.Reader.ReadInt32();
                    }
                }
                else
                {
                    size = context.Reader.ReadInt32();
                }

                int numberOfFields = context.Reader.ReadInt32();
                List<string> fieldNames = new List<string>(numberOfFields);
                List<int> fieldSizes = new List<int>(numberOfFields);
                for (int f = 0; f < numberOfFields; f++)
                {
                    fieldNames.Add(context.Reader.ReadString());
                    fieldSizes.Add(context.Reader.ReadInt32());
                }

                TypeMetadata typeMetadata = new TypeMetadata(registeredTypeMetadata, fieldNames, fieldSizes, (uint)(t + 1),
                    hasParent, size, keySize, itemSize);
                context.Types[t] = typeMetadata;
            }

            // Create empty objects

            int numberOfObjects = context.Reader.ReadInt32();
            bool rootObjectIsReferenceType = context.Reader.ReadBoolean();

            context.Objects = new object[numberOfObjects];
            context.ObjectTypes = new TypeMetadata[numberOfObjects];

            for (int i = 0; i < numberOfObjects; i++)
            {
                uint typeId = context.Reader.ReadUInt32();
                TypeMetadata objectTypeMetadata = context.Types[typeId - 1];
                Type objectType = objectTypeMetadata.Type;
                context.ObjectTypes[i] = objectTypeMetadata;

                object obj;
                if (objectType == typeof(string))
                    obj = context.Reader.ReadString();
                else if (objectType.IsArray)
                {
                    Type elementType = objectType.GetElementType();
                    int arrayLength = context.Reader.ReadInt32();
                    obj = Array.CreateInstance(elementType, arrayLength);
                }
                else if (objectType.IsGenericType && (
                    objectType.GetGenericTypeDefinition() == typeof(List<>) ||
                    objectType.GetGenericTypeDefinition() == typeof(HashSet<>) ||
                    objectType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                {
                    int length = context.Reader.ReadInt32();
                    obj = Activator.CreateInstance(objectType, length);
                }
                else
                    obj = Activator.CreateInstance(objectType);
                context.Objects[i] = obj;
            }

            // Deserialize objects

            T rootObject;
            if (rootObjectIsReferenceType)
                rootObject = (T)context.Objects[0];
            else
            {
                var deserializationOperation = DeserializationOperationFactory.GetOperation(typeof(T));
                rootObject = (T)deserializationOperation(context);
            }

            for (int i = 0; i < numberOfObjects; i++)
            {
                object classObj = context.Objects[i];
                DeserializationOperationFactory.DeserializeClass(classObj, context.ObjectTypes[i], context);
            }

            // Clean up

            context.Dispose();

            return rootObject;
        }
    }
}
