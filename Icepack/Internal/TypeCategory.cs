using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Categories for types, which determine how the serializer handles objects of that type. </summary>
    internal enum TypeCategory : byte
    {
        /// <summary> A primitive, string, or decimal. </summary>
        Immutable = 0,

        /// <summary> An array. </summary>
        Array = 1,

        /// <summary> A list. </summary>
        List = 2,

        /// <summary> A hashset. </summary>
        HashSet = 3,

        /// <summary> A dictionary. </summary>
        Dictionary = 4,

        /// <summary> A value type that is not a primitive, decimal, or enum. </summary>
        Struct = 5,

        /// <summary> A class that is not an array, list, hashset, or dictionary, string, or type. </summary>
        Class = 6,

        /// <summary> An enum. </summary>
        Enum = 7,

        /// <summary> A type. </summary>
        Type = 8
    }
}
