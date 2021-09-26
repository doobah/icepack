using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using System.IO;

namespace Icepack
{
    /// <summary> Contains information necessary to serialize and deserialize a type. </summary>
    internal class TypeMetadata
    {
        public uint Id { get; }
        public Type Type { get; }
        public SortedList<string, FieldMetadata> Fields { get; }
        public uint ParentId { get; }
        public Action<object, object> HashSetAdder { get; }
        public Action<object, SerializationContext> SerializeKey { get; }
        public Action<object, SerializationContext> SerializeItem { get; }
        public Func<DeserializationContext, object> DeserializeKey { get; }
        public Func<DeserializationContext, object> DeserializeItem { get; }

        /// <summary> Called during serialization. </summary>
        /// <param name="registeredTypeMetadata"></param>
        /// <param name="id"> A unique ID for the type. </param>
        public TypeMetadata(TypeMetadata registeredTypeMetadata, uint id, uint parentId)
        {
            Id = id;
            ParentId = parentId;
            Type = registeredTypeMetadata.Type;
            Fields = registeredTypeMetadata.Fields;
            HashSetAdder = registeredTypeMetadata.HashSetAdder;
            SerializeKey = registeredTypeMetadata.SerializeKey;
            SerializeItem = registeredTypeMetadata.SerializeItem;
            DeserializeKey = registeredTypeMetadata.DeserializeKey;
            DeserializeItem = registeredTypeMetadata.DeserializeItem;
        }

        /// <summary>
        /// Called during deserialization. Copies relevant information from the registered type metadata and filters the fields based on
        /// what is expected by the serialized data.
        /// </summary>
        /// <param name="registeredTypeMetadata"> The registered type metadata to copy information from. </param>
        /// <param name="objectTree"> The object tree for type metadata extracted from the serialized data. </param>
        /// <param name="id"> A unique ID for the type. </param>
        public TypeMetadata(TypeMetadata registeredTypeMetadata, List<string> fieldNames, uint id, uint parentId)
        {
            Id = id;
            ParentId = parentId;

            if (registeredTypeMetadata == null)
            {
                Type = null;
                Fields = new SortedList<string, FieldMetadata>(fieldNames.Count);
                for (int i = 0; i < fieldNames.Count; i++)
                {
                    string fieldName = fieldNames[i];
                    Fields.Add(fieldName, null);
                }
                HashSetAdder = null;
                SerializeKey = null;
                SerializeItem = null;
                DeserializeKey = null;
                DeserializeItem = null;
            }
            else
            {
                Type = registeredTypeMetadata.Type;

                Fields = new SortedList<string, FieldMetadata>(fieldNames.Count);
                for (int i = 0; i < fieldNames.Count; i++)
                {
                    string fieldName = fieldNames[i];
                    FieldMetadata fieldMetadata = registeredTypeMetadata.Fields.GetValueOrDefault(fieldName, null);
                    Fields.Add(fieldName, fieldMetadata);
                }

                HashSetAdder = registeredTypeMetadata.HashSetAdder;
                SerializeKey = registeredTypeMetadata.SerializeKey;
                SerializeItem = registeredTypeMetadata.SerializeItem;
                DeserializeKey = registeredTypeMetadata.DeserializeKey;
                DeserializeItem = registeredTypeMetadata.DeserializeItem;
            }
        }

        /// <summary> Called during type registration. </summary>
        /// <param name="type"> The type. </param>
        public TypeMetadata(Type type)
        {
            Id = 0;
            ParentId = 0;
            Type = type;

            Fields = new SortedList<string, FieldMetadata>();
            if (!IsSpecialClassType(type))
            {
                foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    IgnoreFieldAttribute ignoreAttr = fieldInfo.GetCustomAttribute<IgnoreFieldAttribute>();
                    if (ignoreAttr == null)
                        Fields.Add(fieldInfo.Name, new FieldMetadata(fieldInfo));
                }
            }

            HashSetAdder = null;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                HashSetAdder = BuildHashSetAdder();

            // Serialization/Deserialization operations

            SerializeKey = null;
            DeserializeKey = null;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type keyType = type.GenericTypeArguments[0];
                SerializeKey = SerializationOperationFactory.GetOperation(keyType);
                DeserializeKey = DeserializationOperationFactory.GetOperation(keyType);
            }

            SerializeItem = null;
            DeserializeItem = null;
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                SerializeItem = SerializationOperationFactory.GetOperation(elementType);
                DeserializeItem = DeserializationOperationFactory.GetOperation(elementType);
            }
            else if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type itemType = type.GenericTypeArguments[0];
                    SerializeItem = SerializationOperationFactory.GetOperation(itemType);
                    DeserializeItem = DeserializationOperationFactory.GetOperation(itemType);
                }
                else if (type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    Type itemType = type.GenericTypeArguments[0];
                    SerializeItem = SerializationOperationFactory.GetOperation(itemType);
                    DeserializeItem = DeserializationOperationFactory.GetOperation(itemType);
                }
                else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Type itemType = type.GenericTypeArguments[1];
                    SerializeItem = SerializationOperationFactory.GetOperation(itemType);
                    DeserializeItem = DeserializationOperationFactory.GetOperation(itemType);
                }
            }
        }

        private bool IsSpecialClassType(Type type)
        {
            if (type.IsArray)
                return true;

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(List<>) ||
                    type.GetGenericTypeDefinition() == typeof(HashSet<>) ||
                    type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    return true;
                }
            }

            return false;
        }

        private Action<object, object> BuildHashSetAdder()
        {
            MethodInfo methodInfo = Type.GetMethod("Add");
            Type itemType = Type.GetGenericArguments()[0];

            ParameterExpression exInstance = Expression.Parameter(typeof(object));
            UnaryExpression exConvertInstanceToDeclaringType = Expression.Convert(exInstance, Type);
            ParameterExpression exValue = Expression.Parameter(typeof(object));
            UnaryExpression exConvertValueToItemType = Expression.Convert(exValue, itemType);
            MethodCallExpression exAdd = Expression.Call(exConvertInstanceToDeclaringType, methodInfo, exConvertValueToItemType);
            Expression<Action<object, object>> lambda = Expression.Lambda<Action<object, object>>(exAdd, exInstance, exValue);
            Action<object, object> action = lambda.Compile();

            return action;
        }
    }
}
