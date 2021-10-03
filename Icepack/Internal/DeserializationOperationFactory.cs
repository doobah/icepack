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

        public static object DeserializeType(DeserializationContext context)
        {
            uint typeId = context.Reader.ReadUInt32();
            return context.Types[typeId - 1].Type;
        }

        public static object DeserializeStructField(DeserializationContext context)
        {
            uint typeId = context.Reader.ReadUInt32();
            TypeMetadata typeMetadata = context.Types[typeId - 1];

            if (typeMetadata.Type == null)
                context.Reader.BaseStream.Position += typeMetadata.InstanceSize;

            object structObj = Activator.CreateInstance(typeMetadata.Type);

            for (int i = 0; i < typeMetadata.Fields.Count; i++)
            {
                FieldMetadata field = typeMetadata.Fields[i];
                if (field.FieldInfo == null)
                {
                    context.Reader.BaseStream.Position += field.Size;
                }
                else
                {
                    Type fieldType = field.FieldInfo.FieldType;
                    object value = field.Deserialize(context);
                    field.Setter(structObj, value);
                }
            }

            if (structObj is ISerializerListener listener)
                listener.OnAfterDeserialize();

            return structObj;
        }

        public static object DeserializeStruct(ObjectMetadata objectMetadata, DeserializationContext context)
        {
            uint typeId = context.Reader.ReadUInt32();
            TypeMetadata typeMetadata = context.Types[typeId - 1];

            if (typeMetadata.Type == null)
                context.Reader.BaseStream.Position += typeMetadata.InstanceSize;

            object structObj = objectMetadata.Value;

            for (int i = 0; i < typeMetadata.Fields.Count; i++)
            {
                FieldMetadata field = typeMetadata.Fields[i];
                if (field.FieldInfo == null)
                {
                    context.Reader.BaseStream.Position += field.Size;
                }
                else
                {
                    Type fieldType = field.FieldInfo.FieldType;
                    object value = field.Deserialize(context);
                    field.Setter(structObj, value);
                }
            }

            if (structObj is ISerializerListener listener)
                listener.OnAfterDeserialize();

            return structObj;
        }

        public static void DeserializeArray(ObjectMetadata objectMetadata, DeserializationContext context)
        {
            Array arrayObj = (Array)objectMetadata.Value;
            TypeMetadata typeMetadata = objectMetadata.Type;

            if (typeMetadata.Type == null)
                context.Reader.BaseStream.Position += objectMetadata.Length * typeMetadata.ItemSize;
            else
            {
                for (int i = 0; i < arrayObj.Length; i++)
                {
                    object value = typeMetadata.DeserializeItem(context);
                    arrayObj.SetValue(value, i);
                }
            }
        }

        public static void DeserializeList(ObjectMetadata objectMetadata, DeserializationContext context)
        {
            IList listObj = (IList)objectMetadata.Value;
            TypeMetadata typeMetadata = objectMetadata.Type;

            if (typeMetadata.Type == null)
                context.Reader.BaseStream.Position += objectMetadata.Length * typeMetadata.ItemSize;
            else
            {
                for (int i = 0; i < objectMetadata.Length; i++)
                {
                    object value = typeMetadata.DeserializeItem(context);
                    listObj.Add(value);
                }
            }
        }

        public static void DeserializeHashSet(ObjectMetadata objectMetadata, DeserializationContext context)
        {
            object hashSetObj = objectMetadata.Value;
            TypeMetadata typeMetadata = objectMetadata.Type;

            if (typeMetadata.Type == null)
                context.Reader.BaseStream.Position += objectMetadata.Length * typeMetadata.ItemSize;
            else
            {
                for (int i = 0; i < objectMetadata.Length; i++)
                {
                    object value = typeMetadata.DeserializeItem(context);
                    typeMetadata.HashSetAdder(hashSetObj, value);
                }
            }
        }

        public static void DeserializeDictionary(ObjectMetadata objectMetadata, DeserializationContext context)
        {
            IDictionary dictObj = (IDictionary)objectMetadata.Value;
            TypeMetadata typeMetadata = objectMetadata.Type;

            if (typeMetadata.Type == null)
                context.Reader.BaseStream.Position += objectMetadata.Length * (typeMetadata.KeySize + typeMetadata.ItemSize);
            else
            {
                for (int i = 0; i < objectMetadata.Length; i++)
                {
                    object key = typeMetadata.DeserializeKey(context);
                    object value = typeMetadata.DeserializeItem(context);
                    dictObj.Add(key, value);
                }
            }
        }

        public static void DeserializeNormalClass(ObjectMetadata objectMetadata, DeserializationContext context)
        {
            object obj = objectMetadata.Value;
            TypeMetadata typeMetadata = objectMetadata.Type;

            if (typeMetadata.Type == null)
            {
                while (true)
                {
                    uint partialClassTypeId = context.Reader.ReadUInt32();
                    TypeMetadata partialClassTypeMetadata = context.Types[partialClassTypeId - 1];
                    context.Reader.BaseStream.Position += partialClassTypeMetadata.InstanceSize;

                    if (!partialClassTypeMetadata.HasParent)
                        break;
                }
            }
            else
            {
                while (true)
                {
                    uint partialClassTypeId = context.Reader.ReadUInt32();
                    TypeMetadata partialClassTypeMetadata = context.Types[partialClassTypeId - 1];

                    if (partialClassTypeMetadata.Type == null || !typeMetadata.Type.IsAssignableTo(partialClassTypeMetadata.Type))
                        context.Reader.BaseStream.Position += typeMetadata.InstanceSize;
                    else
                    {
                        for (int fieldIdx = 0; fieldIdx < partialClassTypeMetadata.Fields.Count; fieldIdx++)
                        {
                            FieldMetadata field = partialClassTypeMetadata.Fields[fieldIdx];
                            if (field.FieldInfo == null)
                                context.Reader.BaseStream.Position += field.Size;
                            else
                            {
                                Type fieldType = field.FieldInfo.FieldType;
                                object value = field.Deserialize(context);
                                field.Setter(obj, value);
                            }
                        }
                    }

                    if (!partialClassTypeMetadata.HasParent)    
                        break;
                }

                if (obj is ISerializerListener listener)
                    listener.OnAfterDeserialize();
            }
        }

        public static void DeserializeReferenceType(ObjectMetadata objectMetadata, DeserializationContext context)
        {
            TypeMetadata classTypeMetadata = objectMetadata.Type;

            switch (classTypeMetadata.CategoryId)
            {
                case TypeCategory.Basic:
                    // Value is serialized as metadata
                    return;
                case TypeCategory.Array:
                    DeserializeArray(objectMetadata, context);
                    break;
                case TypeCategory.List:
                    DeserializeList(objectMetadata, context);
                    break;
                case TypeCategory.HashSet:
                    DeserializeHashSet(objectMetadata, context);
                    break;
                case TypeCategory.Dictionary:
                    DeserializeDictionary(objectMetadata, context);
                    break;
                case TypeCategory.Struct:
                    DeserializeStruct(objectMetadata, context);
                    break;
                case TypeCategory.Class:
                    DeserializeNormalClass(objectMetadata, context);
                    break;
                case TypeCategory.Enum:
                    // Value is serialized as metadata
                    break;
                case TypeCategory.Type:
                    // Value is serialized as metadata
                    break;
                default:
                    throw new IcepackException($"Invalid category ID: {classTypeMetadata.CategoryId}");
            }
        }

        public static object DeserializeObjectReference(DeserializationContext context)
        {
            uint objId = context.Reader.ReadUInt32();
            return (objId == 0) ? null : context.Objects[objId - 1].Value;
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

        public static Func<DeserializationContext, object> GetFieldOperation(Type type)
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
                return DeserializeStructField;
            else if (type.IsClass || type.IsInterface)
                return DeserializeObjectReference;
            else
                throw new IcepackException($"Unable to deserialize object of type: {type}");
        }

        public static Func<DeserializationContext, object> GetBasicOperation(Type type)
        {
            if (type == typeof(string))
                return DeserializeString;
            else if (type == typeof(byte))
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
            else
                throw new IcepackException($"Unexpected type: {type}");
        }
    }
}
