using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    internal class TypeRegistry
    {
        private const ulong NULL_ID = 0;

        private Dictionary<Type, TypeMetadata> types;
        private ulong largestTypeId;

        public TypeRegistry()
        {
            types = new Dictionary<Type, TypeMetadata>();
            largestTypeId = NULL_ID;
        }

        private void RegisterType(Type type)
        {
            if (types.ContainsKey(type) || !Toolbox.IsClass(type) && !Toolbox.IsStruct(type))
                return;

            types.Add(type, new TypeMetadata(++largestTypeId, type));
        }

        public TypeMetadata GetTypeMetadata(Type type)
        {
            RegisterType(type);

            if (!types.ContainsKey(type))
                throw new ArgumentException($"Cannot register type: {type}");

            return types[type];
        }
    }
}
