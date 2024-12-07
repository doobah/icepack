using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest;

public class InterfaceTests
{
    [Test]
    public void SerializeInterface()
    {
        Serializer serializer = new();

        IInterface obj = new ClassThatImplementsInterface() { Value = 123 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        IInterface? deserializedObj = serializer.Deserialize<IInterface>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Value, Is.EqualTo(123));
    }

    [Test]
    public void SerializeInterfaceArray()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(IInterface));

        IInterface obj1 = new ClassThatImplementsInterface() { Value = 123 };
        IInterface obj2 = new ClassThatImplementsInterface() { Value = 456 };
        IInterface obj3 = new ClassThatImplementsInterface() { Value = 789 };

        IInterface[] obj = [ obj1, obj2, obj3 ];

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        IInterface[]? deserializedObj = serializer.Deserialize<IInterface[]>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Length, Is.EqualTo(3));
        Assert.That(deserializedObj[0].Value, Is.EqualTo(123));
        Assert.That(deserializedObj[1].Value, Is.EqualTo(456));
        Assert.That(deserializedObj[2].Value, Is.EqualTo(789));
    }

    [Test]
    public void SerializeClassWithInterfaceField()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(IInterface));

        IInterface intf = new ClassThatImplementsInterface() { Value = 456 };
        ClassWithInterfaceField obj = new()
        {
            Field1 = 123,
            Field2 = intf,
            Field3 = 789
        };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithInterfaceField? deserializedObj = serializer.Deserialize<ClassWithInterfaceField>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.Not.Null);
        Assert.That(deserializedObj.Field2!.Value, Is.EqualTo(456));
        Assert.That(deserializedObj.Field3, Is.EqualTo(789));
    }
}
