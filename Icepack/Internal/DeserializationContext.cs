using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Icepack
{
    /// <summary> Stores state for the current deserialization operation. </summary>
    internal class DeserializationContext : IDisposable
    {
        /// <summary>
        /// Maps a type ID to information about the type. The type information is different from what is stored in the serializer's
        /// type registry since the document's type table is used when deserializing.
        /// </summary>
        public TypeMetadata[] Types { get; set; }

        /// <summary> Maps an object ID to the object itself. </summary>
        public object[] Objects { get; set; }

        public TypeMetadata[] ObjectTypes { get; set; }

        public BinaryReader Reader { get; }

        public DeserializationContext(Stream inputStream)
        {
            Types = null;
            Objects = null;
            ObjectTypes = null;
            Reader = new BinaryReader(inputStream, Encoding.Unicode, true);
        }

        public void Dispose()
        {
            Reader.Dispose();
        }
    }
}
