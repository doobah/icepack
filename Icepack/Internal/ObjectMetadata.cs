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
        public int Length { get; }
        public object Value { get; }

        public ObjectMetadata(uint id, TypeMetadata type, int length, object value)
        {
            Id = id;
            Type = type;
            Length = length;
            Value = value;
        }
    }
}
