using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Icepack
{
    /// <summary> Contains metadata for an object. </summary>
    internal struct ObjectMetadata
    {
        /// <summary> A unique ID corresponding to an object. </summary>
        public uint Id { get; }

        /// <summary> Metadata about the type of the object. </summary>
        public TypeMetadata TypeMetadata { get; }

        /// <summary> If the object is an array, list, hashset, or dictionary, this is the number of items. </summary>
        public int Length { get; }

        /// <summary> The value of the object. </summary>
        public object? Value { get; }

        /// <summary>
        /// The nesting depth of this object in the hierarchy. This is used to detect circular references when references are not preserved.
        /// </summary>
        public int Depth { get; }

        /// <summary> Creates new object metadata. </summary>
        /// <param name="id"> A unique ID corresponding to an object. </param>
        /// <param name="type"> Metadata about the type of the object. </param>
        /// <param name="length"> If the object is an array, list, hashset, or dictionary, this is the length of the object. </param>
        /// <param name="value"> The value of the object. </param>
        /// <param name="depth"> The nest depth of the object. </param>
        public ObjectMetadata(uint id, TypeMetadata type, int length, object? value, int depth)
        {
            Id = id;
            TypeMetadata = type;
            Length = length;
            Value = value;
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
    }
}
