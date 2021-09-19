using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    internal class DeserializationContext
    {
        private Dictionary<ulong, TypeMetadata> types;
        private Dictionary<ulong, object> objects;

        public DeserializationContext()
        {
            types = new Dictionary<ulong, TypeMetadata>();
            objects = new Dictionary<ulong, object>();
        }

        public Dictionary<ulong, TypeMetadata> Types
        {
            get { return types; }
        }

        public Dictionary<ulong, object> Objects
        {
            get { return objects; }
        }
    }
}
