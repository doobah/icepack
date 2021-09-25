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
        public bool AreItemsReference { get; }
        public SortedList<string, FieldMetadata> Fields { get; }
        public uint ParentId { get; }

        private Action<object, object> hashSetAdder;

        /// <summary> Called during serialization. </summary>
        /// <param name="registeredTypeMetadata"></param>
        /// <param name="id"> A unique ID for the type. </param>
        public TypeMetadata(TypeMetadata registeredTypeMetadata, uint id, uint parentId)
        {
            Id = id;
            ParentId = parentId;
            Type = registeredTypeMetadata.Type;
            AreItemsReference = registeredTypeMetadata.AreItemsReference;
            Fields = registeredTypeMetadata.Fields;
            hashSetAdder = null;
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
                AreItemsReference = false;
                Fields = new SortedList<string, FieldMetadata>(fieldNames.Count);
                for (int i = 0; i < fieldNames.Count; i++)
                {
                    string fieldName = fieldNames[i];
                    Fields.Add(fieldName, null);
                }
            }
            else
            {
                Type = registeredTypeMetadata.Type;
                AreItemsReference = registeredTypeMetadata.AreItemsReference;

                Fields = new SortedList<string, FieldMetadata>(fieldNames.Count);
                for (int i = 0; i < fieldNames.Count; i++)
                {
                    string fieldName = fieldNames[i];
                    FieldMetadata fieldMetadata = registeredTypeMetadata.Fields.GetValueOrDefault(fieldName, null);
                    Fields.Add(fieldName, fieldMetadata);
                }
            }

            hashSetAdder = null;
        }

        /// <summary> Called during type registration. </summary>
        /// <param name="type"> The type. </param>
        public TypeMetadata(Type type, bool areItemsReference)
        {
            Id = 0;
            ParentId = 0;
            Type = type;
            AreItemsReference = calculateAreItemsReference(type, areItemsReference);

            Fields = new SortedList<string, FieldMetadata>();
            foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                IgnoreFieldAttribute ignoreAttr = fieldInfo.GetCustomAttribute<IgnoreFieldAttribute>();
                if (ignoreAttr == null)
                    Fields.Add(fieldInfo.Name, new FieldMetadata(fieldInfo));
            }

            // These are lazy-initialized
            hashSetAdder = null;
        }

        private bool calculateAreItemsReference(Type type, bool areItemsReference)
        {
            if (!areItemsReference)
                return false;

            if (type.IsArray)
                return Toolbox.IsClass(type.GetElementType());

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(List<>))
                    return Toolbox.IsClass(type.GetGenericArguments()[0]);
                else if (type.GetGenericTypeDefinition() == typeof(HashSet<>))
                    return Toolbox.IsClass(type.GetGenericArguments()[0]);
                else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Type[] genericArgs = type.GetGenericArguments();
                    return Toolbox.IsClass(genericArgs[0]) && Toolbox.IsClass(genericArgs[1]);
                }
            }

            return true;
        }

        public Action<object, object> HashSetAdder
        {
            get
            {
                if (hashSetAdder == null)
                {
                    MethodInfo methodInfo = Type.GetMethod("Add");
                    Type itemType = Type.GetGenericArguments()[0];

                    ParameterExpression exInstance = Expression.Parameter(typeof(object));
                    UnaryExpression exConvertInstanceToDeclaringType = Expression.Convert(exInstance, Type);
                    ParameterExpression exValue = Expression.Parameter(typeof(object));
                    UnaryExpression exConvertValueToItemType = Expression.Convert(exValue, itemType);
                    MethodCallExpression exAdd = Expression.Call(exConvertInstanceToDeclaringType, methodInfo, exConvertValueToItemType);
                    Expression<Action<object, object>> lambda = Expression.Lambda<Action<object, object>>(exAdd, exInstance, exValue);
                    hashSetAdder = lambda.Compile();
                }

                return hashSetAdder;
            }
        }
    }
}
