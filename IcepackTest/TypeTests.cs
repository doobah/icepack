using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest;

public class TypeTests
{
    [Test]
    public void SerializeTypeInObjectField()
    {
        Serializer serializer = new();

        ClassWithObjectField obj = new() { Field1 = 123, Field2 = typeof(int), Field3 = 789 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithObjectField? deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.EqualTo(typeof(int)));
        Assert.That(deserializedObj.Field3, Is.EqualTo(789));
    }

    [Test]
    public void SerializeTypeInTypeField()
    {
        Serializer serializer = new();

        ClassWithTypeField obj = new() { Field1 = 123, Field2 = typeof(int), Field3 = 789 };

        MemoryStream stream = new();

        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithTypeField? deserializedObj = serializer.Deserialize<ClassWithTypeField>(stream);

        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.EqualTo(typeof(int)));
        Assert.That(deserializedObj.Field3, Is.EqualTo(789));
    }

    [Test]
    public void SerializeType()
    {
        Serializer serializer = new();

        MemoryStream stream = new();
        serializer.Serialize(typeof(int), stream);
        stream.Position = 0;
        Type? deserializedObj = serializer.Deserialize<Type>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void SerializeNonRegisteredType()
    {
        Serializer serializer = new();

        MemoryStream stream = new();
        Assert.Throws<IcepackException>(() => {
            serializer.Serialize(typeof(UnregisteredClass), stream);
        });
    }
}
