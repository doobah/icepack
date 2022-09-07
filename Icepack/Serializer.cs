using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.IO;

namespace Icepack
{
    /// <summary> Serializes/deserializes objects. </summary>
    public sealed class Serializer
    {
        /// <summary> Serializers with the same compatibility version are guaranteed to be interoperable. </summary>
        public const ushort CompatibilityVersion = 5;

        /// <summary> Keeps track of type information. </summary>
        private readonly TypeRegistry typeRegistry;

        /// <summary> Settings for the serializer. </summary>
        private readonly SerializerSettings settings;

        /// <summary> Creates a new serializer with default settings. </summary>
        public Serializer() : this(new SerializerSettings()) { }

        /// <summary> Creates a new serializer. </summary>
        public Serializer(SerializerSettings settings)
        {
            if (settings == null)
                settings = new SerializerSettings();

            typeRegistry = new TypeRegistry(settings);
            this.settings = settings;

            // Pre-register all immutable types, along with the Type type.
            RegisterType(typeof(string));
            RegisterType(typeof(byte));
            RegisterType(typeof(sbyte));
            RegisterType(typeof(char));
            RegisterType(typeof(bool));
            RegisterType(typeof(int));
            RegisterType(typeof(uint));
            RegisterType(typeof(short));
            RegisterType(typeof(ushort));
            RegisterType(typeof(long));
            RegisterType(typeof(ulong));
            RegisterType(typeof(decimal));
            RegisterType(typeof(float));
            RegisterType(typeof(double));
            RegisterType(typeof(Type));
        }

        /// <summary> Registers a type as serializable. </summary>
        /// <param name="type"> The type to register. </param>
        public void RegisterType(Type type)
        {
            if (Utils.IsUnsupportedType(type))
                throw new IcepackException($"Unsupported type: {type}");

            typeRegistry.GetTypeMetadata(type, true);
        }

        /// <summary> Serializes an object graph to a stream. </summary>
        /// <param name="rootObject"> The root object to be serialized. </param>
        /// <param name="outputStream"> The stream to output the serialized data to. </param>
        public void Serialize(object? rootObject, Stream outputStream)
        {
            var context = new SerializationContext(typeRegistry, settings);

            context.RegisterObject(rootObject);

            using (var objectDataStream = new MemoryStream())
            {
                using (var objectDataWriter = new BinaryWriter(objectDataStream, Encoding.Unicode, true))
                {
                    // Each time an object reference is encountered, a new object will be added to the list.
                    // Iterate through the growing list until there are no more objects to serialize.
                    int currentObjId = 0;
                    while (currentObjId < context.ObjectsInOrder.Count)
                    {
                        context.ObjectsInOrder[currentObjId].SerializeValue(context, objectDataWriter);
                        currentObjId++;
                    }
                }

                using (var metadataWriter = new BinaryWriter(outputStream, Encoding.Unicode, true))
                {
                    metadataWriter.Write(CompatibilityVersion);

                    // Serialize type metadata
                    metadataWriter.Write(context.Types.Count);
                    for (int typeIdx = 0; typeIdx < context.TypesInOrder.Count; typeIdx++)
                        context.TypesInOrder[typeIdx].SerializeMetadata(context, metadataWriter);

                    // Serialize object metadata
                    metadataWriter.Write(context.ObjectsInOrder.Count);
                    for (int objectIdx = 0; objectIdx < context.ObjectsInOrder.Count; objectIdx++)
                        context.ObjectsInOrder[objectIdx].SerializeMetadata(context, metadataWriter);
                }

                // The object data needs to go after the metadata in the output stream
                objectDataStream.Position = 0;
                objectDataStream.CopyTo(outputStream);
            }
        }

        /// <summary> Deserializes a data stream as an object of the specified type. </summary>
        /// <typeparam name="T"> The type of the object being deserialized. </typeparam>
        /// <param name="inputStream"> The stream containing the data to deserialize. </param>
        /// <returns> The deserialized object. </returns>
        public T? Deserialize<T>(Stream inputStream)
        {
            using (var reader = new BinaryReader(inputStream, Encoding.Unicode, true))
            {
                ushort compatibilityVersion = reader.ReadUInt16();
                if (compatibilityVersion != CompatibilityVersion)
                    throw new IcepackException($"Expected compatibility version {CompatibilityVersion}, received {compatibilityVersion}");

                TypeMetadata[] typeMetadatas = TypeMetadata.DeserializeMetadatas(typeRegistry, reader);
                ObjectMetadata[] objectMetadatas = ObjectMetadata.DeserializeMetadatas(typeMetadatas, reader);
                DeserializationContext context = new DeserializationContext(typeMetadatas, objectMetadatas);

                for (int i = 0; i < objectMetadatas.Length; i++)
                {
                    ObjectMetadata classObjMetadata = objectMetadatas[i];
                    classObjMetadata.DeserializeValue(context, reader);
                }

                if (objectMetadatas.Length == 0)
                    return default(T);
                else
                {
                    object? rootObj = objectMetadatas[0].Value;
                    if (rootObj == null)
                        return default(T);
                    else
                        return (T)rootObj;
                }
            }
        }
    }
}
