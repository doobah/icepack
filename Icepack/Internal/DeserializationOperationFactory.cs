using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Icepack
{
    internal static class DeserializationOperationFactory
    {
        public static object DeserializeByte(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadByte();
        }

        public static object DeserializeSByte(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadSByte();
        }

        public static object DeserializeChar(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadChar();
        }

        public static object DeserializeBoolean(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadBoolean();
        }

        public static object DeserializeInt32(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadInt32();
        }

        public static object DeserializeUInt32(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadUInt32();
        }

        public static object DeserializeInt16(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadInt16();
        }

        public static object DeserializeUInt16(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadUInt16();
        }

        public static object DeserializeInt64(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadInt64();
        }

        public static object DeserializeUInt64(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadUInt64();
        }

        public static object DeserializeDecimal(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadDecimal();
        }

        public static object DeserializeSingle(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadSingle();
        }

        public static object DeserializeDouble(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadDouble();
        }

        public static object DeserializeString(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadString();
        }

        public static object DeserializeType(DeserializationContext context, BinaryReader reader)
        {
            uint typeId = reader.ReadUInt32();
            return context.Types[typeId - 1].Type;
        }

        public static object DeserializeStructField(DeserializationContext context, BinaryReader reader)
        {
            uint typeId = reader.ReadUInt32();
            TypeMetadata typeMetadata = context.Types[typeId - 1];

            if (typeMetadata.Type == null)
            {
                reader.BaseStream.Position += typeMetadata.InstanceSize;
                return null;
            }

            object structObj = Activator.CreateInstance(typeMetadata.Type);

            for (int i = 0; i < typeMetadata.Fields.Count; i++)
            {
                FieldMetadata field = typeMetadata.Fields[i];
                if (field.FieldInfo == null)
                {
                    reader.BaseStream.Position += field.Size;
                }
                else
                {
                    Type fieldType = field.FieldInfo.FieldType;
                    object value = field.Deserialize(context, reader);
                    field.Setter(structObj, value);
                }
            }

            if (structObj is ISerializerListener listener)
                listener.OnAfterDeserialize();

            return structObj;
        }

        public static void DeserializeStruct(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            uint typeId = reader.ReadUInt32();
            TypeMetadata typeMetadata = context.Types[typeId - 1];

            if (typeMetadata.Type == null)
            {
                reader.BaseStream.Position += typeMetadata.InstanceSize;
                return;
            }

            object structObj = objectMetadata.Value;

            for (int i = 0; i < typeMetadata.Fields.Count; i++)
            {
                FieldMetadata field = typeMetadata.Fields[i];
                if (field.FieldInfo == null)
                {
                    reader.BaseStream.Position += field.Size;
                }
                else
                {
                    Type fieldType = field.FieldInfo.FieldType;
                    object value = field.Deserialize(context, reader);
                    field.Setter(structObj, value);
                }
            }

            if (structObj is ISerializerListener listener)
                listener.OnAfterDeserialize();
        }

        public static void DeserializeArray(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            Array arrayObj = (Array)objectMetadata.Value;
            TypeMetadata typeMetadata = objectMetadata.Type;

            if (typeMetadata.Type == null)
                reader.BaseStream.Position += objectMetadata.Length * typeMetadata.ItemSize;
            else
            {
                for (int i = 0; i < arrayObj.Length; i++)
                {
                    object value = typeMetadata.DeserializeItem(context, reader);
                    arrayObj.SetValue(value, i);
                }
            }
        }

        public static void DeserializeList(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            IList listObj = (IList)objectMetadata.Value;
            TypeMetadata typeMetadata = objectMetadata.Type;

            if (typeMetadata.Type == null)
                reader.BaseStream.Position += objectMetadata.Length * typeMetadata.ItemSize;
            else
            {
                for (int i = 0; i < objectMetadata.Length; i++)
                {
                    object value = typeMetadata.DeserializeItem(context, reader);
                    listObj.Add(value);
                }
            }
        }

        public static void DeserializeHashSet(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            object hashSetObj = objectMetadata.Value;
            TypeMetadata typeMetadata = objectMetadata.Type;

            if (typeMetadata.Type == null)
                reader.BaseStream.Position += objectMetadata.Length * typeMetadata.ItemSize;
            else
            {
                for (int i = 0; i < objectMetadata.Length; i++)
                {
                    object value = typeMetadata.DeserializeItem(context, reader);
                    typeMetadata.HashSetAdder(hashSetObj, value);
                }
            }
        }

        public static void DeserializeDictionary(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            IDictionary dictObj = (IDictionary)objectMetadata.Value;
            TypeMetadata typeMetadata = objectMetadata.Type;

            if (typeMetadata.Type == null)
                reader.BaseStream.Position += objectMetadata.Length * (typeMetadata.KeySize + typeMetadata.ItemSize);
            else
            {
                for (int i = 0; i < objectMetadata.Length; i++)
                {
                    object key = typeMetadata.DeserializeKey(context, reader);
                    object value = typeMetadata.DeserializeItem(context, reader);
                    dictObj.Add(key, value);
                }
            }
        }

        public static void DeserializeNormalClass(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            object obj = objectMetadata.Value;
            TypeMetadata typeMetadata = objectMetadata.Type;

            if (typeMetadata.Type == null)
            {
                while (true)
                {
                    uint partialClassTypeId = reader.ReadUInt32();
                    TypeMetadata partialClassTypeMetadata = context.Types[partialClassTypeId - 1];
                    reader.BaseStream.Position += partialClassTypeMetadata.InstanceSize;

                    if (!partialClassTypeMetadata.HasParent)
                        break;
                }
            }
            else
            {
                while (true)
                {
                    uint partialClassTypeId = reader.ReadUInt32();
                    TypeMetadata partialClassTypeMetadata = context.Types[partialClassTypeId - 1];

                    if (partialClassTypeMetadata.Type == null || !typeMetadata.Type.IsAssignableTo(partialClassTypeMetadata.Type))
                        reader.BaseStream.Position += typeMetadata.InstanceSize;
                    else
                    {
                        for (int fieldIdx = 0; fieldIdx < partialClassTypeMetadata.Fields.Count; fieldIdx++)
                        {
                            FieldMetadata field = partialClassTypeMetadata.Fields[fieldIdx];
                            if (field.FieldInfo == null)
                                reader.BaseStream.Position += field.Size;
                            else
                            {
                                Type fieldType = field.FieldInfo.FieldType;
                                object value = field.Deserialize(context, reader);
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

        private static void DeserializeImmutableReferenceType(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            // Value is serialized as metadata
        }

        private static void DeserializeEnumReferenceType(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            // Value is serialized as metadata
        }

        private static void DeserializeTypeReferenceType(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            // Value is serialized as metadata
        }

        public static Action<ObjectMetadata, DeserializationContext, BinaryReader> GetReferenceTypeOperation(TypeCategory category)
        {
            return category switch
            {
                TypeCategory.Immutable => DeserializeImmutableReferenceType,
                TypeCategory.Array => DeserializeArray,
                TypeCategory.List => DeserializeList,
                TypeCategory.HashSet => DeserializeHashSet,
                TypeCategory.Dictionary => DeserializeDictionary,
                TypeCategory.Struct => DeserializeStruct,
                TypeCategory.Class => DeserializeNormalClass,
                TypeCategory.Enum => DeserializeEnumReferenceType,
                TypeCategory.Type => DeserializeTypeReferenceType,
                _ => throw new IcepackException($"Invalid type category: {category}"),
            };
        }

        public static object DeserializeObjectReference(DeserializationContext context, BinaryReader reader)
        {
            uint objId = reader.ReadUInt32();
            return (objId == 0) ? null : context.Objects[objId - 1].Value;
        }

        public static Func<DeserializationContext, BinaryReader, object> GetEnumOperation(Type type)
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

        public static Func<DeserializationContext, BinaryReader, object> GetFieldOperation(Type type)
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

        public static Func<DeserializationContext, BinaryReader, object> GetImmutableOperation(Type type)
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
