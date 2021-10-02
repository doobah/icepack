using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using System.IO;

namespace Icepack
{
    internal enum Category : byte
    {
        Basic = 0,
        Array = 1,
        List = 2,
        HashSet = 3,
        Dictionary = 4,
        Struct = 5,
        Class = 6,
        Enum = 7,
        Type = 8
    }

    /// <summary> Contains information necessary to serialize and deserialize a type. </summary>
    internal class TypeMetadata
    {
        public uint Id { get; }
        public Type Type { get; }
        public TypeMetadata EnumUnderlyingTypeMetadata { get; }
        public SortedList<string, FieldMetadata> Fields { get; }
        public bool HasParent { get; }
        public Action<object, object> HashSetAdder { get; }
        public Action<object, SerializationContext> SerializeKey { get; }
        public Action<object, SerializationContext> SerializeItem { get; }
        public Action<object, BinaryWriter, SerializationContext> SerializeBasic { get; }
        public Func<DeserializationContext, object> DeserializeKey { get; }
        public Func<DeserializationContext, object> DeserializeItem { get; }
        public Func<DeserializationContext, object> DeserializeBasic { get; }
        public Category CategoryId { get; }
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
            CategoryId = registeredTypeMetadata.CategoryId;
            ItemSize = registeredTypeMetadata.ItemSize;
            KeySize = registeredTypeMetadata.KeySize;
            InstanceSize = registeredTypeMetadata.InstanceSize;
            HashSetAdder = registeredTypeMetadata.HashSetAdder;
            SerializeKey = registeredTypeMetadata.SerializeKey;
            SerializeItem = registeredTypeMetadata.SerializeItem;
            SerializeBasic = registeredTypeMetadata.SerializeBasic;
            DeserializeKey = registeredTypeMetadata.DeserializeKey;
            DeserializeItem = registeredTypeMetadata.DeserializeItem;
            DeserializeBasic = registeredTypeMetadata.DeserializeBasic;
        }

        /// <summary>
        /// Called during deserialization. Copies relevant information from the registered type metadata and filters the fields based on
        /// what is expected by the serialized data.
        /// </summary>
        /// <param name="registeredTypeMetadata"> The registered type metadata to copy information from. </param>
        /// <param name="objectTree"> The object tree for type metadata extracted from the serialized data. </param>
        /// <param name="id"> A unique ID for the type. </param>
        public TypeMetadata(TypeMetadata registeredTypeMetadata, List<string> fieldNames, List<int> fieldSizes,
            uint id, bool hasParent, Category categoryId, int itemSize, int keySize, int instanceSize, TypeMetadata enumUnderlyingTypeMetadata)
        {
            Id = id;
            HasParent = hasParent;
            CategoryId = categoryId;
            ItemSize = itemSize;
            KeySize = keySize;
            InstanceSize = instanceSize;
            EnumUnderlyingTypeMetadata = enumUnderlyingTypeMetadata;

            if (registeredTypeMetadata == null)
            {
                Type = null;
                Fields = null;
                HashSetAdder = null;
                SerializeKey = null;
                SerializeItem = null;
                SerializeBasic = null;
                DeserializeKey = null;
                DeserializeItem = null;
                DeserializeBasic = null;
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
                SerializeBasic = registeredTypeMetadata.SerializeBasic;
                DeserializeKey = registeredTypeMetadata.DeserializeKey;
                DeserializeItem = registeredTypeMetadata.DeserializeItem;
                DeserializeBasic = registeredTypeMetadata.DeserializeBasic;
            }
        }

        /// <summary> Called during type registration. </summary>
        /// <param name="type"> The type. </param>
        public TypeMetadata(Type type, TypeRegistry typeRegistry)
        {
            HasParent = false;
            Fields = new SortedList<string, FieldMetadata>();
            HashSetAdder = null;
            SerializeKey = null;
            SerializeBasic = null;
            DeserializeKey = null;
            SerializeItem = null;
            DeserializeItem = null;
            DeserializeBasic = null;
            CategoryId = 0;
            ItemSize = 0;
            KeySize = 0;
            InstanceSize = 0;
            Id = 0;
            EnumUnderlyingTypeMetadata = null;

            Type = type;
            CategoryId = GetCategory(type);

            switch (CategoryId)
            {
                case Category.Basic:
                    {
                        SerializeBasic = GetSerializeBasicOperation(type);
                        DeserializeBasic = GetDeserializeBasicOperation(type);
                        break;
                    }
                case Category.Array:
                    {
                        Type elementType = type.GetElementType();
                        SerializeItem = SerializationOperationFactory.GetFieldOperation(elementType);
                        DeserializeItem = DeserializationOperationFactory.GetFieldOperation(elementType);
                        ItemSize = TypeSizeFactory.GetFieldSize(elementType, typeRegistry);
                        break;
                    }
                case Category.List:
                    {
                        Type itemType = type.GenericTypeArguments[0];
                        SerializeItem = SerializationOperationFactory.GetFieldOperation(itemType);
                        DeserializeItem = DeserializationOperationFactory.GetFieldOperation(itemType);
                        ItemSize = TypeSizeFactory.GetFieldSize(itemType, typeRegistry);
                        break;
                    }
                case Category.HashSet:
                    {
                        Type itemType = type.GenericTypeArguments[0];
                        SerializeItem = SerializationOperationFactory.GetFieldOperation(itemType);
                        DeserializeItem = DeserializationOperationFactory.GetFieldOperation(itemType);
                        ItemSize = TypeSizeFactory.GetFieldSize(itemType, typeRegistry);
                        HashSetAdder = BuildHashSetAdder();
                        break;
                    }
                case Category.Dictionary:
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
                case Category.Struct:
                    {
                        PopulateFields(typeRegistry);
                        InstanceSize = CalculateSize();
                        break;
                    }
                case Category.Class:
                    {
                        HasParent = Type.BaseType != typeof(object);
                        PopulateFields(typeRegistry);
                        InstanceSize = CalculateSize();
                        break;
                    }
                case Category.Enum:
                    {
                        EnumUnderlyingTypeMetadata = typeRegistry.GetTypeMetadata(Type.GetEnumUnderlyingType());
                        break;
                    }
                case Category.Type:
                    {
                        break;
                    }
                default:
                    throw new IcepackException($"Invalid category ID: {CategoryId}");
            }
        }

        private static Category GetCategory(Type type)
        {
            if (type == typeof(string) || type.IsPrimitive || type == typeof(decimal))
                return Category.Basic;
            else if (type == typeof(Type))
                return Category.Type;
            else if (type.IsEnum)
                return Category.Enum;
            else if (type.IsArray)
                return Category.Array;
            else if (type.IsGenericType)
            {
                Type genericTypeDef = type.GetGenericTypeDefinition();

                if (genericTypeDef == typeof(List<>))
                    return Category.List;
                else if (genericTypeDef == typeof(HashSet<>))
                    return Category.HashSet;
                else if (genericTypeDef == typeof(Dictionary<,>))
                    return Category.Dictionary;
                else if (type.IsValueType)
                    return Category.Struct;
                else
                    return Category.Class;
            }
            else if (type.IsValueType)
                return Category.Struct;
            else
                return Category.Class;
        }

        private static void SerializeString(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((string)obj);
        }

        private static void SerializeByte(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((byte)obj);
        }

        private static void SerializeSByte(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((sbyte)obj);
        }

        private static void SerializeChar(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((char)obj);
        }

        private static void SerializeBool(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((bool)obj);
        }

        private static void SerializeInt32(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((int)obj);
        }

        private static void SerializeUInt32(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((uint)obj);
        }

        private static void SerializeInt16(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((short)obj);
        }

        private static void SerializeUInt16(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((ushort)obj);
        }

        private static void SerializeInt64(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((long)obj);
        }

        private static void SerializeUInt64(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((ulong)obj);
        }

        private static void SerializeDecimal(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((decimal)obj);
        }

        private static void SerializeSingle(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((float)obj);
        }

        private static void SerializeDouble(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write((double)obj);
        }

        private static void SerializeType(object obj, BinaryWriter writer, SerializationContext context)
        {
            writer.Write(context.GetTypeMetadata((Type)obj).Id);
        }

        private static Action<object, BinaryWriter, SerializationContext> GetSerializeBasicOperation(Type type)
        {
            if (type == typeof(string))
                return SerializeString;
            else if (type == typeof(byte))
                return SerializeByte;
            else if (type == typeof(sbyte))
                return SerializeSByte;
            else if (type == typeof(char))
                return SerializeChar;
            else if (type == typeof(bool))
                return SerializeBool;
            else if (type == typeof(int))
                return SerializeInt32;
            else if (type == typeof(uint))
                return SerializeUInt32;
            else if (type == typeof(short))
                return SerializeInt16;
            else if (type == typeof(ushort))
                return SerializeUInt16;
            else if (type == typeof(long))
                return SerializeInt64;
            else if (type == typeof(ulong))
                return SerializeUInt64;
            else if (type == typeof(decimal))
                return SerializeDecimal;
            else if (type == typeof(float))
                return SerializeSingle;
            else if (type == typeof(double))
                return SerializeDouble;
            else
                throw new IcepackException($"Unexpected type: {type}");
        }

        private static Func<DeserializationContext, object> GetDeserializeBasicOperation(Type type)
        {
            if (type == typeof(string))
                return DeserializationOperationFactory.DeserializeString;
            else if (type == typeof(byte))
                return DeserializationOperationFactory.DeserializeByte;
            else if (type == typeof(sbyte))
                return DeserializationOperationFactory.DeserializeSByte;
            else if (type == typeof(char))
                return DeserializationOperationFactory.DeserializeChar;
            else if (type == typeof(bool))
                return DeserializationOperationFactory.DeserializeBoolean;
            else if (type == typeof(int))
                return DeserializationOperationFactory.DeserializeInt32;
            else if (type == typeof(uint))
                return DeserializationOperationFactory.DeserializeUInt32;
            else if (type == typeof(short))
                return DeserializationOperationFactory.DeserializeInt16;
            else if (type == typeof(ushort))
                return DeserializationOperationFactory.DeserializeUInt16;
            else if (type == typeof(long))
                return DeserializationOperationFactory.DeserializeInt64;
            else if (type == typeof(ulong))
                return DeserializationOperationFactory.DeserializeUInt64;
            else if (type == typeof(decimal))
                return DeserializationOperationFactory.DeserializeDecimal;
            else if (type == typeof(float))
                return DeserializationOperationFactory.DeserializeSingle;
            else if (type == typeof(double))
                return DeserializationOperationFactory.DeserializeDouble;
            else
                throw new IcepackException($"Unexpected type: {type}");
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
