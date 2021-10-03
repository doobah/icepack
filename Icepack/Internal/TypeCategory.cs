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
        Basic = 0,
        Array = 1,
        List = 2,
        HashSet = 3,
        Dictionary = 4,
        Struct = 5,
        Class = 6,
        Enum = 7,
        Type = 8
    }
}
