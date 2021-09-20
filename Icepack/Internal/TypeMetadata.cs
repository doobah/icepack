using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;

namespace Icepack
{
    /// <summary> Contains information necessary to serialize and deserialize a type. </summary>
    internal class TypeMetadata
    {
        private ulong id;
        private Type type;
        private SortedList<string, FieldMetadata> fields;
        private string serializedStr;
        private Action<object, object> hashSetAdder;
        private bool isItemsNoReference;

        /// <summary>
        /// Called during deserialization. Copies relevant information from the registered type metadata and filters the fields based on
        /// what is expected by the serialized data.
        /// </summary>
        /// <param name="registeredTypeMetadata"> The registered type metadata to copy information from. </param>
        /// <param name="objectTree"> The object tree for type metadata extracted from the serialized data. </param>
        public TypeMetadata(TypeMetadata registeredTypeMetadata, List<object> objectTree)
        {
            id = ulong.Parse((string)objectTree[0]);
            type = registeredTypeMetadata.type;

            fields = new SortedList<string, FieldMetadata>();
            for (int i = 2; i < objectTree.Count; i++)
            {
                string fieldName = (string)objectTree[i];
                FieldMetadata fieldMetadata = registeredTypeMetadata.Fields.GetValueOrDefault(fieldName, null);
                fields.Add(fieldName, fieldMetadata);
            }

            serializedStr = null;
            hashSetAdder = registeredTypeMetadata.hashSetAdder;
            isItemsNoReference = registeredTypeMetadata.isItemsNoReference;
        }

        /// <summary> Called during serialization. </summary>
        /// <param name="id"> A unique ID for the type. </param>
        /// <param name="type"> The type. </param>
        public TypeMetadata(ulong id, Type type, bool isItemsNoReference)
        {
            this.id = id;
            this.type = type;
            this.isItemsNoReference = isItemsNoReference;

            this.fields = new SortedList<string, FieldMetadata>();
            foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                IgnoreFieldAttribute ignoreAttr = fieldInfo.GetCustomAttribute<IgnoreFieldAttribute>();
                if (ignoreAttr == null)
                    fields.Add(fieldInfo.Name, new FieldMetadata(fieldInfo));
            }

            // These are lazy-initialized
            this.serializedStr = null;
            this.hashSetAdder = null;
        }

        /// <summary> A unique ID for the type. </summary>
        public ulong Id
        {
            get { return id; }
        }

        /// <summary> The type. </summary>
        public Type Type
        {
            get { return type; }
        }

        public bool IsItemsNoReference
        {
            get { return isItemsNoReference; }
        }

        /// <summary> A dictionary that maps a field name to information about the field. </summary>
        public SortedList<string, FieldMetadata> Fields
        {
            get { return fields; }
        }

        /// <summary> The serialized representation of this type. This is lazy-initialized as it is only needed for serialization. </summary>
        public string SerializedString
        {
            get
            {
                if (serializedStr == null)
                {
                    StringBuilder strBuilder = new StringBuilder();
                    strBuilder.Append('[');
                    strBuilder.Append(id);
                    strBuilder.Append(',');
                    strBuilder.Append(Toolbox.EscapeString(type.AssemblyQualifiedName));
                    foreach (FieldMetadata field in fields.Values)
                    {
                        strBuilder.Append(',');
                        strBuilder.Append(field.FieldInfo.Name);
                    }
                    strBuilder.Append(']');
                    serializedStr = strBuilder.ToString();
                }

                return serializedStr;
            }
        }

        public Action<object, object> HashSetAdder
        {
            get
            {
                if (hashSetAdder == null)
                {
                    MethodInfo methodInfo = type.GetMethod("Add");
                    Type itemType = type.GetGenericArguments()[0];

                    ParameterExpression exInstance = Expression.Parameter(typeof(object));
                    UnaryExpression exConvertInstanceToDeclaringType = Expression.Convert(exInstance, type);
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
