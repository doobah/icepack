using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Icepack
{
    /// <summary> Stores state for the current deserialization operation. </summary>
    internal class DeserializationContext
    {
        /// <summary>
        /// Maps a type ID to information about the type. The type information is different from what is stored in the serializer's
        /// type registry since the document's type table is used when deserializing.
        /// </summary>
        public TypeMetadata[] Types { get; set; }

        /// <summary> Maps an object ID to the object itself. </summary>
        public ObjectMetadata[] Objects { get; set; }

        public DeserializationContext()
        {
            Types = null;
            Objects = null;
        }
    }
}
