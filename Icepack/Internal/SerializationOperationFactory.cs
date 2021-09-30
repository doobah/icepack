using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    internal static class SerializationOperationFactory
    {
        public static void SerializeByte(object obj, SerializationContext context)
        {
            context.Writer.Write((byte)obj);
        }

        public static void SerializeSByte(object obj, SerializationContext context)
        {
            context.Writer.Write((sbyte)obj);
        }

        public static void SerializeBool(object obj, SerializationContext context)
        {
            context.Writer.Write((bool)obj);
        }

        public static void SerializeChar(object obj, SerializationContext context)
        {
            context.Writer.Write((char)obj);
        }

        public static void SerializeInt16(object obj, SerializationContext context)
        {
            context.Writer.Write((short)obj);
        }

        public static void SerializeUInt16(object obj, SerializationContext context)
        {
            context.Writer.Write((ushort)obj);
        }

        public static void SerializeInt32(object obj, SerializationContext context)
        {
            context.Writer.Write((int)obj);
        }

        public static void SerializeUInt32(object obj, SerializationContext context)
        {
            context.Writer.Write((uint)obj);
        }

        public static void SerializeInt64(object obj, SerializationContext context)
        {
            context.Writer.Write((long)obj);
        }

        public static void SerializeUInt64(object obj, SerializationContext context)
        {
            context.Writer.Write((ulong)obj);
        }

        public static void SerializeSingle(object obj, SerializationContext context)
        {
            context.Writer.Write((float)obj);
        }

        public static void SerializeDouble(object obj, SerializationContext context)
        {
            context.Writer.Write((double)obj);
        }

        public static void SerializeDecimal(object obj, SerializationContext context)
        {
            context.Writer.Write((decimal)obj);
        }

        public static void SerializeObjectReference(object value, SerializationContext context)
        {
            if (value == null)
                context.Writer.Write((uint)0);
            else if (context.Objects.ContainsKey(value))
                context.Writer.Write(context.Objects[value].Id);
            else
            {
                uint id = context.RegisterObject(value);
                context.ObjectsToSerialize.Enqueue(value);
                context.Writer.Write(id);
            }
        }

        public static void SerializeStruct(object obj, SerializationContext context)
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
                field.Serialize(value, context);
            }
        }

        public static void SerializeArray(object obj, TypeMetadata typeMetadata, SerializationContext context)
        {
            Array array = (Array)obj;

            for (int arrayIdx = 0; arrayIdx < array.Length; arrayIdx++)
            {
                object item = array.GetValue(arrayIdx);
                typeMetadata.SerializeItem(item, context);
            }
        }

        public static void SerializeList(object obj, TypeMetadata typeMetadata, SerializationContext context)
        {
            IList list = (IList)obj;

            for (int itemIdx = 0; itemIdx < list.Count; itemIdx++)
            {
                object item = list[itemIdx];
                typeMetadata.SerializeItem(item, context);
            }
        }

        public static void SerializeHashSet(object obj, TypeMetadata typeMetadata, SerializationContext context)
        {
            IEnumerable set = (IEnumerable)obj;

            foreach (object item in set)
                typeMetadata.SerializeItem(item, context);
        }

        public static void SerializeDictionary(object obj, TypeMetadata typeMetadata, SerializationContext context)
        {
            IDictionary dict = (IDictionary)obj;

            foreach (DictionaryEntry entry in dict)
            {
                typeMetadata.SerializeKey(entry.Key, context);
                typeMetadata.SerializeItem(entry.Value, context);
            }
        }

        public static void SerializeNormalClass(object obj, TypeMetadata typeMetadata, SerializationContext context)
        {
            context.Writer.Write(typeMetadata.Id);

            for (int fieldIdx = 0; fieldIdx < typeMetadata.Fields.Count; fieldIdx++)
            {
                FieldMetadata field = typeMetadata.Fields.Values[fieldIdx];
                object value = field.Getter(obj);
                field.Serialize(value, context);
            }

            Type parentType = typeMetadata.Type.BaseType;
            if (parentType != typeof(object))
            {
                TypeMetadata parentTypeMetadata = context.GetTypeMetadata(parentType);
                if (parentTypeMetadata != null)
                    SerializeNormalClass(obj, parentTypeMetadata, context);
            }
        }

        public static void SerializeReferenceType(object obj, SerializationContext context)
        {
            if (obj is ISerializerListener)
                ((ISerializerListener)obj).OnBeforeSerialize();

            Type type = obj.GetType();
            TypeMetadata typeMetadata = context.GetTypeMetadata(type);

            switch (typeMetadata.CategoryId)
            {
                case 0: // String
                    break;
                case 1: // Array
                    SerializeArray(obj, typeMetadata, context);
                    break;
                case 2: // List
                    SerializeList(obj, typeMetadata, context);
                    break;
                case 3: // HashSet
                    SerializeHashSet(obj, typeMetadata, context);
                    break;
                case 4: // Dictionary
                    SerializeDictionary(obj, typeMetadata, context);
                    break;
                case 5: // Struct
                    throw new IcepackException($"Unexpected category ID: {typeMetadata.CategoryId}");
                case 6: // Class
                    SerializeNormalClass(obj, typeMetadata, context);
                    break;
                default:
                    throw new IcepackException($"Invalid category ID: {typeMetadata.CategoryId}");
            }
        }

        public static Action<object, SerializationContext> GetEnumOperation(Type type)
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
                throw new IcepackException($"Invalid enum type: {type}");
        }

        public static Action<object, SerializationContext> GetOperation(Type type)
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
                return GetEnumOperation(type);
            else if (type.IsValueType)
                return SerializeStruct;
            else if (type.IsClass || type.IsInterface)
                return SerializeObjectReference;
            else
                throw new IcepackException($"Unable to serialize object of type: {type}");
        }
    }
}
