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
}
