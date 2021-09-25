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
        }

        /// <summary> Registers a type as serializable. </summary>
        /// <param name="type"> The type to register. </param>
        public void RegisterType(Type type, bool areItemsReference = true)
        {
            typeRegistry.RegisterType(type, areItemsReference);
        }

        #region Serialization

        /// <summary> Serializes an object graph as a string. </summary>
        /// <param name="obj"> The root object to be serialized. </param>
        /// <returns> The serialized object graph. </returns>
        public void Serialize(object obj, Stream outputStream)
        {
            // Initialize

            MemoryStream objectStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(outputStream, Encoding.Unicode, true);
            SerializationContext context = new SerializationContext(typeRegistry, objectStream);

            writer.Write(CompatibilityVersion);

            // Serialize objects

            bool rootObjectIsReferenceType = false;
            if (Toolbox.IsClass(obj.GetType()))
            {
                context.RegisterObject(obj);
                rootObjectIsReferenceType = true;
            }
            SerializeObject(obj, context);
            while (context.ObjectsToSerialize.Count > 0)
            {
                object objToSerialize = context.ObjectsToSerialize.Dequeue();
                SerializeObject(objToSerialize, context);
            }

            // Write type data

            writer.Write(context.Types.Count);
            for (int typeIdx = 0; typeIdx < context.TypesInOrder.Count; typeIdx++)
            {
                TypeMetadata typeMetadata = context.TypesInOrder[typeIdx];
                writer.Write(typeMetadata.Type.AssemblyQualifiedName);
                writer.Write(typeMetadata.ParentId);
                writer.Write(typeMetadata.Fields.Count);
                for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields.Count; fieldIdx++)
                {
                    FieldMetadata fieldMetadata = typeMetadata.Fields.Values[fieldIdx];
                    writer.Write(fieldMetadata.FieldInfo.Name);
                }
            }

            // Write object data

            writer.Write(context.Objects.Count);
            writer.Write(rootObjectIsReferenceType);

            for (int objectIdx = 0; objectIdx < context.ObjectsInOrder.Count; objectIdx++)
            {
                ObjectMetadata objectMetadata = context.ObjectsInOrder[objectIdx];
                writer.Write(objectMetadata.Type.Id);
                if (objectMetadata.Type.Type.IsArray)
                    writer.Write(objectMetadata.ArrayLength);
            }

            objectStream.Position = 0;
            objectStream.CopyTo(outputStream);

            // Clean up

            context.Dispose();
            objectStream.Close();
            writer.Close();
        }

        private void SerializeObject(object obj, SerializationContext context)
        {
            Type type = obj.GetType();

            if (type == typeof(byte))
                context.Writer.Write((byte)obj);
            else if (type == typeof(sbyte))
                context.Writer.Write((sbyte)obj);
            else if (type == typeof(bool))
                context.Writer.Write((bool)obj);
            else if (type == typeof(char))
                context.Writer.Write((char)obj);
            else if (type == typeof(short))
                context.Writer.Write((short)obj);
            else if (type == typeof(ushort))
                context.Writer.Write((ushort)obj);
            else if (type == typeof(int))
                context.Writer.Write((int)obj);
            else if (type == typeof(uint))
                context.Writer.Write((uint)obj);
            else if (type == typeof(long))
                context.Writer.Write((long)obj);
            else if (type == typeof(ulong))
                context.Writer.Write((ulong)obj);
            else if (type == typeof(float))
                context.Writer.Write((float)obj);
            else if (type == typeof(double))
                context.Writer.Write((double)obj);
            else if (type == typeof(decimal))
                context.Writer.Write((decimal)obj);
            else if (type == typeof(string))
                context.Writer.Write((string)obj);
            else if (type.IsEnum)
                SerializeEnum(obj, context);
            else if (type.IsValueType)
                SerializeStruct(obj, context);
            else if (type.IsClass)
                SerializeClass(obj, context);
            else
                throw new IcepackException($"Unable to serialize object: {obj}");
        }

        private void SerializeEnum(object obj, SerializationContext context)
        {
            Type type = obj.GetType();

            Type underlyingType = Enum.GetUnderlyingType(type);
            if (underlyingType == typeof(byte))
                context.Writer.Write((byte)obj);
            else if (underlyingType == typeof(sbyte))
                context.Writer.Write((sbyte)obj);
            else if (underlyingType == typeof(short))
                context.Writer.Write((short)obj);
            else if (underlyingType == typeof(ushort))
                context.Writer.Write((ushort)obj);
            else if (underlyingType == typeof(int))
                context.Writer.Write((int)obj);
            else if (underlyingType == typeof(uint))
                context.Writer.Write((uint)obj);
            else if (underlyingType == typeof(long))
                context.Writer.Write((long)obj);
            else if (underlyingType == typeof(ulong))
                context.Writer.Write((ulong)obj);
            else
                throw new IcepackException($"Unable to serialize enum: {obj}");
        }

        private void SerializeField(object value, bool isReference, SerializationContext context)
        {
            if (value == null)
                context.Writer.Write((uint)0);
            else if (isReference)
            {
                if (context.Objects.ContainsKey(value))
                    context.Writer.Write((context.Objects[value]).Id);
                else
                {
                    uint id = context.RegisterObject(value);
                    context.ObjectsToSerialize.Enqueue(value);
                    context.Writer.Write(id);
                }
            }
            else
                SerializeObject(value, context);
        }

        private void SerializeStruct(object obj, SerializationContext context)
        {
            if (obj is ISerializerListener)
                ((ISerializerListener)obj).OnBeforeSerialize();

            Type type = obj.GetType();
            TypeMetadata typeMetadata = context.GetTypeMetadata(type);

            context.Writer.Write(typeMetadata.Id);

            for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields.Count; fieldIdx++)
            {
                FieldMetadata field = typeMetadata.Fields.Values[fieldIdx];
                object value = field.Getter(obj);
                SerializeField(value, field.IsReference, context);
            }
        }

        private void SerializeArray(object obj, TypeMetadata typeMetadata, SerializationContext context)
        {
            Array array = (Array)obj;

            context.Writer.Write(array.Length);

            for (int arrayIdx = 0; arrayIdx < array.Length; arrayIdx++)
            {
                object item = array.GetValue(arrayIdx);
                SerializeField(item, typeMetadata.AreItemsReference, context);
            }
        }

        private void SerializeList(object obj, TypeMetadata typeMetadata, SerializationContext context)
        {
            IList list = (IList)obj;

            context.Writer.Write(list.Count);

            for (int itemIdx = 0; itemIdx < list.Count; itemIdx++)
            {
                object item = list[itemIdx];
                SerializeField(item, typeMetadata.AreItemsReference, context);
            }
        }

        private void SerializeHashSet(object obj, TypeMetadata typeMetadata, SerializationContext context)
        {
            IEnumerable set = (IEnumerable)obj;

            int count = 0;
            foreach (object item in set)
                count++;

            context.Writer.Write(count);

            foreach (object item in set)
                SerializeField(item, typeMetadata.AreItemsReference, context);
        }

        private void SerializeDictionary(object obj, TypeMetadata typeMetadata, SerializationContext context)
        {
            IDictionary dict = (IDictionary)obj;

            context.Writer.Write(dict.Count);

            foreach (DictionaryEntry entry in dict)
            {
                SerializeField(entry.Key, typeMetadata.AreItemsReference, context);
                SerializeField(entry.Value, typeMetadata.AreItemsReference, context);
            }
        }

        private void SerializeNormalClass(object obj, TypeMetadata typeMetadata, SerializationContext context)
        {
            context.Writer.Write(typeMetadata.Id);

            for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields.Count; fieldIdx++)
            {
                FieldMetadata field = typeMetadata.Fields.Values[fieldIdx];
                object value = field.Getter(obj);
                SerializeField(value, field.IsReference, context);
            }

            Type parentType = typeMetadata.Type.BaseType;
            if (parentType != typeof(object))
            {
                TypeMetadata parentTypeMetadata = context.GetTypeMetadata(parentType);
                if (parentTypeMetadata != null)
                    SerializeNormalClass(obj, parentTypeMetadata, context);
            }
        }

        private void SerializeClass(object obj, SerializationContext context)
        {
            if (obj is ISerializerListener)
                ((ISerializerListener)obj).OnBeforeSerialize();

            Type type = obj.GetType();
            TypeMetadata typeMetadata = context.GetTypeMetadata(type);
            context.Writer.Write(typeMetadata.Id);

            if (type.IsArray)
                SerializeArray(obj, typeMetadata, context);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                SerializeList(obj, typeMetadata, context);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                SerializeHashSet(obj, typeMetadata, context);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                SerializeDictionary(obj, typeMetadata, context);
            else
                SerializeNormalClass(obj, typeMetadata, context);
        }

        #endregion

        #region Deserialization

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

                uint parentId = context.Reader.ReadUInt32();

                int numberOfFields = context.Reader.ReadInt32();
                List<string> fieldNames = new List<string>(numberOfFields);
                for (int f = 0; f < numberOfFields; f++)
                    fieldNames.Add(context.Reader.ReadString());

                TypeMetadata typeMetadata = new TypeMetadata(registeredTypeMetadata, fieldNames, (uint)(t + 1), parentId);
                context.Types[t] = typeMetadata;
            }

            // Create empty objects

            int numberOfObjects = context.Reader.ReadInt32();
            bool rootObjectIsReferenceType = context.Reader.ReadBoolean();

            context.Objects = new object[numberOfObjects];
            context.ObjectTypes = new TypeMetadata[numberOfObjects];
            context.CurrentObjectId = 0;

            for (int i = 0; i < numberOfObjects; i++)
            {
                uint typeId = context.Reader.ReadUInt32();
                TypeMetadata objectTypeMetadata = context.Types[typeId - 1];
                Type objectType = objectTypeMetadata.Type;

                context.ObjectTypes[i] = objectTypeMetadata;

                object obj;
                if (objectType.IsArray)
                {
                    Type elementType = objectType.GetElementType();
                    int arrayLength = context.Reader.ReadInt32();
                    obj = Array.CreateInstance(elementType, arrayLength);
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
                var deserializationOperation = DeserializationOperationFactory.GetOperation(typeof(T), false);
                rootObject = (T)deserializationOperation(context);
            }

            for (int i = 0; i < numberOfObjects; i++)
            {
                context.CurrentObjectId = (uint)i + 1;
                DeserializationOperationFactory.DeserializeClass(context);
            }

            // Clean up

            context.Dispose();

            return rootObject;
        }

        #endregion
    }
}
