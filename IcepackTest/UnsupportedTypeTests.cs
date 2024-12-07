using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest;

public class UnsupportedTypeTests
{
    [Test]
    public void SerializeIntPtrInObjectField()
    {
        Serializer serializer = new();

        ClassWithObjectField objWithNintField = new();
        objWithNintField.Field2 = (nint)2;

        MemoryStream stream = new();
        Assert.Throws<IcepackException>(() => {
            serializer.Serialize(objWithNintField, stream);
        });
        stream.Close();
    }

    [Test]
    public void SerializeDelegateInObjectField()
    {
        Serializer serializer = new();

        ClassWithObjectField objWithDelegateField = new();
        objWithDelegateField.Field2 = delegate (int a) { return a; };

        MemoryStream stream = new();
        Assert.Throws<IcepackException>(() => {
            serializer.Serialize(objWithDelegateField, stream);
        });
        stream.Close();
    }

    [Test]
    public void SerializeClassWithIntPtrField()
    {
        Serializer serializer = new();

        ClassWithIntPtrField obj = new();
        obj.Field1 = 1;
        obj.Field2 = 2;
        obj.Field3 = 3;

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithIntPtrField? deserializedObj = serializer.Deserialize<ClassWithIntPtrField>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.EqualTo(1));
        Assert.That(deserializedObj.Field2, Is.EqualTo(default(nint)));
        Assert.That(deserializedObj.Field3, Is.EqualTo(3));
    }

    [Test]
    public void SerializeClassWithDelegateField()
    {
        Serializer serializer = new();

        ClassWithDelegateField obj = new();
        obj.Field1 = 1;
        obj.Field2 = delegate (int a) { return a; };
        obj.Field3 = 3;

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithDelegateField? deserializedObj = serializer.Deserialize<ClassWithDelegateField>(stream);
        stream.Close();

        Assert.That(deserializedObj!.Field1, Is.EqualTo(1));
        Assert.That(deserializedObj.Field2, Is.Null);
        Assert.That(deserializedObj.Field3, Is.EqualTo(3));
    }
}
