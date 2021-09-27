using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    internal static class DeserializationOperationFactory
    {
        public static object DeserializeByte(DeserializationContext context)
        {
            return context.Reader.ReadByte();
        }

        public static object DeserializeSByte(DeserializationContext context)
        {
            return context.Reader.ReadSByte();
        }

        public static object DeserializeChar(DeserializationContext context)
        {
            return context.Reader.ReadChar();
        }

        public static object DeserializeBoolean(DeserializationContext context)
        {
            return context.Reader.ReadBoolean();
        }

        public static object DeserializeInt32(DeserializationContext context)
        {
            return context.Reader.ReadInt32();
        }

        public static object DeserializeUInt32(DeserializationContext context)
        {
            return context.Reader.ReadUInt32();
        }

        public static object DeserializeInt16(DeserializationContext context)
        {
            return context.Reader.ReadInt16();
        }

        public static object DeserializeUInt16(DeserializationContext context)
        {
            return context.Reader.ReadUInt16();
        }

        public static object DeserializeInt64(DeserializationContext context)
        {
            return context.Reader.ReadInt64();
        }

        public static object DeserializeUInt64(DeserializationContext context)
        {
            return context.Reader.ReadUInt64();
        }

        public static object DeserializeDecimal(DeserializationContext context)
        {
            return context.Reader.ReadDecimal();
        }

        public static object DeserializeSingle(DeserializationContext context)
        {
            return context.Reader.ReadSingle();
        }

        public static object DeserializeDouble(DeserializationContext context)
        {
            return context.Reader.ReadDouble();
        }

        public static object DeserializeString(DeserializationContext context)
        {
            return context.Reader.ReadString();
        }

        public static object DeserializeStruct(DeserializationContext context)
        {
            uint typeId = context.Reader.ReadUInt32();
            TypeMetadata typeMetadata = context.Types[typeId - 1];
            object structObj = Activator.CreateInstance(typeMetadata.Type);

            for (int i = 0; i < typeMetadata.Fields.Count; i++)
            {
                FieldMetadata field = typeMetadata.Fields.Values[i];
                if (field != null)
                {
                    Type fieldType = field.FieldInfo.FieldType;
                    object value = field.Deserialize(context);
                    field.Setter(structObj, value);
                }
            }

            if (structObj is ISerializerListener)
                ((ISerializerListener)structObj).OnAfterDeserialize();

            return structObj;
        }

        public static void DeserializeArray(object classObj, TypeMetadata classTypeMetadata, DeserializationContext context)
        {
            Array arrayObj = (Array)classObj;
            Type elementType = classTypeMetadata.Type.GetElementType();

            int arrayLength = context.Reader.ReadInt32();

            for (int i = 0; i < arrayLength; i++)
            {
                object value = classTypeMetadata.DeserializeItem(context);
                arrayObj.SetValue(value, i);
            }
        }

        public static void DeserializeList(object classObj, TypeMetadata classTypeMetadata, DeserializationContext context)
        {
            IList listObj = (IList)classObj;
            Type elementType = classTypeMetadata.Type.GenericTypeArguments[0];

            int length = context.Reader.ReadInt32();
            for (int i = 0; i < length; i++)
            {
                object value = classTypeMetadata.DeserializeItem(context);
                listObj.Add(value);
            }
        }

        public static void DeserializeHashSet(object classObj, TypeMetadata classTypeMetadata, DeserializationContext context)
        {
            Type elementType = classTypeMetadata.Type.GenericTypeArguments[0];

            int length = context.Reader.ReadInt32();
            for (int i = 0; i < length; i++)
            {
                object value = classTypeMetadata.DeserializeItem(context);
                classTypeMetadata.HashSetAdder(classObj, value);
            }
        }

        public static void DeserializeDictionary(object classObj, TypeMetadata classTypeMetadata, DeserializationContext context)
        {
            IDictionary dictObj = (IDictionary)classObj;
            Type keyType = classTypeMetadata.Type.GenericTypeArguments[0];
            Type valueType = classTypeMetadata.Type.GenericTypeArguments[1];
            int length = context.Reader.ReadInt32();

            for (int i = 0; i < length; i++)
            {
                object key = classTypeMetadata.DeserializeKey(context);
                object value = classTypeMetadata.DeserializeItem(context);
                dictObj.Add(key, value);
            }
        }

        public static void DeserializeNormalClass(object classObj, DeserializationContext context)
        {
            while (true)
            {
                uint partialClassTypeId = context.Reader.ReadUInt32();
                TypeMetadata partialClassTypeMetadata = context.Types[partialClassTypeId - 1];
                for (int fieldIdx = 0; fieldIdx < partialClassTypeMetadata.Fields.Count; fieldIdx++)
                {
                    FieldMetadata field = partialClassTypeMetadata.Fields.Values[fieldIdx];
                    if (field != null)
                    {
                        Type fieldType = field.FieldInfo.FieldType;
                        object value = field.Deserialize(context);
                        field.Setter(classObj, value);
                    }
                }

                if (!partialClassTypeMetadata.HasParent)
                    break;
            }

            if (classObj is ISerializerListener)
                ((ISerializerListener)classObj).OnAfterDeserialize();
        }

        public static void DeserializeClass(object classObj, DeserializationContext context)
        {
            uint typeId = context.Reader.ReadUInt32();
            TypeMetadata classTypeMetadata = context.Types[typeId - 1];
            Type classType = classTypeMetadata.Type;

            if (classType == typeof(string))
                return;                 // String is already deserialized as metadata
            if (classType.IsArray)
                DeserializeArray(classObj, classTypeMetadata, context);
            else if (classType.IsGenericType && classType.GetGenericTypeDefinition() == typeof(List<>))
                DeserializeList(classObj, classTypeMetadata, context);
            else if (classType.IsGenericType && classType.GetGenericTypeDefinition() == typeof(HashSet<>))
                DeserializeHashSet(classObj, classTypeMetadata, context);
            else if (classType.IsGenericType && classType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                DeserializeDictionary(classObj, classTypeMetadata, context);
            else
                DeserializeNormalClass(classObj, context);
        }

        public static object DeserializeObjectReference(DeserializationContext context)
        {
            uint objId = context.Reader.ReadUInt32();
            return (objId == 0) ? null : context.Objects[objId - 1];
        }

        public static Func<DeserializationContext, object> GetEnumOperation(Type type)
        {
            Type underlyingType = type.GetEnumUnderlyingType();

            if (underlyingType == typeof(byte))
                return DeserializeByte;
            else if (underlyingType == typeof(sbyte))
                return DeserializeSByte;
            else if (underlyingType == typeof(short))
                return DeserializeInt16;
            else if (underlyingType == typeof(ushort))
                return DeserializeUInt16;
            else if (underlyingType == typeof(int))
                return DeserializeInt32;
            else if (underlyingType == typeof(uint))
                return DeserializeUInt32;
            else if (underlyingType == typeof(long))
                return DeserializeInt64;
            else if (underlyingType == typeof(ulong))
                return DeserializeUInt64;
            else
                throw new IcepackException($"Invalid enum type: {type}");
        }

        public static Func<DeserializationContext, object> GetOperation(Type type)
        {
            if (type == typeof(byte))
                return DeserializeByte;
            else if (type == typeof(sbyte))
                return DeserializeSByte;
            else if (type == typeof(char))
                return DeserializeChar;
            else if (type == typeof(bool))
                return DeserializeBoolean;
            else if (type == typeof(int))
                return DeserializeInt32;
            else if (type == typeof(uint))
                return DeserializeUInt32;
            else if (type == typeof(short))
                return DeserializeInt16;
            else if (type == typeof(ushort))
                return DeserializeUInt16;
            else if (type == typeof(long))
                return DeserializeInt64;
            else if (type == typeof(ulong))
                return DeserializeUInt64;
            else if (type == typeof(decimal))
                return DeserializeDecimal;
            else if (type == typeof(float))
                return DeserializeSingle;
            else if (type == typeof(double))
                return DeserializeDouble;
            else if (type.IsEnum)
                return GetEnumOperation(type);
            else if (type.IsValueType)
                return DeserializeStruct;
            else if (type.IsClass)
                return DeserializeObjectReference;
            else
                throw new IcepackException($"Unable to deserialize object of type: {type}");
        }
    }
}
