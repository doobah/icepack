using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    internal class SerializationContext
    {
        public Queue<object> ObjectsToSerialize { get; }
        public Dictionary<Type, TypeMetadata> Types { get; }

        private Dictionary<object, ulong> instanceIds;
        private ulong largestInstanceId;
        private ulong largestTypeId;
        private TypeRegistry typeRegistry;

        public SerializationContext(TypeRegistry typeRegistry)
        {
            instanceIds = new Dictionary<object, ulong>();
            largestInstanceId = Toolbox.NULL_ID;
            ObjectsToSerialize = new Queue<object>();
            Types = new Dictionary<Type, TypeMetadata>();
            largestTypeId = Toolbox.NULL_ID;
            this.typeRegistry = typeRegistry;
        }

        public ulong GetInstanceId(object obj)
        {
            if (instanceIds.ContainsKey(obj))
                return instanceIds[obj];

            return Toolbox.NULL_ID;
        }

        public ulong RegisterObject(object obj)
        {
            ulong newId = ++largestInstanceId;
            instanceIds.Add(obj, newId);

            return newId;
        }

        public bool IsObjectRegistered(object obj)
        {
            return instanceIds.ContainsKey(obj);
        }

        /// <summary> Retrieves the metadata for a type. </summary>
        /// <param name="type"> The type to retrieve metadata for. </param>
        /// <returns> The metadata for the type. </returns>
        /// <remarks> This method lazy-registers types that have the <see cref="SerializableObjectAttribute"/> attribute. </remarks>
        public TypeMetadata GetTypeMetadata(Type type)
        {
            if (!IsTypeRegistered(type))
            {
                TypeMetadata registeredTypeMetadata = typeRegistry.GetTypeMetadata(type);
                if (registeredTypeMetadata == null)
                    throw new IcepackException($"Type {type} is not registered for serialization!");

                TypeMetadata newTypeMetadata = new TypeMetadata(registeredTypeMetadata, ++largestTypeId);
                Types.Add(type, newTypeMetadata);
                return newTypeMetadata;
            }

            return Types[type];
        }

        private bool IsTypeRegistered(Type type)
        {
            return Types.ContainsKey(type);
        }
    }
}
