﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Icepack.Internal;

/// <summary> Contains metadata for an object. </summary>
internal class ObjectMetadata
{
    /// <summary> Deserializes the object metadata. </summary>
    /// <param name="typeMetadatas"> An array of type metadata objects. </param>
    /// <param name="reader"> Reads the metadata from the stream. </param>
    public static ObjectMetadata[] DeserializeMetadatas(TypeMetadata[] typeMetadatas, BinaryReader reader)
    {
        int numberOfObjects = reader.ReadInt32();
        ObjectMetadata[] objectMetadatas = new ObjectMetadata[numberOfObjects];
        DeserializationContext context = new(typeMetadatas, objectMetadatas);

        for (int i = 0; i < numberOfObjects; i++)
        {
            uint typeId = reader.ReadUInt32();
            TypeMetadata objectTypeMetadata = typeMetadatas![typeId - 1];
            Type? objectType = objectTypeMetadata.Type;
            int length = 0;
            object? serializedObj = null;
            object? obj = null;
            switch (objectTypeMetadata.Category)
            {
                case TypeCategory.Immutable:
                    serializedObj = objectTypeMetadata.DeserializeImmutable!(context, reader);
                    obj = serializedObj;
                    break;
                case TypeCategory.Array:
                    {
                        length = reader.ReadInt32();

                        if (objectType != null)
                        {
                            Type elementType = objectType.GetElementType()!;
                            serializedObj = Array.CreateInstance(elementType, length);
                            obj = serializedObj;
                        }
                        break;
                    }
                case TypeCategory.List:
                case TypeCategory.HashSet:
                case TypeCategory.Dictionary:
                    {
                        length = reader.ReadInt32();

                        if (objectType != null)
                        {
                            serializedObj = objectTypeMetadata.CreateCollection!(length);
                            obj = serializedObj;
                        }
                        break;
                    }
                case TypeCategory.Struct:
                case TypeCategory.Class:
                    if (objectType != null)
                    {
                        serializedObj = objectTypeMetadata.CreateClassOrStruct!();
                        if (objectTypeMetadata.HasSurrogate)
                            obj = objectTypeMetadata.CreateActualClassOrStruct!();
                        else
                            obj = serializedObj;
                    }
                    break;
                case TypeCategory.Enum:
                    // Need to deserialize regardless of whether the type still exists, to advance the stream past the value serialized in metadata.
                    object underlyingValue = objectTypeMetadata.EnumUnderlyingTypeMetadata!.DeserializeImmutable!(context, reader)!;

                    if (objectType != null)
                    {
                        serializedObj = Enum.ToObject(objectType, underlyingValue);
                        obj = serializedObj;
                    }
                    break;
                case TypeCategory.Type:
                    uint value = reader.ReadUInt32();
                    serializedObj = typeMetadatas[value - 1].Type;
                    obj = serializedObj;
                    break;
                default:
                    throw new IcepackException($"Invalid category: {objectTypeMetadata.Category}");
            }

            objectMetadatas[i] = new ObjectMetadata((uint)i + 1, objectTypeMetadata, length, obj, serializedObj, 0);
        }

        return objectMetadatas;
    }

    /// <summary> A unique ID corresponding to an object. </summary>
    public uint Id { get; init; }

    /// <summary> Metadata about the type of the object. </summary>
    public TypeMetadata TypeMetadata { get; init; }

    /// <summary> If the object is an array, list, hashset, or dictionary, this is the number of items. </summary>
    public int Length { get; init; }

    /// <summary> The actual value of the object. </summary>
    public object? Value { get; private set; }

    /// <summary> The value that was serialized. If the type has a surrogate, this is the surrogate object, otherwise it is the actual value. </summary>
    public object? SerializedValue { get; init; }

    /// <summary>
    /// The nesting depth of this object in the hierarchy. This is used to detect circular references when references are not preserved.
    /// </summary>
    public int Depth { get; init; }

    /// <summary> Creates new object metadata. </summary>
    /// <param name="id"> A unique ID corresponding to an object. </param>
    /// <param name="type"> Metadata about the type of the object. </param>
    /// <param name="length"> If the object is an array, list, hashset, or dictionary, this is the length of the object. </param>
    /// <param name="value"> The value of the object. </param>
    /// <param name="serializedValue"> The serialized value of the object. </param>
    /// <param name="depth"> The nest depth of the object. </param>
    public ObjectMetadata(uint id, TypeMetadata type, int length, object? value, object? serializedValue, int depth)
    {
        Id = id;
        TypeMetadata = type;
        Length = length;
        Value = value;
        SerializedValue = serializedValue;
        Depth = depth;
    }

    /// <summary> Serialize the object value. </summary>
    /// <param name="context"> The current serialization context. </param>
    /// <param name="writer"> Writes the data to a stream. </param>
    public void SerializeValue(SerializationContext context, BinaryWriter writer)
    {
        context.CurrentDepth = Depth;
        TypeMetadata.SerializeReferenceType!(this, context, writer);
    }

    /// <summary> Serialize the object metadata. </summary>
    /// <param name="context"> The current serialization context. </param>
    /// <param name="writer"> Writes the metadata to a stream. </param>
    public void SerializeMetadata(SerializationContext context, BinaryWriter writer)
    {
        writer.Write(TypeMetadata.Id);

        switch (TypeMetadata.Category)
        {
            case TypeCategory.Immutable:
                // "Boxed" immutable values are serialized entirely as metadata since they are unable to be
                // pre-instantiated and updated later like mutable structs and classes, and the final value
                // must be present when resolving references to these values.
                TypeMetadata.SerializeImmutable!(Value, context, writer, null);
                break;
            case TypeCategory.Array:
            case TypeCategory.List:
            case TypeCategory.HashSet:
            case TypeCategory.Dictionary:
                writer.Write(Length);
                break;
            case TypeCategory.Struct:
            case TypeCategory.Class:
                break;
            case TypeCategory.Enum:
                TypeMetadata.EnumUnderlyingTypeMetadata!.SerializeImmutable!(Value, context, writer, null);
                break;
            case TypeCategory.Type:
                // Type objects are serialized as an ID of a registered type, as metadata so that references
                // to a type object can be immediately resolved to a type.
                TypeMetadata valueTypeMetadata = context.GetTypeMetadata((Type)Value!);
                writer.Write(valueTypeMetadata.Id);
                break;
            default:
                throw new IcepackException($"Invalid type category: {TypeMetadata.Category}");
        }
    }

    /// <summary> Deserializes the referenced object. </summary>
    /// <param name="context"> The current deserialization context. </param>
    /// <param name="reader"> Reads the object data from a stream. </param>
    public void DeserializeValue(DeserializationContext context, BinaryReader reader)
    {
        TypeMetadata.DeserializeReferenceType!(this, context, reader);
    }

    public void RestoreFromSurrogate()
    {
        if (Value != SerializedValue)
        {
            Value = ((ISerializationSurrogate)SerializedValue!).Restore(Value!);
        }
    }
}
