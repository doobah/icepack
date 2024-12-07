using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest;

public class UnregisteredTypeTests
{
    [Test]
    public void SerializeUnregisteredType()
    {
        Serializer serializer = new();

        MemoryStream stream = new();

        Assert.Throws<IcepackException>(() => {
            serializer.Serialize(typeof(UnregisteredClass), stream);
        });

        stream.Close();
    }

    [Test]
    public void SerializeUnregisteredTypeInTypeField()
    {
        Serializer serializer = new();

        ClassWithTypeField obj = new() { Field1 = 123, Field2 = typeof(UnregisteredClass), Field3 = 789 };

        MemoryStream stream = new();

        Assert.Throws<IcepackException>(() => {
            serializer.Serialize(obj, stream);
        });

        stream.Close();
    }

    [Test]
    public void SerializeUnregisteredTypeInObjectField()
    {
        Serializer serializer = new();

        ClassWithObjectField obj = new() { Field1 = 123, Field2 = typeof(UnregisteredClass), Field3 = 789 };

        MemoryStream stream = new();

        Assert.Throws<IcepackException>(() => {
            serializer.Serialize(obj, stream);
        });

        stream.Close();
    }

    [Test]
    public void SerializeUnregisteredClass()
    {
        Serializer serializer = new();
        UnregisteredClass obj = new();

        MemoryStream stream = new();
        Assert.Throws<IcepackException>(() => {
            serializer.Serialize(obj, stream);
        });
        stream.Close();
    }
}
