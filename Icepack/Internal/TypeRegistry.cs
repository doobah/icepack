using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Icepack
{
    /// <summary> Maintains a collection of type metadata used for serialization/deserialization operations. </summary>
    internal sealed class TypeRegistry
    {
        /// <summary> The serializer. </summary>
        private readonly Serializer serializer;

        /// <summary> Maps a type to metadata about the type. </summary>
        private readonly Dictionary<Type, TypeMetadata> types;

        /// <summary> Creates a new type registry. </summary>
        public TypeRegistry(Serializer serializer)
        {
            this.serializer = serializer;
            types = new Dictionary<Type, TypeMetadata>();
        }

        /// <summary>
        /// Called during type registration and serialization. Retrieves the metadata for a type and
        /// lazy-registers types that have the <see cref="SerializableTypeAttribute"/> attribute.
        /// </summary>
        /// <param name="type"> The type to retrieve metadata for. </param>
        /// <param name="preRegister"> Whether this method was called by <see cref="RegisterType(Type)"/>. </param>
        /// <returns> The metadata for the type. </returns>
        public TypeMetadata GetTypeMetadata(Type type, bool preRegister = false)
        {
            // Treat all type values as instances of Type, for simplicity.
            if (type.IsSubclassOf(typeof(Type)))
                return types[typeof(Type)];

            TypeMetadata? typeMetadata;
            if (types.TryGetValue(type, out typeMetadata))
                return typeMetadata;

            // Register common types by default
            if (type == typeof(object))
            {
            }
            else if (type.IsArray)
            {
            }
            else if (type.IsGenericType)
            {
                Type genericTypeDef = type.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(List<>) ||
                    genericTypeDef == typeof(HashSet<>) ||
                    genericTypeDef == typeof(Dictionary<,>))
                {
                }
                else
                {
                    if (!preRegister)
                    {
                        SerializableTypeAttribute? attr = type.GetCustomAttribute<SerializableTypeAttribute>(true);
                        if (attr == null)
                            throw new IcepackException($"Type {type} is not registered for serialization!");
                    }
                }
            }
            else
            {
                if (!preRegister)
                {
                    SerializableTypeAttribute? attr = type.GetCustomAttribute<SerializableTypeAttribute>(true);
                    if (attr == null)
                        throw new IcepackException($"Type {type} is not registered for serialization!");
                }
            }

            var newTypeMetadata = new TypeMetadata(type, serializer);
            types.Add(type, newTypeMetadata);
            return newTypeMetadata;
        }

        /// <summary>
        /// Called during deserialization to match a type name to a registered type. It lazy-registers types that
        /// have the <see cref="SerializableTypeAttribute"/> attribute.
        /// </summary>
        /// <param name="name"> The assembly qualified name of the type. </param>
        /// <returns> Metadata for the specified type. </returns>
        public TypeMetadata? GetTypeMetadata(string name)
        {
            Type? type = Type.GetType(name);
            if (type == null)
                return null;

            return GetTypeMetadata(type);
        }
    }
}
