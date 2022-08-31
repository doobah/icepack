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
    internal sealed class TypeMetadata
    {
        /// <summary> A unique ID for the type. This is not assigned during registration. </summary>
        public uint Id { get; }

        /// <summary> The type. </summary>
        public Type Type { get; }

        /// <summary> Only used for enum types. This is metadata for the underlying type. </summary>
        public TypeMetadata EnumUnderlyingTypeMetadata { get; }

        /// <summary> Only used for non-immutable and regular struct and class types. Metadata for each serializable field. </summary>
        public List<FieldMetadata> Fields { get; }

        /// <summary> Maps a field name to metadata about that field. </summary>
        public Dictionary<string, FieldMetadata> FieldsByName { get; }

        /// <summary> Maps a field's previous name (specified by <see cref="PreviousNameAttribute"/>) to metadata about that field. </summary>
        public Dictionary<string, FieldMetadata> FieldsByPreviousName { get; }

        /// <summary> Only used for regular class types. This indicates whether the class has a base class that is not <see cref="object"/>. </summary>
        public TypeMetadata ParentTypeMetadata { get; }

        /// <summary> Only used for dictionary types during serialization. The metadata for the key type. </summary>
        public TypeMetadata KeyTypeMetadata { get; }

        /// <summary> Used for array, list, hashset, and dictionary types during serialization. The metadata for the item type. </summary>
        public TypeMetadata ItemTypeMetadata { get; }

        /// <summary> Only used for hashset types during deserialization. A delegate that adds an item to a hash set without having to cast it to the right type. </summary>
        public Action<object, object> HashSetAdder { get; }

        /// <summary> Only used for dictionary types. A delegate that serializes the key for a dictionary entry. </summary>
        public Action<object, SerializationContext, BinaryWriter, TypeMetadata> SerializeKey { get; }

        /// <summary>
        /// Used for array, list, hashset, and dictionary types. A delegate that serializes an item (or an entry value for a dictionary).
        /// </summary>
        public Action<object, SerializationContext, BinaryWriter, TypeMetadata> SerializeItem { get; }

        /// <summary> Used for immutable types. Serializes the object. </summary>
        public Action<object, SerializationContext, BinaryWriter, TypeMetadata> SerializeImmutable { get; }

        /// <summary> A delegate used to serialize a reference type object, or a boxed value type object. </summary>
        public Action<ObjectMetadata, SerializationContext, BinaryWriter> SerializeReferenceType { get; }

        /// <summary> Only used for dictionary types. A delegate that deserializes the key for a dictionary entry. </summary>
        public Func<DeserializationContext, BinaryReader, object> DeserializeKey { get; }

        /// <summary>
        /// Used for array, list, hashset, and dictionary types. A delegate that deserializes an item (or an entry value for a dictionary).
        /// </summary>
        public Func<DeserializationContext, BinaryReader, object> DeserializeItem { get; }

        /// <summary> Used for immutable types. Deserializes the object. </summary>
        public Func<DeserializationContext, BinaryReader, object> DeserializeImmutable { get; }

        /// <summary> A delegate used to deserialize a reference type object, or a boxed value type object. </summary>
        public Action<ObjectMetadata, DeserializationContext, BinaryReader> DeserializeReferenceType { get; }

        /// <summary> A compiled expression that creates a class or struct for deserialization. </summary>
        public Func<object> CreateClassOrStruct { get; }

        /// <summary> A compiled expression that creates a collection class for deserialization. </summary>
        public Func<int, object> CreateCollection { get; }

        /// <summary> The category for the type. Determines serialization/deserialization behaviour for a type. </summary>
        public TypeCategory Category { get; }

        /// <summary>
        /// Used for array, list, hashset, and dictionary types. The size of an item (or an entry value for a dictionary) in bytes.
        /// </summary>
        public int ItemSize { get; }

        /// <summary> Only used for dictionary types. The size of an entry key in bytes. </summary>
        public int KeySize { get; }        

        /// <summary>
        /// Only used for regular struct and class types. The size of an instance of the type in bytes, calculated by summing the sizes
        /// of each of the fields during registration. During deserialization, this value is determined by the encoded instance size value.
        /// </summary>
        public int InstanceSize { get; private set; }

        /// <summary> Options that control how objects of this type are serialized. </summary>
        public TypeSerializationSettings Settings { get; }

        /// <summary> Called during serialization. Creates new type metadata for the serialization context. </summary>
        /// <param name="registeredTypeMetadata"> The metadata for the type retrieved from the type registry. </param>
        /// <param name="id"> A unique ID for the type. </param>
        /// <param name="enumUnderlyingTypeMetadata"> For an enum type, this is the metadata for the underlying type. Otherwise null. </param>
        /// <param name="parentTypeMetadata"> The metadata for the base type. </param>
        /// <param name="keyTypeMetadata"> For a dictionary type, this is the the metadata for the key type. Otherwise null. </param>
        /// <param name="itemTypeMetadata"> For an array, list, hashset, or dictionary type, this is the metadata for the item type. Otherwise null. </param>
        public TypeMetadata(TypeMetadata registeredTypeMetadata, uint id, TypeMetadata enumUnderlyingTypeMetadata, TypeMetadata parentTypeMetadata,
                            TypeMetadata keyTypeMetadata, TypeMetadata itemTypeMetadata, List<FieldMetadata> fields, Dictionary<string, FieldMetadata> fieldsByName,
                            Dictionary<string, FieldMetadata> fieldsByPreviousName)
        {
            Id = id;
            EnumUnderlyingTypeMetadata = enumUnderlyingTypeMetadata;
            ParentTypeMetadata = parentTypeMetadata;
            KeyTypeMetadata = keyTypeMetadata;
            ItemTypeMetadata = itemTypeMetadata;
            Fields = fields;
            FieldsByName = fieldsByName;
            FieldsByPreviousName = fieldsByPreviousName;

            Type = registeredTypeMetadata.Type;
            Category = registeredTypeMetadata.Category;
            ItemSize = registeredTypeMetadata.ItemSize;
            KeySize = registeredTypeMetadata.KeySize;
            InstanceSize = registeredTypeMetadata.InstanceSize;
            SerializeKey = registeredTypeMetadata.SerializeKey;
            SerializeItem = registeredTypeMetadata.SerializeItem;
            SerializeImmutable = registeredTypeMetadata.SerializeImmutable;
            SerializeReferenceType = registeredTypeMetadata.SerializeReferenceType;
            Settings = registeredTypeMetadata.Settings;
        }

        /// <summary>
        /// Called during deserialization. Copies relevant information from the registered type metadata and filters the fields based on
        /// what is provided by the serialized data. Only for the deserialization context.
        /// </summary>
        /// <param name="registeredTypeMetadata"> The metadata for the type retrieved from the type registry. </param>
        /// <param name="fieldNames"> A list of names of serialized fields. </param>
        /// <param name="fieldSizes"> A list of sizes, in bytes, of serialized fields. </param>
        /// <param name="id"> A unique ID for the type. </param>
        /// <param name="category">
        /// The category for the type. This is necessary because the registered type may be missing, and the serializer needs
        /// to know how to skip instances of the missing type.
        /// </param>
        /// <param name="itemSize"> For array, list, hashset, and dictionary types. The size of an item in bytes. </param>
        /// <param name="keySize"> For dictionary types. The size of a key in bytes. </param>
        /// <param name="enumUnderlyingTypeMetadata"> For enum types. Metadata for the underlying type. </param>
        public TypeMetadata(TypeMetadata registeredTypeMetadata, List<string> fieldNames, List<int> fieldSizes,
            uint id, TypeMetadata parentTypeMetadata, TypeCategory category, int itemSize, int keySize, int instanceSize,
            TypeMetadata enumUnderlyingTypeMetadata)
        {
            Id = id;
            ParentTypeMetadata = parentTypeMetadata;
            Category = category;
            ItemSize = itemSize;
            KeySize = keySize;
            InstanceSize = instanceSize;
            EnumUnderlyingTypeMetadata = enumUnderlyingTypeMetadata;
            FieldsByName = null;
            FieldsByPreviousName = null;
            DeserializeReferenceType = DeserializationDelegateFactory.GetReferenceTypeOperation(category);

            if (registeredTypeMetadata == null)
            {
                Type = null;
                Fields = null;
                HashSetAdder = null;
                DeserializeKey = null;
                DeserializeItem = null;
                DeserializeImmutable = null;
                CreateClassOrStruct = null;
                CreateCollection = null;
                Settings = null;
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
                DeserializeKey = registeredTypeMetadata.DeserializeKey;
                DeserializeItem = registeredTypeMetadata.DeserializeItem;
                DeserializeImmutable = registeredTypeMetadata.DeserializeImmutable;
                CreateClassOrStruct = registeredTypeMetadata.CreateClassOrStruct;
                CreateCollection = registeredTypeMetadata.CreateCollection;
                Settings = registeredTypeMetadata.Settings;
            }
        }

        /// <summary> Called during type registration. </summary>
        /// <param name="type"> The type. </param>
        /// <param name="serializer"> The serializer. </param>
        /// <param name="settings"> Options that control how objects of this type are serialized. </param>
        public TypeMetadata(Type type, Serializer serializer, TypeSerializationSettings settings)
        {
            ParentTypeMetadata = null;
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
            CreateClassOrStruct = null;
            CreateCollection = null;
            ItemSize = 0;
            KeySize = 0;
            InstanceSize = 0;
            Id = 0;
            EnumUnderlyingTypeMetadata = null;

            Settings = settings;
            Type = type;
            Category = GetCategory(type);
            SerializeReferenceType = SerializationDelegateFactory.GetReferenceTypeOperation(Category);
            DeserializeReferenceType = DeserializationDelegateFactory.GetReferenceTypeOperation(Category);

            switch (Category)
            {
                case TypeCategory.Immutable:
                    {
                        SerializeImmutable = SerializationDelegateFactory.GetImmutableOperation(type);
                        DeserializeImmutable = DeserializationDelegateFactory.GetImmutableOperation(type);
                        break;
                    }
                case TypeCategory.Array:
                    {
                        Type elementType = type.GetElementType();
                        SerializeItem = SerializationDelegateFactory.GetFieldOperation(elementType);
                        DeserializeItem = DeserializationDelegateFactory.GetFieldOperation(elementType);
                        ItemSize = FieldSizeFactory.GetFieldSize(elementType, serializer.TypeRegistry);
                        break;
                    }
                case TypeCategory.List:
                    {
                        Type itemType = type.GenericTypeArguments[0];
                        SerializeItem = SerializationDelegateFactory.GetFieldOperation(itemType);
                        DeserializeItem = DeserializationDelegateFactory.GetFieldOperation(itemType);
                        ItemSize = FieldSizeFactory.GetFieldSize(itemType, serializer.TypeRegistry);
                        CreateCollection = BuildCollectionCreator();
                        break;
                    }
                case TypeCategory.HashSet:
                    {
                        Type itemType = type.GenericTypeArguments[0];
                        SerializeItem = SerializationDelegateFactory.GetFieldOperation(itemType);
                        DeserializeItem = DeserializationDelegateFactory.GetFieldOperation(itemType);
                        ItemSize = FieldSizeFactory.GetFieldSize(itemType, serializer.TypeRegistry);
                        HashSetAdder = BuildHashSetAdder();
                        CreateCollection = BuildCollectionCreator();
                        break;
                    }
                case TypeCategory.Dictionary:
                    {
                        Type keyType = type.GenericTypeArguments[0];
                        SerializeKey = SerializationDelegateFactory.GetFieldOperation(keyType);
                        DeserializeKey = DeserializationDelegateFactory.GetFieldOperation(keyType);
                        KeySize = FieldSizeFactory.GetFieldSize(keyType, serializer.TypeRegistry);
                        Type valueType = type.GenericTypeArguments[1];
                        SerializeItem = SerializationDelegateFactory.GetFieldOperation(valueType);
                        DeserializeItem = DeserializationDelegateFactory.GetFieldOperation(valueType);
                        ItemSize = FieldSizeFactory.GetFieldSize(valueType, serializer.TypeRegistry);
                        CreateCollection = BuildCollectionCreator();
                        break;
                    }
                case TypeCategory.Struct:
                    {
                        PopulateFields(serializer);
                        PopulateSize();
                        CreateClassOrStruct = BuildClassOrStructCreator();
                        break;
                    }
                case TypeCategory.Class:
                    {
                        PopulateFields(serializer);
                        PopulateSize();
                        if (!Type.IsAbstract)
                            CreateClassOrStruct = BuildClassOrStructCreator();
                        break;
                    }
                case TypeCategory.Enum:
                case TypeCategory.Type:
                    {
                        break;
                    }
                default:
                    throw new IcepackException($"Invalid type category: {Category}");
            }
        }

        /// <summary> Determines the category for a given type. </summary>
        /// <param name="type"> The type. </param>
        /// <returns> The category for the type. </returns>
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

        /// <summary> Used for regular struct and class types. Builds the metadata for the fields. </summary>
        /// <param name="typeRegistry"> The serializer's type registry. </param>
        private void PopulateFields(Serializer serializer)
        {
            foreach (FieldInfo fieldInfo in Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (Utils.IsUnsupportedType(fieldInfo.FieldType))
                    continue;

                if (serializer.Settings.SerializeByDefault)
                {
                    IgnoreFieldAttribute ignoreAttr = fieldInfo.GetCustomAttribute<IgnoreFieldAttribute>();
                    if (ignoreAttr != null)
                        continue;
                }
                else
                {
                    SerializableFieldAttribute serializableFieldAttr = fieldInfo.GetCustomAttribute<SerializableFieldAttribute>();
                    if (serializableFieldAttr == null)
                        continue;
                }

                var fieldMetadata = new FieldMetadata(fieldInfo, serializer);
                FieldsByName.Add(fieldInfo.Name, fieldMetadata);

                PreviousNameAttribute previousNameAttr = fieldInfo.GetCustomAttribute<PreviousNameAttribute>();
                if (previousNameAttr != null)
                    FieldsByPreviousName.Add(previousNameAttr.Name, fieldMetadata);

                Fields.Add(fieldMetadata);
            }
        }

        /// <summary> Used for regular struct and class types. Populates the instance size for the type. </summary>
        private void PopulateSize()
        {
            int size = 0;
            for (int i = 0; i < Fields.Count; i++)
            {
                FieldMetadata fieldMetadata = Fields[i];
                size += fieldMetadata.Size;
            }

            InstanceSize = size;
        }

        /// <summary> Builds the delegate used to add items to a hashset. </summary>
        /// <returns> The delegate. </returns>
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
            Action<object, object> compiled = lambda.Compile();

            return compiled;
        }

        /// <summary> Builds the delegate used to create new instances of a type. </summary>
        /// <returns> The delegate. </returns>
        private Func<object> BuildClassOrStructCreator()
        {
            NewExpression exNew;
            try
            {
                exNew = Expression.New(Type);
            }
            catch (ArgumentException)
            {
                throw new IcepackException($"Type '{Type}' does not have a default constructor!");
            }

            UnaryExpression exConvert = Expression.Convert(exNew, typeof(object));
            Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(exConvert);
            var compiled = lambda.Compile();

            return compiled;
        }

        /// <summary> Builds the delegate used to create new instances of a collection type. </summary>
        /// <returns> The delegate. </returns>
        private Func<int, object> BuildCollectionCreator()
        {
            ConstructorInfo constructor = Type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(int) }, null);
            ParameterExpression exParam = Expression.Parameter(typeof(int));
            NewExpression exNew = Expression.New(constructor, exParam);
            UnaryExpression exConvert = Expression.Convert(exNew, typeof(object));
            Expression<Func<int, object>> lambda = Expression.Lambda<Func<int, object>>(exConvert, exParam);
            var compiled = lambda.Compile();

            return compiled;
        }
    }
}
