using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack.Internal;

/// <summary> Contains helpful methods. </summary>
internal static class Utils
{
    /// <summary> Returns whether the specified type is unsupported. </summary>
    /// <param name="type"> The type to check. </param>
    /// <returns> Whether the type is unsupported. </returns>
    internal static bool IsUnsupportedType(Type type)
    {
        return
            type == typeof(nint) ||
            type == typeof(nuint) ||
            type.IsAssignableTo(typeof(Delegate)) ||
            type.IsPointer ||
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Span<>) ||
            type.IsGenericTypeDefinition;
    }

    /// <summary> Determines the category for a given type. </summary>
    /// <param name="type"> The type. </param>
    /// <returns> The category for the type. </returns>
    internal static TypeCategory GetTypeCategory(Type type)
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
}
