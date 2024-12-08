using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest;

public class SurrogateTests
{
    [Test]
    public void InvalidSurrogateTypesFailRegistration()
    {
        Serializer serializer = new();

        // Original and surrogate types must not be the same
        Assert.Throws<IcepackException>(() => {
            serializer.RegisterType(typeof(SerializableStruct), typeof(SerializableStruct));
        });

        // Surrogate type must implement ISerializationSurrogate
        Assert.Throws<IcepackException>(() => {
            serializer.RegisterType(typeof(SerializableStruct), typeof(RegisteredStruct));
        });

        // Surrogate for struct must also be struct
        Assert.Throws<IcepackException>(() => {
            serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateClass));
        });

        // Surrogate for class must also be class
        Assert.Throws<IcepackException>(() => {
            serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateClass));
        });
    }

    [Test]
    public void SerializeSurrogateForStruct()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateStruct));

        SerializableStruct obj = new() { Field1 = 1, Field2 = 2 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        SerializableStruct deserializedObj = serializer.Deserialize<SerializableStruct>(stream);
        stream.Close();

        Assert.That(deserializedObj.Field1, Is.EqualTo(2));
        Assert.That(deserializedObj.Field2, Is.EqualTo(3));
    }

    [Test]
    public void SerializeSurrogateForClass()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClass));

        SerializableClass obj = new() { Field1 = 1, Field2 = 2 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        SerializableClass? deserializedObj = serializer.Deserialize<SerializableClass>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.EqualTo(2));
        Assert.That(deserializedObj.Field2, Is.EqualTo(3));
    }
}
