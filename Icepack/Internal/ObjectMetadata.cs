using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Contains metadata for an object. </summary>
    internal struct ObjectMetadata
    {
        /// <summary> A unique ID corresponding to an object. </summary>
        public uint Id { get; }

        /// <summary> Metadata about the type of the object. </summary>
        public TypeMetadata TypeMetadata { get; }

        /// <summary> If the object is an array, list, hashset, or dictionary, this is the number of items. </summary>
        public int Length { get; }

        /// <summary> The value of the object. </summary>
        public object Value { get; }

        /// <summary>
        /// The nesting depth of this object in the hierarchy. This is used to detect circular references when references are not preserved.
        /// </summary>
        public int Depth { get; }

        /// <summary> Creates new object metadata. </summary>
        /// <param name="id"> A unique ID corresponding to an object. </param>
        /// <param name="type"> Metadata about the type of the object. </param>
        /// <param name="length"> If the object is an array, list, hashset, or dictionary, this is the length of the object. </param>
        /// <param name="value"> The value of the object. </param>
        /// <param name="depth"> The nest depth of the object. </param>
        public ObjectMetadata(uint id, TypeMetadata type, int length, object value, int depth)
        {
            Id = id;
            TypeMetadata = type;
            Length = length;
            Value = value;
            Depth = depth;
        }
    }
}
