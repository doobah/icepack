using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Icepack
{
    /// <summary> Contains state information for the current serialization process. </summary>
    internal class SerializationContext
    {
        /// <summary> Maps a type to metadata about the type. </summary>
        public Dictionary<Type, TypeMetadata> Types { get; }

        /// <summary> A list of type metadata in order of ID. </summary>
        public List<TypeMetadata> TypesInOrder { get; }

        /// <summary> Maps an object to metadata about the object. </summary>
        public Dictionary<object, ObjectMetadata> Objects { get; }

        /// <summary> A list of object metadata in order of ID. </summary>
        public List<ObjectMetadata> ObjectsInOrder { get; }

        /// <summary> Keeps track of the largest assigned object ID. </summary>
        private uint largestObjectId;

        /// <summary> Keeps track of the largest assigned type ID. </summary>
        private uint largestTypeId;

        /// <summary> The serializer's type registry. </summary>
        private readonly TypeRegistry typeRegistry;

        /// <summary> Creates a new serialization context. </summary>
        /// <param name="typeRegistry"> The serializer's type registry. </param>
        public SerializationContext(TypeRegistry typeRegistry)
        {
            Objects = new Dictionary<object, ObjectMetadata>();
            ObjectsInOrder = new List<ObjectMetadata>();
            Types = new Dictionary<Type, TypeMetadata>();
            TypesInOrder = new List<TypeMetadata>();
            largestObjectId = 0;
            largestTypeId = 0;
            this.typeRegistry = typeRegistry;
        }

        /// <summary> Registers an object for serialization. </summary>
        /// <param name="obj"> The object. </param>
        /// <returns> A unique ID for the object. </returns>
        public uint RegisterObject(object obj)
        {
            uint newId = ++largestObjectId;

            // Treat all type values as instances of Type, for simplicity.
            Type type = obj.GetType();
            if (type.IsSubclassOf(typeof(Type)))
                type = typeof(Type);
            TypeMetadata typeMetadata = GetTypeMetadata(type);

            int length = 0;
            switch (typeMetadata.Category)
            {
                case TypeCategory.Immutable:
                    break;
                case TypeCategory.Array:
                    length = ((Array)obj).Length;
                    break;
                case TypeCategory.List:
                    length = ((IList)obj).Count;
                    break;
                case TypeCategory.HashSet:
                    // Necessary because hashset doesn't have a specific non-generic interface
                    length = 0;
                    foreach (object item in (IEnumerable)obj)
                        length++;
                    break;
                case TypeCategory.Dictionary:
                    length = ((IDictionary)obj).Count;
                    break;
                case TypeCategory.Struct:
                case TypeCategory.Class:
                case TypeCategory.Enum:
                case TypeCategory.Type:
                    break;
                default:
                    throw new IcepackException($"Invalid type category: {typeMetadata.Category}");
            }

            var objMetadata = new ObjectMetadata(newId, typeMetadata, length, obj);
            Objects.Add(obj, objMetadata);
            ObjectsInOrder.Add(objMetadata);
            
            return newId;
        }

        /// <summary>
        /// Retrieves the metadata for a type, lazy-registers types that have the <see cref="SerializableObjectAttribute"/> attribute.
        /// </summary>
        /// <param name="type"> The type to retrieve metadata for. </param>
        /// <returns> The metadata for the type. </returns>
        public TypeMetadata GetTypeMetadata(Type type)
        {
            if (!Types.ContainsKey(type))
            {
                // If this is an enum, we want the underlying type to be present ahead of the enum type
                TypeMetadata enumUnderlyingTypeMetadata = null;
                if (type.IsEnum)
                    enumUnderlyingTypeMetadata = GetTypeMetadata(type.GetEnumUnderlyingType());

                TypeMetadata registeredTypeMetadata = typeRegistry.GetTypeMetadata(type);
                if (registeredTypeMetadata == null)
                    throw new IcepackException($"Type {type} is not registered for serialization!");

                var newTypeMetadata = new TypeMetadata(registeredTypeMetadata, ++largestTypeId, enumUnderlyingTypeMetadata);
                Types.Add(type, newTypeMetadata);
                TypesInOrder.Add(newTypeMetadata);
                return newTypeMetadata;
            }

            return Types[type];
        }
    }
}
