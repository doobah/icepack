using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest;

public class HookTests
{
    [Test]
    public void SerializeClassWithSerializationHooks()
    {
        Serializer serializer = new();

        ClassWithSerializationHooks obj = new() { Field = 123 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithSerializationHooks? deserializedObj = serializer.Deserialize<ClassWithSerializationHooks>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field, Is.EqualTo(247));
    }

    [Test]
    public void SerializeStructWithSerializationHooks()
    {
        Serializer serializer = new();

        StructWithSerializationHooks obj = new() { Field = 123 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        StructWithSerializationHooks deserializedObj = serializer.Deserialize<StructWithSerializationHooks>(stream);
        stream.Close();

        Assert.That(deserializedObj.Field, Is.EqualTo(247));
    }

    [Test]
    public void SerializeFieldWithStructWithSerializationHooks()
    {
        Serializer serializer = new();

        StructWithSerializationHooks s = new() { Field = 123 };
        ClassWithStructWithSerializationHooksField obj = new() { Field1 = 111, Field2 = s, Field3 = 333 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithStructWithSerializationHooksField? deserializedObj = serializer.Deserialize<ClassWithStructWithSerializationHooksField>(stream);
        stream.Close();

        StructWithSerializationHooks expectedStruct = new() { Field = 247 };

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.EqualTo(111));
        Assert.That(deserializedObj.Field2, Is.EqualTo(expectedStruct));
        Assert.That(deserializedObj.Field3, Is.EqualTo(333));
    }
}
