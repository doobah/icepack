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
        private Dictionary<string, TypeMetadata> types;
        private ulong largestTypeId;

        public TypeRegistry()
        {
            types = new Dictionary<string, TypeMetadata>();
            largestTypeId = Toolbox.NULL_ID;
        }

        /// <summary> Registers a type as serializable. </summary>
        /// <param name="type"> The type to register. </param>
        /// <remarks> This is generally used to allow types in other assemblies to be serialized. </remarks>
        public void RegisterType(Type type, bool isItemsNoReference)
        {
            if (IsTypeRegistered(type))
                return;

            if (!Toolbox.IsClass(type) && !Toolbox.IsStruct(type) || type == typeof(object) || type == typeof(ValueType))
                throw new IcepackException($"Type {type} cannot be registered for serialization!");

            types.Add(type.AssemblyQualifiedName, new TypeMetadata(++largestTypeId, type, isItemsNoReference));
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

                // IsItemsNoReference is only set with a call to RegisterType which should already have happened.
                RegisterType(type, false);
            }

            return types[type.AssemblyQualifiedName];
        }

        public TypeMetadata GetTypeMetadata(string name)
        {
            if (!IsTypeRegistered(name))
            {
                Type type = Type.GetType(name);
                object[] attributes = type.GetCustomAttributes(typeof(SerializableObjectAttribute), true);
                if (attributes.Length == 0)
                    throw new IcepackException($"Type {type.AssemblyQualifiedName} is not registered for serialization!");

                RegisterType(type, false);
            }

            return types[name];
        }

        private bool IsTypeRegistered(Type type)
        {
            return IsTypeRegistered(type.AssemblyQualifiedName);
        }

        private bool IsTypeRegistered(string name)
        {
            return types.ContainsKey(name);
        }
    }
}
