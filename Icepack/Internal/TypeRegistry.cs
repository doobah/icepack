using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Icepack.Internal;

/// <summary> Maintains a collection of type metadata used for serialization/deserialization operations. </summary>
internal sealed class TypeRegistry
{
    /// <summary> The serializer. </summary>
    private readonly Serializer serializer;

    /// <summary> Maps a type to metadata about the type. </summary>
    private readonly Dictionary<Type, TypeMetadata> types;

    public Serializer Serializer { get { return serializer; } }

    /// <summary> Creates a new type registry. </summary>
    /// <param name="serializer"> The serializer. </param>
    public TypeRegistry(Serializer serializer)
    {
        this.serializer = serializer;
        types = [];
    }

    /// <summary> Explicitly register a type for serialization. </summary>
    /// <param name="type"> The type to register for serialization. </param>
    public void RegisterType(Type type, Type? surrogateType)
    {
        if (Utils.IsUnsupportedType(type))
            throw new IcepackException($"Unsupported type: {type}");

        TypeMetadata? surrogateTypeMetadata = null;
        if (surrogateType != null)
        {
            if (Utils.IsUnsupportedType(surrogateType))
                throw new IcepackException($"Unsupported type: {surrogateType}");

            if (!surrogateType.IsAssignableTo(typeof(ISerializationSurrogate)))
                throw new IcepackException($"Invalid surrogate type: {surrogateType}. Surrogate type must implement {nameof(ISerializationSurrogate)}.");

            TypeCategory typeCategory = Utils.GetTypeCategory(type);
            TypeCategory surrogateTypeCategory = Utils.GetTypeCategory(surrogateType);
            if (surrogateTypeCategory != TypeCategory.Class && surrogateTypeCategory != TypeCategory.Struct)
                throw new IcepackException($"Invalid surrogate type: {surrogateType}. Surrogate type must be a class or struct, and not an array or collection.");

            if (surrogateTypeCategory != typeCategory)
                throw new IcepackException($"Surrogate type: {surrogateType} and original type: {type} must both be reference types or value types");

            if (surrogateType == type)
                throw new IcepackException($"Surrogate type and original type: {surrogateType} cannot be the same");

            surrogateTypeMetadata = GetTypeMetadata(surrogateType);
            if (surrogateTypeMetadata.HasSurrogate)
                throw new IcepackException($"Invalid surrogate type: {surrogateType}. Surrogate type must not have a surrogate.");
        }

        // Treat all type values as instances of Type, for simplicity.
        if (type.IsSubclassOf(typeof(Type)))
            return;

        if (types.ContainsKey(type))
            return;

        TypeMetadata newTypeMetadata = new(type, surrogateTypeMetadata, this);
        types.Add(type, newTypeMetadata);
    }

    /// <summary>
    /// Called during type registration and serialization. Retrieves the metadata for a type and
    /// lazy-registers types that have the <see cref="SerializableTypeAttribute"/> attribute.
    /// </summary>
    /// <param name="type"> The type to retrieve metadata for. </param>
    /// <returns> The metadata for the type. </returns>
    public TypeMetadata GetTypeMetadata(Type type)
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
                SerializableTypeAttribute? attr = type.GetCustomAttribute<SerializableTypeAttribute>(true);
                if (attr == null)
                    throw new IcepackException($"Type {type} is not registered for serialization!");
            }
        }
        else
        {
            SerializableTypeAttribute? attr = type.GetCustomAttribute<SerializableTypeAttribute>(true);
            if (attr == null)
                throw new IcepackException($"Type {type} is not registered for serialization!");
        }

        TypeMetadata newTypeMetadata = new(type, null, this);
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
