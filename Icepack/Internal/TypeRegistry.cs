using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Contains metadata about serializable structs and classes. </summary>
    internal class TypeRegistry
    {
        private Dictionary<Type, TypeMetadata> types;

        public TypeRegistry()
        {
            types = new Dictionary<Type, TypeMetadata>();
        }

        /// <summary> Registers a type as serializable. </summary>
        /// <param name="type"> The type to register. </param>
        /// <remarks> This is generally used to allow types in other assemblies to be serialized. </remarks>
        public TypeMetadata RegisterType(Type type)
        {
            if (types.ContainsKey(type))
                return types[type];

            if (!Toolbox.IsClass(type) && !Toolbox.IsStruct(type) || type == typeof(object) || type == typeof(ValueType))
                throw new IcepackException($"Type {type} cannot be registered for serialization!");

            TypeMetadata newTypeMetadata = new TypeMetadata(type);
            types.Add(type, newTypeMetadata);

            return newTypeMetadata;
        }

        /// <summary> Retrieves the metadata for a type. </summary>
        /// <param name="type"> The type to retrieve metadata for. </param>
        /// <returns> The metadata for the type. </returns>
        /// <remarks> This method lazy-registers types that have the <see cref="SerializableObjectAttribute"/> attribute. </remarks>
        public TypeMetadata GetTypeMetadata(Type type)
        {
            if (!IsTypeRegistered(type))
            {
                object[] attributes = type.GetCustomAttributes(typeof(SerializableObjectAttribute), true);
                if (attributes.Length == 0)
                    throw new IcepackException($"Type {type} is not registered for serialization!");

                return RegisterType(type);
            }

            return types[type];
        }

        public TypeMetadata GetTypeMetadata(string name)
        {
            Type type = Type.GetType(name);
            if (type == null)
                throw new IcepackException($"No type exists with name {name}");

            if (!IsTypeRegistered(type))
            {
                object[] attributes = type.GetCustomAttributes(typeof(SerializableObjectAttribute), true);
                if (attributes.Length == 0)
                    throw new IcepackException($"Type {type.AssemblyQualifiedName} is not registered for serialization!");

                RegisterType(type);
            }

            return types[type];
        }

        private bool IsTypeRegistered(Type type)
        {
            return types.ContainsKey(type);
        }
    }
}
