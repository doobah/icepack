using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Icepack
{
    /// <summary> Returns delegates for deserializing types and fields. </summary>
    internal static class DeserializationDelegateFactory
    {
        private static object DeserializeByte(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadByte();
        }

        private static object DeserializeSByte(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadSByte();
        }

        private static object DeserializeChar(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadChar();
        }

        private static object DeserializeBoolean(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadBoolean();
        }

        private static object DeserializeInt32(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadInt32();
        }

        private static object DeserializeUInt32(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadUInt32();
        }

        private static object DeserializeInt16(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadInt16();
        }

        private static object DeserializeUInt16(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadUInt16();
        }

        private static object DeserializeInt64(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadInt64();
        }

        private static object DeserializeUInt64(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadUInt64();
        }

        private static object DeserializeDecimal(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadDecimal();
        }

        private static object DeserializeSingle(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadSingle();
        }

        private static object DeserializeDouble(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadDouble();
        }

        private static object DeserializeString(DeserializationContext context, BinaryReader reader)
        {
            return reader.ReadString();
        }

        private static object? DeserializeStructField(DeserializationContext context, BinaryReader reader)
        {
            uint typeId = reader.ReadUInt32();
            // Type IDs start from 1
            TypeMetadata typeMetadata = context.Types[typeId - 1];

            // Skip instance if type doesn't exist
            if (typeMetadata.Type == null)
            {
                reader.BaseStream.Position += typeMetadata.InstanceSize;
                return null;
            }

            object structObj = typeMetadata.CreateClassOrStruct!();

            for (int i = 0; i < typeMetadata.Fields!.Count; i++)
            {
                FieldMetadata field = typeMetadata.Fields[i];
                if (field.FieldInfo == null)
                    reader.BaseStream.Position += field.Size;
                else
                {
                    object value = field.Deserialize!(context, reader)!;
                    field.Setter!(structObj, value);
                }
            }

            if (structObj is ISerializerListener listener)
                listener.OnAfterDeserialize();

            return structObj;
        }

        private static void DeserializeBoxedStruct(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            uint typeId = reader.ReadUInt32();
            // Type IDs start from 1
            TypeMetadata typeMetadata = context.Types[typeId - 1];

            // Skip instance if type doesn't exist
            if (typeMetadata.Type == null)
            {
                reader.BaseStream.Position += typeMetadata.InstanceSize;
                return;
            }

            object structObj = objectMetadata.Value!;

            for (int i = 0; i < typeMetadata.Fields!.Count; i++)
            {
                FieldMetadata field = typeMetadata.Fields[i];
                if (field.FieldInfo == null)
                    reader.BaseStream.Position += field.Size;
                else
                {
                    object value = field.Deserialize!(context, reader)!;
                    field.Setter!(structObj, value);
                }
            }

            if (structObj is ISerializerListener listener)
                listener.OnAfterDeserialize();
        }

        private static void DeserializeArray(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            TypeMetadata typeMetadata = objectMetadata.TypeMetadata;

            if (typeMetadata.Type == null)
                reader.BaseStream.Position += objectMetadata.Length * typeMetadata.ItemSize;
            else
            {
                Array arrayObj = (Array)objectMetadata.Value!;

                for (int i = 0; i < arrayObj.Length; i++)
                {
                    object? value = typeMetadata.DeserializeItem!(context, reader);
                    arrayObj.SetValue(value, i);
                }
            }
        }

        private static void DeserializeList(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            TypeMetadata typeMetadata = objectMetadata.TypeMetadata;

            if (typeMetadata.Type == null)
                reader.BaseStream.Position += objectMetadata.Length * typeMetadata.ItemSize;
            else
            {
                IList listObj = (IList)objectMetadata.Value!;

                for (int i = 0; i < objectMetadata.Length; i++)
                {
                    object? value = typeMetadata.DeserializeItem!(context, reader);
                    listObj.Add(value);
                }
            }
        }

        private static void DeserializeHashSet(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            TypeMetadata typeMetadata = objectMetadata.TypeMetadata;

            if (typeMetadata.Type == null)
                reader.BaseStream.Position += objectMetadata.Length * typeMetadata.ItemSize;
            else
            {
                object hashSetObj = objectMetadata.Value!;

                for (int i = 0; i < objectMetadata.Length; i++)
                {
                    object value = typeMetadata.DeserializeItem!(context, reader)!;
                    typeMetadata.HashSetAdder!(hashSetObj, value);
                }
            }
        }

        private static void DeserializeDictionary(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            TypeMetadata typeMetadata = objectMetadata.TypeMetadata;

            if (typeMetadata.Type == null)
                reader.BaseStream.Position += objectMetadata.Length * (typeMetadata.KeySize + typeMetadata.ItemSize);
            else
            {
                IDictionary dictObj = (IDictionary)objectMetadata.Value!;

                for (int i = 0; i < objectMetadata.Length; i++)
                {
                    object key = typeMetadata.DeserializeKey!(context, reader)!;
                    object value = typeMetadata.DeserializeItem!(context, reader)!;
                    dictObj.Add(key, value);
                }
            }
        }

        private static void DeserializeNormalClass(ObjectMetadata objectMetadata, DeserializationContext context, BinaryReader reader)
        {
            object obj = objectMetadata.Value!;
            TypeMetadata typeMetadata = objectMetadata.TypeMetadata;
            TypeMetadata? currentTypeMetadata = typeMetadata;

            while (currentTypeMetadata != null)
            {
                // Skip the class if it is missing or the object no longer derives from it
                if (currentTypeMetadata.Type == null || !currentTypeMetadata.Type.IsAssignableFrom(typeMetadata.Type))
                    reader.BaseStream.Position += currentTypeMetadata.InstanceSize;
                else
                {
                    for (int fieldIdx = 0; fieldIdx < currentTypeMetadata.Fields!.Count; fieldIdx++)
                    {
                        FieldMetadata field = currentTypeMetadata.Fields[fieldIdx];
                        if (field.FieldInfo == null)
                            reader.BaseStream.Position += field.Size;
                        else
                        {
                            object value = field.Deserialize!(context, reader)!;
                            field.Setter!(obj, value);
                        }
                    }
                }

                currentTypeMetadata = currentTypeMetadata.ParentTypeMetadata;
            }

            if (obj is ISerializerListener listener)
                listener.OnAfterDeserialize();
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

        /// <summary> Returns a delegate which is used to deserialize instances of the given type category. </summary>
        /// <param name="category"> The type category. </param>
        /// <returns> The deserialization delegate. </returns>
        public static Action<ObjectMetadata, DeserializationContext, BinaryReader> GetReferenceTypeOperation(TypeCategory category)
        {
            switch(category)
            {
                case TypeCategory.Immutable:
                    return DeserializeImmutableReferenceType;
                case TypeCategory.Array: 
                    return DeserializeArray;
                case TypeCategory.List: 
                    return DeserializeList;
                case TypeCategory.HashSet: 
                    return DeserializeHashSet;
                case TypeCategory.Dictionary: 
                    return DeserializeDictionary;
                case TypeCategory.Struct: 
                    return DeserializeBoxedStruct;
                case TypeCategory.Class: 
                    return DeserializeNormalClass;
                case TypeCategory.Enum:
                    return DeserializeEnumReferenceType;
                case TypeCategory.Type:
                    return DeserializeTypeReferenceType;
                default:
                    throw new IcepackException($"Invalid type category: {category}");
            };
        }

        private static object? DeserializeObjectReference(DeserializationContext context, BinaryReader reader)
        {
            uint objId = reader.ReadUInt32();
            return (objId == 0) ? null : context.Objects[objId - 1].Value;
        }

        private static Func<DeserializationContext, BinaryReader, object> GetEnumOperation(Type type)
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

        /// <summary> Returns the delegate used to deserialize fields of the given type. </summary>
        /// <param name="type"> The field's declaring type. </param>
        /// <returns> The deserialization delegate. </returns>
        public static Func<DeserializationContext, BinaryReader, object?> GetFieldOperation(Type type)
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

        /// <summary> Returns the delegate used to deserialize immutable types. </summary>
        /// <param name="type"> The type. </param>
        /// <returns> The deserialization delegate. </returns>
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
