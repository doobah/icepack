using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Icepack
{
    internal class SerializationContext : IDisposable
    {
        public Queue<object> ObjectsToSerialize { get; }
        public Dictionary<Type, TypeMetadata> Types { get; }
        public List<TypeMetadata> TypesInOrder { get; }
        public BinaryWriter Writer { get; }
        public Dictionary<object, ObjectMetadata> Objects { get; }
        public List<ObjectMetadata> ObjectsInOrder { get; }

        private uint largestInstanceId;
        private uint largestTypeId;
        private TypeRegistry typeRegistry;

        public SerializationContext(TypeRegistry typeRegistry, Stream objectStream)
        {
            Objects = new Dictionary<object, ObjectMetadata>();
            ObjectsInOrder = new List<ObjectMetadata>();
            ObjectsToSerialize = new Queue<object>();
            Types = new Dictionary<Type, TypeMetadata>();
            TypesInOrder = new List<TypeMetadata>();
            Writer = new BinaryWriter(objectStream, Encoding.Unicode, true);

            largestInstanceId = 0;
            largestTypeId = 0;
            this.typeRegistry = typeRegistry;
        }

        public void Dispose()
        {
            Writer.Dispose();
        }

        public uint RegisterObject(object obj)
        {
            uint newId = ++largestInstanceId;
            Type type = obj.GetType();
            TypeMetadata typeMetadata = GetTypeMetadata(type);

            int arrayLength = 0;
            if (type.IsArray)
                arrayLength = ((Array)obj).Length;

            ObjectMetadata objMetadata = new ObjectMetadata(newId, typeMetadata, arrayLength);
            Objects.Add(obj, objMetadata);
            ObjectsInOrder.Add(objMetadata);
            
            return newId;
        }

        /// <summary> Retrieves the metadata for a type. </summary>
        /// <param name="type"> The type to retrieve metadata for. </param>
        /// <returns> The metadata for the type. </returns>
        /// <remarks> This method lazy-registers types that have the <see cref="SerializableObjectAttribute"/> attribute. </remarks>
        public TypeMetadata GetTypeMetadata(Type type)
        {
            if (!Types.ContainsKey(type))
            {
                uint parentId = 0;
                if (type.BaseType != typeof(object) && type.BaseType != typeof(ValueType) && type.BaseType != typeof(Array))
                    parentId = GetTypeMetadata(type.BaseType).Id;

                TypeMetadata registeredTypeMetadata = typeRegistry.GetTypeMetadata(type);
                if (registeredTypeMetadata == null)
                    throw new IcepackException($"Type {type} is not registered for serialization!");

                TypeMetadata newTypeMetadata = new TypeMetadata(registeredTypeMetadata, ++largestTypeId, parentId);
                Types.Add(type, newTypeMetadata);
                TypesInOrder.Add(newTypeMetadata);
                return newTypeMetadata;
            }

            return Types[type];
        }
    }
}
