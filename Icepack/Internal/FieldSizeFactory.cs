using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack.Internal;

/// <summary> Provides the size of a field of a given type. </summary>
internal static class FieldSizeFactory
{
    /// <summary> Gets the size of a field of a given type. </summary>
    /// <param name="type"> The field's declaring type. </param>
    /// <param name="typeRegistry"> The serializer's type registry. </param>
    /// <returns> The size of the field in bytes. </returns>
    public static int GetFieldSize(Type type, TypeRegistry typeRegistry)
    {
        if (type == typeof(byte))
            return 1;
        else if (type == typeof(sbyte))
            return 1;
        else if (type == typeof(bool))
            return 1;
        else if (type == typeof(char))
            return 2;
        else if (type == typeof(short))
            return 2;
        else if (type == typeof(ushort))
            return 2;
        else if (type == typeof(int))
            return 4;
        else if (type == typeof(uint))
            return 4;
        else if (type == typeof(long))
            return 8;
        else if (type == typeof(ulong))
            return 8;
        else if (type == typeof(float))
            return 4;
        else if (type == typeof(double))
            return 8;
        else if (type == typeof(decimal))
            return 16;
        else if (typeof(Type).IsAssignableFrom(type))
            return 4;
        else if (type.IsEnum)
            return GetEnumFieldSize(type);
        else if (type.IsValueType)
            return GetStructFieldSize(type, typeRegistry);
        else if (type.IsClass || type.IsInterface)
            return 4;
        else
            throw new IcepackException($"Unable to determine size of field type: {type}");
    }

    /// <summary> Gets the size of a struct field. </summary>
    /// <param name="type"> The field's declaring type. </param>
    /// <param name="typeRegistry"> The serializer's type registry. </param>
    /// <returns> The size of the field in bytes. </returns>
    private static int GetStructFieldSize(Type type, TypeRegistry typeRegistry)
    {
        TypeMetadata structTypeMetadata = typeRegistry.GetTypeMetadata(type);
        // Add 4 bytes for type ID
        return structTypeMetadata.InstanceSize + 4;
    }

    /// <summary> Gets the size of an enum field. </summary>
    /// <param name="type"> The field's declaring type. </param>
    /// <returns> The size of the field in bytes. </returns>
    private static int GetEnumFieldSize(Type type)
    {
        Type underlyingType = Enum.GetUnderlyingType(type);
        if (underlyingType == typeof(byte))
            return 1;
        else if (underlyingType == typeof(sbyte))
            return 1;
        else if (underlyingType == typeof(short))
            return 2;
        else if (underlyingType == typeof(ushort))
            return 2;
        else if (underlyingType == typeof(int))
            return 4;
        else if (underlyingType == typeof(uint))
            return 4;
        else if (underlyingType == typeof(long))
            return 8;
        else if (underlyingType == typeof(ulong))
            return 8;
        else
            throw new IcepackException($"Invalid enum underlying type: {type}");
    }
}
