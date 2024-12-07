using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest;

public class StructTests
{
    [Test]
    public void SerializeBoxedStruct()
    {
        Serializer serializer = new();

        SerializableStruct s = new() { Field1 = 222, Field2 = 444 };
        ClassWithObjectField obj = new() { Field1 = 123, Field2 = s, Field3 = 789 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithObjectField? deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.EqualTo(s));
        Assert.That(deserializedObj.Field3, Is.EqualTo(789));
    }

    [Test]
    public void SerializeStructAssignedToInterfaceField()
    {
        Serializer serializer = new();

        StructThatImplementsInterface s = new() { Value = 99999 };
        ClassWithInterfaceField obj = new() { Field1 = 123, Field2 = s, Field3 = 789 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithInterfaceField? deserializedObj = serializer.Deserialize<ClassWithInterfaceField>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.EqualTo(s));
        Assert.That(deserializedObj.Field3, Is.EqualTo(789));
    }

    [Test]
    public void SerializeStructsInInterfaceArray()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(IInterface));

        StructThatImplementsInterface s1 = new() { Value = 123 };
        StructThatImplementsInterface s2 = new() { Value = 456 };
        StructThatImplementsInterface s3 = new() { Value = 789 };

        IInterface[] array = [ s1, s2, s3 ];

        MemoryStream stream = new();
        serializer.Serialize(array, stream);
        stream.Position = 0;
        IInterface[]? deserializedObj = serializer.Deserialize<IInterface[]>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj![0], Is.EqualTo(s1));
        Assert.That(deserializedObj[1], Is.EqualTo(s2));
        Assert.That(deserializedObj[2], Is.EqualTo(s3));
    }

    [Test]
    public void SerializeStruct()
    {
        Serializer serializer = new();

        SerializableStruct s = new() { Field1 = 123, Field2 = 456 };

        MemoryStream stream = new();
        serializer.Serialize(s, stream);
        stream.Position = 0;
        SerializableStruct deserializedStruct = serializer.Deserialize<SerializableStruct>(stream);
        stream.Close();

        Assert.That(deserializedStruct.Field1, Is.EqualTo(123));
        Assert.That(deserializedStruct.Field2, Is.EqualTo(456));
    }

    [Test]
    public void SerializeStructWithObjectReferences()
    {
        Serializer serializer = new();

        FlatClass nestedObj = new() { Field1 = 234, Field2 = "asdf", Field3 = 1.23f };
        StructWithObjectReferences obj = new() { Field1 = nestedObj, Field2 = 123 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        StructWithObjectReferences deserializedObj = serializer.Deserialize<StructWithObjectReferences>(stream);
        stream.Close();

        Assert.That(deserializedObj.Field1, Is.Not.Null);
        Assert.That(deserializedObj.Field1.Field1, Is.EqualTo(234));
        Assert.That(deserializedObj.Field1.Field2, Is.EqualTo("asdf"));
        Assert.That(deserializedObj.Field1.Field3, Is.EqualTo(1.23f));
        Assert.That(deserializedObj.Field2, Is.EqualTo(123));
    }
}
