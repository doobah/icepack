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
    internal sealed class SerializationContext
    {
        /// <summary> Maps a type to metadata about the type. </summary>
        public Dictionary<Type, TypeMetadata> Types { get; }

        /// <summary> A list of type metadata in order of ID. </summary>
        public List<TypeMetadata> TypesInOrder { get; }

        /// <summary> Maps an object to metadata about the object. </summary>
        public Dictionary<object, ObjectMetadata> Objects { get; }

        /// <summary> A list of object metadata in order of ID. </summary>
        public List<ObjectMetadata> ObjectsInOrder { get; }

        /// <summary> The nesting depth of the object currently being serialized. </summary>
        public int CurrentDepth { get; set; }

        /// <summary> The serializer. </summary>
        public Serializer Serializer { get; }

        /// <summary> Keeps track of the largest assigned object ID. </summary>
        private uint largestObjectId;

        /// <summary> Keeps track of the largest assigned type ID. </summary>
        private uint largestTypeId;

        /// <summary> Creates a new serialization context. </summary>
        /// <param name="typeRegistry"> The serializer's type registry. </param>
        public SerializationContext(Serializer serializer)
        {
            Objects = new Dictionary<object, ObjectMetadata>();
            ObjectsInOrder = new List<ObjectMetadata>();
            Types = new Dictionary<Type, TypeMetadata>();
            TypesInOrder = new List<TypeMetadata>();
            CurrentDepth = -1;
            largestObjectId = 0;
            largestTypeId = 0;
            Serializer = serializer;
        }

        /// <summary> Registers an object for serialization. </summary>
        /// <param name="obj"> The object. </param>
        /// <returns> A unique ID for the object. </returns>
        public uint RegisterObject(object obj)
        {
            if (obj == null)
                return 0;

            if (Serializer.Settings.PreserveReferences && Objects.ContainsKey(obj))
                return Objects[obj].Id;

            TypeMetadata typeMetadata = GetTypeMetadata(obj.GetType());

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

            uint newId = ++largestObjectId;

            var objMetadata = new ObjectMetadata(newId, typeMetadata, length, obj, CurrentDepth + 1);
            if (Serializer.Settings.PreserveReferences)
                Objects.Add(obj, objMetadata);
            else if (CurrentDepth > Serializer.Settings.MaxDepth)
                throw new IcepackException($"Exceeded maximum depth while serializing: ${obj}");
            ObjectsInOrder.Add(objMetadata);
            
            return newId;
        }

        /// <summary>
        /// Retrieves the metadata for a type, lazy-registers types that have the <see cref="SerializableTypeAttribute"/> attribute.
        /// </summary>
        /// <param name="type"> The type to retrieve metadata for. </param>
        /// <returns> The metadata for the type. </returns>
        public TypeMetadata GetTypeMetadata(Type type)
        {
            // Treat all type values as instances of Type, for simplicity.
            if (type.IsSubclassOf(typeof(Type)))
                type = typeof(Type);

            TypeMetadata typeMetadata;
            if (Types.TryGetValue(type, out typeMetadata))
                return typeMetadata;

            // If this is an enum, we want the underlying type to be present ahead of the enum type
            TypeMetadata enumUnderlyingTypeMetadata = null;
            if (type.IsEnum)
                enumUnderlyingTypeMetadata = GetTypeMetadata(type.GetEnumUnderlyingType());

            TypeMetadata registeredTypeMetadata = Serializer.TypeRegistry.GetTypeMetadata(type);
            if (registeredTypeMetadata == null)
                throw new IcepackException($"Type {type} is not registered for serialization!");

            TypeMetadata parentTypeMetadata = null;
            if (registeredTypeMetadata.Category == TypeCategory.Class && type.BaseType != typeof(object) && type.BaseType != null)
                parentTypeMetadata = GetTypeMetadata(type.BaseType);

            TypeMetadata keyTypeMetadata = null;
            if (registeredTypeMetadata.Category == TypeCategory.Dictionary)
            {
                Type keyType = type.GenericTypeArguments[0];
                if (keyType.IsValueType)
                    keyTypeMetadata = GetTypeMetadata(keyType);
            }

            TypeMetadata itemTypeMetadata = null;
            switch (registeredTypeMetadata.Category)
            {
                case TypeCategory.Array:
                    {
                        Type itemType = type.GetElementType();
                        if (itemType.IsValueType)
                            itemTypeMetadata = GetTypeMetadata(itemType);
                        break;
                    }
                case TypeCategory.List:
                    {
                        Type itemType = type.GenericTypeArguments[0];
                        if (itemType.IsValueType)
                            itemTypeMetadata = GetTypeMetadata(itemType);
                        break;
                    }
                case TypeCategory.HashSet:
                    {
                        Type itemType = type.GenericTypeArguments[0];
                        if (itemType.IsValueType)
                            itemTypeMetadata = GetTypeMetadata(itemType);
                        break;
                    }
                case TypeCategory.Dictionary:
                    {
                        Type itemType = type.GenericTypeArguments[1];
                        if (itemType.IsValueType)
                            itemTypeMetadata = GetTypeMetadata(itemType);
                        break;
                    }
            }

            var fields = new List<FieldMetadata>();
            foreach (FieldMetadata field in registeredTypeMetadata.Fields)
                fields.Add(convertRegisteredFieldMetadataToSerializable(field));
            var fieldsByName = new Dictionary<string, FieldMetadata>();
            foreach (KeyValuePair<string, FieldMetadata> pair in registeredTypeMetadata.FieldsByName)
                fieldsByName.Add(pair.Key, convertRegisteredFieldMetadataToSerializable(pair.Value));
            var fieldsByPreviousName = new Dictionary<string, FieldMetadata>();
            foreach (KeyValuePair<string, FieldMetadata> pair in registeredTypeMetadata.FieldsByPreviousName)
                fieldsByPreviousName.Add(pair.Key, convertRegisteredFieldMetadataToSerializable(pair.Value));

            var newTypeMetadata = new TypeMetadata(registeredTypeMetadata, ++largestTypeId, enumUnderlyingTypeMetadata, parentTypeMetadata,
                                                   keyTypeMetadata, itemTypeMetadata, fields, fieldsByName, fieldsByPreviousName);
            Types.Add(type, newTypeMetadata);
            TypesInOrder.Add(newTypeMetadata);
            return newTypeMetadata;
        }

        private FieldMetadata convertRegisteredFieldMetadataToSerializable(FieldMetadata registeredFieldMetadata)
        {
            TypeMetadata fieldTypeMetadata = null;
            Type fieldType = registeredFieldMetadata.FieldInfo.FieldType;
            if (fieldType.IsValueType)
                fieldTypeMetadata = GetTypeMetadata(fieldType);
            return new FieldMetadata(registeredFieldMetadata, fieldTypeMetadata);
        }
    }
}
