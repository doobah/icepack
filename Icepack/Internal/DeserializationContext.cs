using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Icepack
{
    /// <summary> Stores state for the current deserialization operation. </summary>
    internal sealed class DeserializationContext
    {
        /// <summary> Metadata for types declared in the serialized data, indexed by type ID. </summary>
        public TypeMetadata[] Types { get; }

        /// <summary> Metadata for objects declared in the serialized data, indexed by object ID. </summary>
        public ObjectMetadata[] Objects { get; }

        /// <summary> Creates a new deserialization context. </summary>
        public DeserializationContext(TypeMetadata[] types, ObjectMetadata[] objects)
        {
            Types = types;
            Objects = objects;
        }
    }
}
