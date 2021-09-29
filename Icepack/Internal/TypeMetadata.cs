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
        public bool HasParent { get; }
        public Action<object, object> HashSetAdder { get; }
        public Action<object, SerializationContext> SerializeKey { get; }
        public Action<object, SerializationContext> SerializeItem { get; }
        public Func<DeserializationContext, object> DeserializeKey { get; }
        public Func<DeserializationContext, object> DeserializeItem { get; }

        public byte CategoryId { get; }

        public int ItemSize { get; }

        public int KeySize { get; }
        
        public int InstanceSize { get; }

        /// <summary> Called during serialization. </summary>
        /// <param name="registeredTypeMetadata"></param>
        /// <param name="id"> A unique ID for the type. </param>
        public TypeMetadata(TypeMetadata registeredTypeMetadata, uint id)
        {
            Id = id;

            HasParent = registeredTypeMetadata.HasParent;
            Type = registeredTypeMetadata.Type;
            Fields = registeredTypeMetadata.Fields;
            CategoryId = registeredTypeMetadata.CategoryId;
            ItemSize = registeredTypeMetadata.ItemSize;
            KeySize = registeredTypeMetadata.KeySize;
            InstanceSize = registeredTypeMetadata.InstanceSize;
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
        public TypeMetadata(TypeMetadata registeredTypeMetadata, List<string> fieldNames, List<int> fieldSizes,
            uint id, bool hasParent, byte categoryId, int itemSize, int keySize, int instanceSize)
        {
            Id = id;
            HasParent = hasParent;
            CategoryId = categoryId;
            ItemSize = itemSize;
            KeySize = keySize;
            InstanceSize = instanceSize;

            if (registeredTypeMetadata == null)
            {
                Type = null;
                Fields = null;
                HashSetAdder = null;
                SerializeKey = null;
                SerializeItem = null;
                DeserializeKey = null;
                DeserializeItem = null;
            }
            else
            {
                Type = registeredTypeMetadata.Type;

                if (fieldNames == null)
                    Fields = null;
                else
                {
                    Fields = new SortedList<string, FieldMetadata>(fieldNames.Count);
                    for (int i = 0; i < fieldNames.Count; i++)
                    {
                        string fieldName = fieldNames[i];
                        int fieldSize = fieldSizes[i];
                        FieldMetadata registeredFieldMetadata = registeredTypeMetadata.Fields.GetValueOrDefault(fieldName, null);
                        FieldMetadata fieldMetadata = new FieldMetadata(fieldSize, registeredFieldMetadata);
                        Fields.Add(fieldName, fieldMetadata);
                    }
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
        public TypeMetadata(Type type, TypeRegistry typeRegistry)
        {
            Id = 0;
            Type = type;

            HasParent = false;
            Fields = new SortedList<string, FieldMetadata>();
            HashSetAdder = null;
            SerializeKey = null;
            DeserializeKey = null;
            SerializeItem = null;
            DeserializeItem = null;
            CategoryId = 0;
            ItemSize = 0;
            KeySize = 0;
            InstanceSize = 0;

            if (type == typeof(string))
                CategoryId = 0;
            else if (type.IsArray)
            {
                CategoryId = 1;
                Type elementType = type.GetElementType();
                SerializeItem = SerializationOperationFactory.GetOperation(elementType);
                DeserializeItem = DeserializationOperationFactory.GetOperation(elementType);
                ItemSize = TypeSizeFactory.GetFieldSize(elementType, typeRegistry);
            }
            else if (type.IsGenericType)
            {
                Type genericTypeDef = type.GetGenericTypeDefinition();

                if (genericTypeDef == typeof(List<>))
                {
                    CategoryId = 2;
                    Type itemType = type.GenericTypeArguments[0];
                    SerializeItem = SerializationOperationFactory.GetOperation(itemType);
                    DeserializeItem = DeserializationOperationFactory.GetOperation(itemType);
                    ItemSize = TypeSizeFactory.GetFieldSize(itemType, typeRegistry);
                }
                else if (genericTypeDef == typeof(HashSet<>))
                {
                    CategoryId = 3;
                    Type itemType = type.GenericTypeArguments[0];
                    SerializeItem = SerializationOperationFactory.GetOperation(itemType);
                    DeserializeItem = DeserializationOperationFactory.GetOperation(itemType);
                    ItemSize = TypeSizeFactory.GetFieldSize(itemType, typeRegistry);
                    HashSetAdder = BuildHashSetAdder();
                }
                else if (genericTypeDef == typeof(Dictionary<,>))
                {
                    CategoryId = 4;
                    Type keyType = type.GenericTypeArguments[0];
                    SerializeKey = SerializationOperationFactory.GetOperation(keyType);
                    DeserializeKey = DeserializationOperationFactory.GetOperation(keyType);
                    KeySize = TypeSizeFactory.GetFieldSize(keyType, typeRegistry);
                    Type itemType = type.GenericTypeArguments[1];
                    SerializeItem = SerializationOperationFactory.GetOperation(itemType);
                    DeserializeItem = DeserializationOperationFactory.GetOperation(itemType);
                    ItemSize = TypeSizeFactory.GetFieldSize(itemType, typeRegistry);
                }
                else if (Toolbox.IsStruct(Type))
                {
                    CategoryId = 5;
                    PopulateFields(typeRegistry);
                    InstanceSize = CalculateSize();
                }
                else
                {
                    CategoryId = 6;
                    HasParent = Type.BaseType != typeof(object);
                    PopulateFields(typeRegistry);
                    InstanceSize = CalculateSize();
                }
            }
            else if (Toolbox.IsStruct(Type))
            {
                CategoryId = 5;
                PopulateFields(typeRegistry);
                InstanceSize = CalculateSize();
            }
            else
            {
                CategoryId = 6;
                HasParent = Type.BaseType != typeof(object);
                PopulateFields(typeRegistry);
                InstanceSize = CalculateSize();
            }
        }

        private void PopulateFields(TypeRegistry typeRegistry)
        {
            foreach (FieldInfo fieldInfo in Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                IgnoreFieldAttribute ignoreAttr = fieldInfo.GetCustomAttribute<IgnoreFieldAttribute>();
                if (ignoreAttr == null)
                    Fields.Add(fieldInfo.Name, new FieldMetadata(fieldInfo, typeRegistry));
            }
        }

        private int CalculateSize()
        {
            int size = 0;
            foreach (FieldMetadata field in Fields.Values)
                size += field.Size;

            return size;
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
