using NUnit.Framework;
using Icepack;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

namespace IcepackTest;

public class MiscTests
{
    [Test]
    public void SerializeClassWithPrivateConstructor()
    {
        Serializer serializer = new();

        ClassWithPrivateConstructor obj = ClassWithPrivateConstructor.Create();

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithPrivateConstructor deserializedObj = serializer.Deserialize<ClassWithPrivateConstructor>(stream)!;
        stream.Close();

        Assert.That(deserializedObj.Field, Is.EqualTo(123));
    }

    [Test]
    public void SerializeClassWithReadonlyField()
    {
        Serializer serializer = new();

        ClassWithReadonlyField obj = new();

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithReadonlyField deserializedObj = serializer.Deserialize<ClassWithReadonlyField>(stream)!;
        stream.Close();

        Assert.That(deserializedObj.Field, Is.EqualTo(123));
    }

    [Test]
    public void SerializeFlatObject()
    {
        Serializer serializer = new();

        FlatClass obj = new() { Field1 = 123, Field2 = "asdf", Field3 = 6.78f };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream)!;
        stream.Close();

        Assert.That(deserializedObj.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.EqualTo("asdf"));
        Assert.That(deserializedObj.Field3, Is.EqualTo(6.78f));
    }

    [Test]
    public void SerializeFlatObjectWithNullString()
    {
        Serializer serializer = new();

        FlatClass obj = new() { Field1 = 123, Field2 = null, Field3 = 6.78f };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream)!;
        stream.Close();

        Assert.That(deserializedObj.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.EqualTo(null));
        Assert.That(deserializedObj.Field3, Is.EqualTo(6.78f));
    }

    [Test]
    public void SerializeHierarchicalObject()
    {
        Serializer serializer = new();

        HierarchicalObject nestedObj = new() { Field1 = 123, Nested = null };
        HierarchicalObject rootObj = new() { Field1 = 456, Nested = nestedObj };

        MemoryStream stream = new();
        serializer.Serialize(rootObj, stream);
        stream.Position = 0;
        HierarchicalObject deserializedObj = serializer.Deserialize<HierarchicalObject>(stream)!;
        stream.Close();

        Assert.That(deserializedObj.Field1, Is.EqualTo(456));
        Assert.That(deserializedObj.Nested, Is.Not.Null);

        HierarchicalObject deserializedNestedObj = deserializedObj.Nested!;

        Assert.That(deserializedNestedObj.Field1, Is.EqualTo(123));
        Assert.That(deserializedNestedObj.Nested, Is.Null);
    }

    [Test]
    public void SerializeDerivedObjectWithSameFieldNameAsParent()
    {
        Serializer serializer = new();

        ChildClass obj = new(123, 456);

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ChildClass deserializedObj = serializer.Deserialize<ChildClass>(stream)!;
        stream.Close();

        Assert.That(deserializedObj.Field, Is.EqualTo(123));
        Assert.That(deserializedObj.ParentField, Is.EqualTo(456));
    }

    [Test]
    public void SerializeObjectWithIgnoredField()
    {
        Serializer serializer = new();

        ObjectWithIgnoredField obj = new() { Field1 = 123, Field2 = 456 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ObjectWithIgnoredField deserializedObj = serializer.Deserialize<ObjectWithIgnoredField>(stream)!;
        stream.Close();

        Assert.That(deserializedObj.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.EqualTo(0));
    }

    [Test]
    public void SerializeObjectReferences()
    {
        Serializer serializer = new();

        RegisteredClass nestedObj = new();
        ObjectWithObjectReferences obj = new() { Field1 = nestedObj, Field2 = nestedObj };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ObjectWithObjectReferences deserializedObj = serializer.Deserialize<ObjectWithObjectReferences>(stream)!;
        stream.Close();

        Assert.That(deserializedObj.Field1, Is.Not.Null);
        Assert.That(deserializedObj.Field2, Is.Not.Null);
        Assert.That(deserializedObj.Field1, Is.EqualTo(deserializedObj.Field2));
    }

    [Test]
    public void SerializeBoxedInt()
    {
        Serializer serializer = new();

        ClassWithObjectField obj = new() { Field1 = 123, Field2 = 456, Field3 = 789 };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithObjectField deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream)!;
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.EqualTo(456));
        Assert.That(deserializedObj.Field3, Is.EqualTo(789));
    }

    [Test]
    public void SerializeReferenceTypeInObjectField()
    {
        Serializer serializer = new();

        ClassWithMultipleObjectFields obj = new()
        {
            Field1 = new RegisteredClass(),
            Field2 = new ClassWithIntField() { Field1 = 123 },
            Field3 = new RegisteredClass()
        };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithMultipleObjectFields deserializedObj = serializer.Deserialize<ClassWithMultipleObjectFields>(stream)!;
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj.Field1, Is.Not.Null);
        Assert.That(deserializedObj.Field1!.GetType() == typeof(RegisteredClass));
        Assert.That(deserializedObj.Field2, Is.Not.Null);
        Assert.That(deserializedObj.Field2!.GetType() == typeof(ClassWithIntField));
        Assert.That(((ClassWithIntField)deserializedObj.Field2).Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field3, Is.Not.Null);
        Assert.That(deserializedObj.Field3!.GetType() == typeof(RegisteredClass));
    }

    [Test]
    public void SerializeClassWithNoDefaultConstructor()
    {
        Serializer serializer = new();

        ClassWithNoDefaultConstructor obj = new(123);

        MemoryStream stream = new();
        IcepackException exception = Assert.Throws<IcepackException>(() => {
            serializer.Serialize(obj, stream);
        })!;
        Assert.That(exception.Message.Contains("does not have a default constructor"));
        stream.Close();
    }

    [Test]
    public void SerializeClassWithoutSerializeByDefault()
    {
        Serializer serializer = new(new SerializerSettings(serializeByDefault: false));

        ClassWithSerializedField obj = new();
        obj.Field1 = 123;
        obj.Field2 = 456;

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithSerializedField deserializedObj = serializer.Deserialize<ClassWithSerializedField>(stream)!;
        stream.Close();

        Assert.That(deserializedObj.Field1, Is.EqualTo(123));
        Assert.That(deserializedObj.Field2, Is.EqualTo(0));
    }

    [Test]
    public void SerializeGenericClass()
    {
        Serializer serializer = new();

        GenericClass<string> obj = new();
        obj.Field = "asdf";

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        GenericClass<string> deserializedObj = serializer.Deserialize<GenericClass<string>>(stream)!;
        stream.Close();

        Assert.That(deserializedObj.Field, Is.EqualTo("asdf"));
    }

    [Test]
    public void SerializeNull()
    {
        Serializer serializer = new();

        MemoryStream stream = new();
        serializer.Serialize(null, stream);
        stream.Position = 0;
        object? deserializedObj = serializer.Deserialize<object>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Null);
    }

    [Test]
    public void SerializeObject()
    {
        Serializer serializer = new();

        MemoryStream stream = new();
        serializer.Serialize(new object(), stream);
        stream.Position = 0;
        object? deserializedObj = serializer.Deserialize<object>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
    }
}
