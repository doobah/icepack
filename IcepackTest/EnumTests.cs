using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest;

public class EnumTests
{
    [Test]
    public void SerializeEnum()
    {
        Serializer serializer = new();

        MemoryStream stream = new();
        serializer.Serialize(SerializableEnum.Option2, stream);
        stream.Position = 0;
        SerializableEnum deserializedEnum = serializer.Deserialize<SerializableEnum>(stream);
        stream.Close();

        Assert.That(deserializedEnum, Is.EqualTo(SerializableEnum.Option2));
    }

    [Test]
    public void SerializeClassWithEnumField()
    {
        Serializer serializer = new();

        ClassWithEnumField obj = new() { Field1 = 123, Field2 = SerializableEnum.Option2, Field3 = 789 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithEnumField? deserializedObj = serializer.Deserialize<ClassWithEnumField>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.EqualTo(SerializableEnum.Option2));
        Assert.That(deserializedObj.Field3, Is.EqualTo(789));
    }

    [Test]
    public void SerializeBoxedEnum()
    {
        Serializer serializer = new();

        ClassWithObjectField obj = new() { Field1 = 123, Field2 = SerializableEnum.Option2, Field3 = 789 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithObjectField? deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.EqualTo(SerializableEnum.Option2));
        Assert.That(deserializedObj.Field3, Is.EqualTo(789));
    }
}
