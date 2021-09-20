using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Stores state for the current deserialization operation. </summary>
    internal class DeserializationContext
    {
        /// <summary>
        /// Maps a type ID to information about the type. The type information is different from what is stored in the serializer's
        /// type registry since the document's type table is used when deserializing.
        /// </summary>
        public Dictionary<ulong, TypeMetadata> Types { get; }

        /// <summary> Maps an object ID to the object itself. </summary>
        public object[] Objects { get; set; }

        public ulong CurrentObjectId { get; set; }

        public DeserializationContext()
        {
            Types = new Dictionary<ulong, TypeMetadata>();
            Objects = null;
            CurrentObjectId = Toolbox.NULL_ID;
        }
    }
}
