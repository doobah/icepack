using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;

namespace Icepack
{
    /// <summary> Returns delegates for serializing types and fields. </summary>
    internal static class SerializationDelegateFactory
    {
        private static void SerializeString(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((string)obj!);
        }

        private static void SerializeByte(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((byte)obj!);
        }

        private static void SerializeSByte(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((sbyte)obj!);
        }

        private static void SerializeBool(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((bool)obj!);
        }

        private static void SerializeChar(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((char)obj!);
        }

        private static void SerializeInt16(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((short)obj!);
        }

        private static void SerializeUInt16(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((ushort)obj!);
        }

        private static void SerializeInt32(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((int)obj!);
        }

        private static void SerializeUInt32(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((uint)obj!);
        }

        private static void SerializeInt64(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((long)obj!);
        }

        private static void SerializeUInt64(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((ulong)obj!);
        }

        private static void SerializeSingle(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((float)obj!);
        }

        private static void SerializeDouble(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((double)obj!);
        }

        private static void SerializeDecimal(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            writer.Write((decimal)obj!);
        }

        private static void SerializeObjectReference(object? value, SerializationContext context, BinaryWriter writer, TypeMetadata? _)
        {
            uint id = context.RegisterObject(value);
            writer.Write(id);
        }

        private static void SerializeBoxedStruct(ObjectMetadata objectMetadata, SerializationContext context, BinaryWriter writer)
        {
            TypeMetadata typeMetadata = objectMetadata.TypeMetadata;
            object obj = objectMetadata.Value!;

            if (obj is ISerializerListener listener)
                listener.OnBeforeSerialize();

            writer.Write(typeMetadata.Id);

            for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields!.Count; fieldIdx++)
            {
                FieldMetadata field = typeMetadata.Fields[fieldIdx];
                object value = field.Getter!(obj);
                field.Serialize!(value, context, writer, field.TypeMetadata);
            }
        }

        /// <summary> Serializes a struct field. </summary>
        private static void SerializeStruct(object? obj, SerializationContext context, BinaryWriter writer, TypeMetadata? typeMetadata)
        {
            if (obj is ISerializerListener listener)
                listener.OnBeforeSerialize();

            // This will be set if the struct is a key or item type.
            if (typeMetadata == null)
            {
                Type type = obj!.GetType();
                typeMetadata = context.GetTypeMetadata(type);
            }

            writer.Write(typeMetadata.Id);

            for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields!.Count; fieldIdx++)
            {
                FieldMetadata field = typeMetadata.Fields[fieldIdx];
                object value = field.Getter!(obj!);
                field.Serialize!(value, context, writer, field.TypeMetadata);
            }
        }

        private static void SerializeArray(ObjectMetadata objectMetadata, SerializationContext context, BinaryWriter writer)
        {
            Array array = (Array)objectMetadata.Value!;
            TypeMetadata typeMetadata = objectMetadata.TypeMetadata;

            for (int arrayIdx = 0; arrayIdx < array.Length; arrayIdx++)
            {
                object? item = array.GetValue(arrayIdx);
                typeMetadata.SerializeItem!(item, context, writer, typeMetadata.ItemTypeMetadata);
            }
        }

        private static void SerializeList(ObjectMetadata objectMetadata, SerializationContext context, BinaryWriter writer)
        {
            IList list = (IList)objectMetadata.Value!;
            TypeMetadata typeMetadata = objectMetadata.TypeMetadata;

            for (int itemIdx = 0; itemIdx < list.Count; itemIdx++)
            {
                object? item = list[itemIdx];
                typeMetadata.SerializeItem!(item, context, writer, typeMetadata.ItemTypeMetadata);
            }
        }

        private static void SerializeHashSet(ObjectMetadata objectMetadata, SerializationContext context, BinaryWriter writer)
        {
            IEnumerable set = (IEnumerable)objectMetadata.Value!;
            TypeMetadata typeMetadata = objectMetadata.TypeMetadata;

            foreach (object item in set)
                typeMetadata.SerializeItem!(item, context, writer, typeMetadata.ItemTypeMetadata);
        }

        private static void SerializeDictionary(ObjectMetadata objectMetadata, SerializationContext context, BinaryWriter writer)
        {
            IDictionary dict = (IDictionary)objectMetadata.Value!;
            TypeMetadata typeMetadata = objectMetadata.TypeMetadata;

            foreach (DictionaryEntry entry in dict)
            {
                typeMetadata.SerializeKey!(entry.Key, context, writer, typeMetadata.KeyTypeMetadata);
                typeMetadata.SerializeItem!(entry.Value, context, writer, typeMetadata.ItemTypeMetadata);
            }
        }

        private static void SerializeNormalClass(ObjectMetadata objectMetadata, SerializationContext context, BinaryWriter writer)
        {
            object obj = objectMetadata.Value!;
            TypeMetadata typeMetadata = objectMetadata.TypeMetadata;

            if (obj is ISerializerListener listener)
                listener.OnBeforeSerialize();

            while (true)
            {
                for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields!.Count; fieldIdx++)
                {
                    FieldMetadata field = typeMetadata.Fields[fieldIdx];
                    object value = field.Getter!(obj);
                    field.Serialize!(value, context, writer, field.TypeMetadata);
                }

                Type? parentType = typeMetadata.Type!.BaseType;
                if (parentType == null)
                    break;

                // This will throw an exception if the parent type does not exist
                typeMetadata = context.GetTypeMetadata(parentType);
            }
        }

        private static void SerializeBoxedImmutable(ObjectMetadata objectMetadata, SerializationContext context, BinaryWriter writer)
        {
            // Value is serialized as metadata
        }

        private static void SerializeBoxedEnum(ObjectMetadata objectMetadata, SerializationContext context, BinaryWriter writer)
        {
            // Value is serialized as metadata
        }

        private static void SerializeType(ObjectMetadata objectMetadata, SerializationContext context, BinaryWriter writer)
        {
            // Value is serialized as metadata but we should make sure the type is added to the context first
            context.GetTypeMetadata((Type)objectMetadata.Value!);
        }

        /// <summary> Returns a delegate which is used to serialize instances of the given type category. </summary>
        /// <param name="category"> The type category. </param>
        /// <returns> The serialization delegate. </returns>
        public static Action<ObjectMetadata, SerializationContext, BinaryWriter> GetReferenceTypeOperation(TypeCategory category)
        {
            switch (category)
            {
                case TypeCategory.Immutable:
                    return SerializeBoxedImmutable;
                case TypeCategory.Array:
                    return SerializeArray;
                case TypeCategory.List:
                    return SerializeList;
                case TypeCategory.HashSet:
                    return SerializeHashSet;
                case TypeCategory.Dictionary:
                    return SerializeDictionary;
                case TypeCategory.Struct:
                    return SerializeBoxedStruct;
                case TypeCategory.Class:
                    return SerializeNormalClass;
                case TypeCategory.Enum:
                    return SerializeBoxedEnum;
                case TypeCategory.Type:
                    return SerializeType;
                default:
                    throw new IcepackException($"Unexpected type category: {category}");
            }
        }

        private static Action<object?, SerializationContext, BinaryWriter, TypeMetadata?> GetEnumFieldOperation(Type type)
        {
            Type underlyingType = Enum.GetUnderlyingType(type);
            if (underlyingType == typeof(byte))
                return SerializeByte;
            else if (underlyingType == typeof(sbyte))
                return SerializeSByte;
            else if (underlyingType == typeof(short))
                return SerializeInt16;
            else if (underlyingType == typeof(ushort))
                return SerializeUInt16;
            else if (underlyingType == typeof(int))
                return SerializeInt32;
            else if (underlyingType == typeof(uint))
                return SerializeUInt32;
            else if (underlyingType == typeof(long))
                return SerializeInt64;
            else if (underlyingType == typeof(ulong))
                return SerializeUInt64;
            else
                throw new IcepackException($"Unexpected enum type: {type}");
        }

        /// <summary> Returns the delegate used to serialize fields of the given type. </summary>
        /// <param name="type"> The field's declaring type. </param>
        /// <returns> The serialization delegate. </returns>
        public static Action<object?, SerializationContext, BinaryWriter, TypeMetadata?> GetFieldOperation(Type type)
        {
            if (type == typeof(byte))
                return SerializeByte;
            else if (type == typeof(sbyte))
                return SerializeSByte;
            else if (type == typeof(bool))
                return SerializeBool;
            else if (type == typeof(char))
                return SerializeChar;
            else if (type == typeof(short))
                return SerializeInt16;
            else if (type == typeof(ushort))
                return SerializeUInt16;
            else if (type == typeof(int))
                return SerializeInt32;
            else if (type == typeof(uint))
                return SerializeUInt32;
            else if (type == typeof(long))
                return SerializeInt64;
            else if (type == typeof(ulong))
                return SerializeUInt64;
            else if (type == typeof(float))
                return SerializeSingle;
            else if (type == typeof(double))
                return SerializeDouble;
            else if (type == typeof(decimal))
                return SerializeDecimal;
            else if (type.IsEnum)
                return GetEnumFieldOperation(type);
            else if (type.IsValueType)
                return SerializeStruct;
            else if (type.IsClass || type.IsInterface)
                return SerializeObjectReference;
            else
                throw new IcepackException($"Unexpected field type: {type}");
        }

        /// <summary> Returns the delegate used to serialize immutable types. </summary>
        /// <param name="type"> The type. </param>
        /// <returns> The serialization delegate. </returns>
        public static Action<object?, SerializationContext, BinaryWriter, TypeMetadata?> GetImmutableOperation(Type type)
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
    }
}
