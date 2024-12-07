using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest;

public class ReferencePreservationSettingsTests
{
    [Test]
    public void SerializeWithoutPreservingReferences()
    {
        Serializer serializer = new(new SerializerSettings(preserveReferences: false));

        RegisteredClass nestedObj = new();
        ObjectWithObjectReferences obj = new() { Field1 = nestedObj, Field2 = nestedObj };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ObjectWithObjectReferences? deserializedObj = serializer.Deserialize<ObjectWithObjectReferences>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.Not.Null);
        Assert.That(deserializedObj.Field2, Is.Not.Null);
        Assert.That(deserializedObj.Field1, Is.Not.EqualTo(deserializedObj.Field2));
    }

    [Test]
    public void SerializeCircularReferenceWithoutPreservingReferences()
    {
        Serializer serializer = new(new SerializerSettings(preserveReferences: false));

        HierarchicalObject obj = new();
        obj.Field1 = 123;
        obj.Nested = obj;

        MemoryStream stream = new();
        IcepackException? exception = Assert.Throws<IcepackException>(() => {
            serializer.Serialize(obj, stream);
        });
        Assert.That(exception!.Message.Contains("Exceeded maximum depth"));
        stream.Close();
    }
}
