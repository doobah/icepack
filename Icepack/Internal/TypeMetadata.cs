using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using System.IO;

namespace Icepack
{
    /// <summary> Contains information necessary to serialize/deserialize a type. </summary>
    internal class TypeMetadata
    {
        public uint Id { get; }
        public Type Type { get; }
        public TypeMetadata EnumUnderlyingTypeMetadata { get; }
        public List<FieldMetadata> Fields { get; }
        public Dictionary<string, FieldMetadata> FieldsByName { get; }
        public Dictionary<string, FieldMetadata> FieldsByPreviousName { get; }
        public bool HasParent { get; }
        public Action<object, object> HashSetAdder { get; }
        public Action<object, SerializationContext, BinaryWriter> SerializeKey { get; }
        public Action<object, SerializationContext, BinaryWriter> SerializeItem { get; }
        public Action<object, SerializationContext, BinaryWriter> SerializeImmutable { get; }
        public Action<ObjectMetadata, SerializationContext, BinaryWriter> SerializeReferenceType { get; }
        public Func<DeserializationContext, BinaryReader, object> DeserializeKey { get; }
        public Func<DeserializationContext, BinaryReader, object> DeserializeItem { get; }
        public Func<DeserializationContext, BinaryReader, object> DeserializeImmutable { get; }
        public Action<ObjectMetadata, DeserializationContext, BinaryReader> DeserializeReferenceType { get; }
        public TypeCategory Category { get; }
        public int ItemSize { get; }
        public int KeySize { get; }        
        public int InstanceSize { get; }

        /// <summary> Called during serialization. </summary>
        /// <param name="registeredTypeMetadata"></param>
        /// <param name="id"> A unique ID for the type. </param>
        public TypeMetadata(TypeMetadata registeredTypeMetadata, uint id, TypeMetadata enumUnderlyingTypeMetadata)
        {
            Id = id;
            EnumUnderlyingTypeMetadata = enumUnderlyingTypeMetadata;

            HasParent = registeredTypeMetadata.HasParent;
            Type = registeredTypeMetadata.Type;
            Fields = registeredTypeMetadata.Fields;
            FieldsByName = registeredTypeMetadata.FieldsByName;
            FieldsByPreviousName = registeredTypeMetadata.FieldsByPreviousName;
            Category = registeredTypeMetadata.Category;
            ItemSize = registeredTypeMetadata.ItemSize;
            KeySize = registeredTypeMetadata.KeySize;
            InstanceSize = registeredTypeMetadata.InstanceSize;
            HashSetAdder = registeredTypeMetadata.HashSetAdder;
            SerializeKey = registeredTypeMetadata.SerializeKey;
            SerializeItem = registeredTypeMetadata.SerializeItem;
            SerializeImmutable = registeredTypeMetadata.SerializeImmutable;
            SerializeReferenceType = registeredTypeMetadata.SerializeReferenceType;
            DeserializeKey = registeredTypeMetadata.DeserializeKey;
            DeserializeItem = registeredTypeMetadata.DeserializeItem;
            DeserializeImmutable = registeredTypeMetadata.DeserializeImmutable;
            DeserializeReferenceType = registeredTypeMetadata.DeserializeReferenceType;
        }

        /// <summary>
        /// Called during deserialization. Copies relevant information from the registered type metadata and filters the fields based on
        /// what is provided by the serialized data.
        /// </summary>
        /// <param name="registeredTypeMetadata"> The registered type metadata to copy information from. </param>
        /// <param name="objectTree"> The object tree for type metadata extracted from the serialized data. </param>
        /// <param name="id"> A unique ID for the type. </param>
        public TypeMetadata(TypeMetadata registeredTypeMetadata, List<string> fieldNames, List<int> fieldSizes,
            uint id, bool hasParent, TypeCategory category, int itemSize, int keySize, int instanceSize, TypeMetadata enumUnderlyingTypeMetadata)
        {
            Id = id;
            HasParent = hasParent;
            Category = category;
            ItemSize = itemSize;
            KeySize = keySize;
            InstanceSize = instanceSize;
            EnumUnderlyingTypeMetadata = enumUnderlyingTypeMetadata;
            FieldsByName = null;
            FieldsByPreviousName = null;
            SerializeReferenceType = SerializationOperationFactory.GetReferenceTypeOperation(category);
            DeserializeReferenceType = DeserializationOperationFactory.GetReferenceTypeOperation(category);

            if (registeredTypeMetadata == null)
            {
                Type = null;
                Fields = null;
                HashSetAdder = null;
                SerializeKey = null;
                SerializeItem = null;
                SerializeImmutable = null;
                DeserializeKey = null;
                DeserializeItem = null;
                DeserializeImmutable = null;
            }
            else
            {
                Type = registeredTypeMetadata.Type;

                if (fieldNames == null)
                    Fields = null;
                else
                {
                    Fields = new List<FieldMetadata>(fieldNames.Count);
                    for (int i = 0; i < fieldNames.Count; i++)
                    {
                        string fieldName = fieldNames[i];
                        int fieldSize = fieldSizes[i];
                        FieldMetadata registeredFieldMetadata = registeredTypeMetadata.FieldsByName.GetValueOrDefault(fieldName, null);
                        if (registeredFieldMetadata == null)
                            registeredFieldMetadata = registeredTypeMetadata.FieldsByPreviousName.GetValueOrDefault(fieldName, null);
                        var fieldMetadata = new FieldMetadata(fieldSize, registeredFieldMetadata);
                        Fields.Add(fieldMetadata);
                    }
                }

                HashSetAdder = registeredTypeMetadata.HashSetAdder;
                SerializeKey = registeredTypeMetadata.SerializeKey;
                SerializeItem = registeredTypeMetadata.SerializeItem;
                SerializeImmutable = registeredTypeMetadata.SerializeImmutable;
                DeserializeKey = registeredTypeMetadata.DeserializeKey;
                DeserializeItem = registeredTypeMetadata.DeserializeItem;
                DeserializeImmutable = registeredTypeMetadata.DeserializeImmutable;
            }
        }

        /// <summary> Called during type registration. </summary>
        /// <param name="type"> The type. </param>
        public TypeMetadata(Type type, TypeRegistry typeRegistry)
        {
            HasParent = false;
            Fields = new List<FieldMetadata>();
            FieldsByName = new Dictionary<string, FieldMetadata>();
            FieldsByPreviousName = new Dictionary<string, FieldMetadata>();
            HashSetAdder = null;
            SerializeKey = null;
            SerializeItem = null;
            SerializeImmutable = null;
            SerializeReferenceType = null;
            DeserializeKey = null;
            DeserializeItem = null;
            DeserializeImmutable = null;
            DeserializeReferenceType = null;
            ItemSize = 0;
            KeySize = 0;
            InstanceSize = 0;
            Id = 0;
            EnumUnderlyingTypeMetadata = null;

            Type = type;
            Category = GetCategory(type);

            switch (Category)
            {
                case TypeCategory.Immutable:
                    {
                        SerializeImmutable = SerializationOperationFactory.GetImmutableOperation(type);
                        DeserializeImmutable = DeserializationOperationFactory.GetImmutableOperation(type);
                        break;
                    }
                case TypeCategory.Array:
                    {
                        Type elementType = type.GetElementType();
                        SerializeItem = SerializationOperationFactory.GetFieldOperation(elementType);
                        DeserializeItem = DeserializationOperationFactory.GetFieldOperation(elementType);
                        ItemSize = TypeSizeFactory.GetFieldSize(elementType, typeRegistry);
                        break;
                    }
                case TypeCategory.List:
                    {
                        Type itemType = type.GenericTypeArguments[0];
                        SerializeItem = SerializationOperationFactory.GetFieldOperation(itemType);
                        DeserializeItem = DeserializationOperationFactory.GetFieldOperation(itemType);
                        ItemSize = TypeSizeFactory.GetFieldSize(itemType, typeRegistry);
                        break;
                    }
                case TypeCategory.HashSet:
                    {
                        Type itemType = type.GenericTypeArguments[0];
                        SerializeItem = SerializationOperationFactory.GetFieldOperation(itemType);
                        DeserializeItem = DeserializationOperationFactory.GetFieldOperation(itemType);
                        ItemSize = TypeSizeFactory.GetFieldSize(itemType, typeRegistry);
                        HashSetAdder = BuildHashSetAdder();
                        break;
                    }
                case TypeCategory.Dictionary:
                    {
                        Type keyType = type.GenericTypeArguments[0];
                        SerializeKey = SerializationOperationFactory.GetFieldOperation(keyType);
                        DeserializeKey = DeserializationOperationFactory.GetFieldOperation(keyType);
                        KeySize = TypeSizeFactory.GetFieldSize(keyType, typeRegistry);
                        Type valueType = type.GenericTypeArguments[1];
                        SerializeItem = SerializationOperationFactory.GetFieldOperation(valueType);
                        DeserializeItem = DeserializationOperationFactory.GetFieldOperation(valueType);
                        ItemSize = TypeSizeFactory.GetFieldSize(valueType, typeRegistry);
                        break;
                    }
                case TypeCategory.Struct:
                    {
                        PopulateFields(typeRegistry);
                        InstanceSize = CalculateSize();
                        break;
                    }
                case TypeCategory.Class:
                    {
                        HasParent = Type.BaseType != typeof(object);
                        PopulateFields(typeRegistry);
                        InstanceSize = CalculateSize();
                        break;
                    }
                case TypeCategory.Enum:
                    {
                        EnumUnderlyingTypeMetadata = typeRegistry.GetTypeMetadata(Type.GetEnumUnderlyingType());
                        break;
                    }
                case TypeCategory.Type:
                    {
                        break;
                    }
                default:
                    throw new IcepackException($"Invalid category ID: {Category}");
            }

            SerializeReferenceType = SerializationOperationFactory.GetReferenceTypeOperation(Category);
            DeserializeReferenceType = DeserializationOperationFactory.GetReferenceTypeOperation(Category);
        }

        private static TypeCategory GetCategory(Type type)
        {
            if (type == typeof(string) || type.IsPrimitive || type == typeof(decimal))
                return TypeCategory.Immutable;
            else if (type == typeof(Type))
                return TypeCategory.Type;
            else if (type.IsEnum)
                return TypeCategory.Enum;
            else if (type.IsArray)
                return TypeCategory.Array;
            else if (type.IsGenericType)
            {
                Type genericTypeDef = type.GetGenericTypeDefinition();

                if (genericTypeDef == typeof(List<>))
                    return TypeCategory.List;
                else if (genericTypeDef == typeof(HashSet<>))
                    return TypeCategory.HashSet;
                else if (genericTypeDef == typeof(Dictionary<,>))
                    return TypeCategory.Dictionary;
                else if (type.IsValueType)
                    return TypeCategory.Struct;
                else
                    return TypeCategory.Class;
            }
            else if (type.IsValueType)
                return TypeCategory.Struct;
            else
                return TypeCategory.Class;
        }

        private void PopulateFields(TypeRegistry typeRegistry)
        {
            foreach (FieldInfo fieldInfo in Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (fieldInfo.IsInitOnly)
                    continue;

                IgnoreFieldAttribute ignoreAttr = fieldInfo.GetCustomAttribute<IgnoreFieldAttribute>();
                if (ignoreAttr != null)
                    continue;

                var fieldMetadata = new FieldMetadata(fieldInfo, typeRegistry);
                FieldsByName.Add(fieldInfo.Name, fieldMetadata);

                PreviousNameAttribute previousNameAttr = fieldInfo.GetCustomAttribute<PreviousNameAttribute>();
                if (previousNameAttr != null)
                    FieldsByPreviousName.Add(previousNameAttr.Name, fieldMetadata);

                Fields.Add(fieldMetadata);
            }
        }

        private int CalculateSize()
        {
            int size = 0;
            foreach (FieldMetadata field in Fields)
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
