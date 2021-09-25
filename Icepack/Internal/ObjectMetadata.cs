using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    internal class ObjectMetadata
    {
        public uint Id { get; }
        public TypeMetadata Type { get; }
        public int ArrayLength { get; }

        public ObjectMetadata(uint id, TypeMetadata type, int arrayLength)
        {
            Id = id;
            Type = type;
            ArrayLength = arrayLength;
        }
    }
}
