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
        // Original and surrogate types must not be the same
        Assert.Throws<IcepackException>(() => {
            Serializer serializer = new();
            serializer.RegisterType(typeof(SerializableStruct), typeof(SerializableStruct));
        });

        // Surrogate type must implement ISerializationSurrogate
        Assert.Throws<IcepackException>(() => {
            Serializer serializer = new();
            serializer.RegisterType(typeof(SerializableStruct), typeof(RegisteredStruct));
        });

        // Surrogate for struct must also be struct
        Assert.Throws<IcepackException>(() => {
            Serializer serializer = new();
            serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateClass));
        });

        // Surrogate for class must also be class
        Assert.Throws<IcepackException>(() => {
            Serializer serializer = new();
            serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateClass));
        });

        // Surrogate must be registered or marked as serializable
        Assert.Throws<IcepackException>(() => {
            Serializer serializer = new();
            serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateStruct));
        });

        // Surrogate must not have surrogate
        Assert.Throws<IcepackException>(() => {
            Serializer serializer = new();
            serializer.RegisterType(typeof(SurrogateClass), typeof(RegisteredClass));
            serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClass));
        });

        // Registering original struct with surrogate struct that contains original struct will throw an error
        Assert.Throws<IcepackException>(() => {
            Serializer serializer = new();
            serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateStructContainingOriginalStruct));
        });

        // Surrogate type will be lazy-registered if marked as serializable
        Assert.DoesNotThrow(() => {
            Serializer serializer = new();
            serializer.RegisterType(typeof(SerializableClass), typeof(SerializableSurrogateClass));
        });
    }

    [Test]
    public void SerializeSurrogateForStruct()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateStruct));
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
        serializer.RegisterType(typeof(SurrogateClass));
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

    [Test]
    public void SerializeStructWithSurrogateForStruct()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateStruct));
        serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateStruct));

        StructWithStruct obj = new() { Field = new() { Field1 = 1, Field2 = 2 } };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        StructWithStruct deserializedObj = serializer.Deserialize<StructWithStruct>(stream);
        stream.Close();

        Assert.That(deserializedObj.Field.Field1, Is.EqualTo(2));
        Assert.That(deserializedObj.Field.Field2, Is.EqualTo(3));
    }

    [Test]
    public void SerializeStructWithSurrogateForClass()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateClass));
        serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClass));

        StructWithClass obj = new() { Field = new() { Field1 = 1, Field2 = 2 } };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        StructWithClass deserializedObj = serializer.Deserialize<StructWithClass>(stream);
        stream.Close();

        Assert.That(deserializedObj.Field.Field1, Is.EqualTo(2));
        Assert.That(deserializedObj.Field.Field2, Is.EqualTo(3));
    }

    [Test]
    public void SerializeClassWithSurrogateForStruct()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateStruct));
        serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateStruct));

        ClassWithStruct obj = new() { Field = new() { Field1 = 1, Field2 = 2 } };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithStruct deserializedObj = serializer.Deserialize<ClassWithStruct>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj.Field.Field1, Is.EqualTo(2));
        Assert.That(deserializedObj.Field.Field2, Is.EqualTo(3));
    }

    [Test]
    public void SerializeClassWithSurrogateForClass()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateClass));
        serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClass));

        ClassWithClass obj = new() { Field = new() { Field1 = 1, Field2 = 2 } };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithClass deserializedObj = serializer.Deserialize<ClassWithClass>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj.Field.Field1, Is.EqualTo(2));
        Assert.That(deserializedObj.Field.Field2, Is.EqualTo(3));
    }

    [Test]
    public void SerializeArrayWithSurrogateStructForElementType()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateStruct));
        serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateStruct));

        SerializableStruct[] obj = [ new() { Field1 = 1, Field2 = 2 }, new() { Field1 = 3, Field2 = 4 } ];

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        SerializableStruct[]? deserializedObj = serializer.Deserialize<SerializableStruct[]>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Length, Is.EqualTo(2));
        Assert.That(deserializedObj[0].Field1, Is.EqualTo(2));
        Assert.That(deserializedObj[0].Field2, Is.EqualTo(3));
        Assert.That(deserializedObj[1].Field1, Is.EqualTo(4));
        Assert.That(deserializedObj[1].Field2, Is.EqualTo(5));
    }

    [Test]
    public void SerializeArrayWithSurrogateClassForElementType()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateClass));
        serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClass));

        SerializableClass[] obj = [new() { Field1 = 1, Field2 = 2 }, new() { Field1 = 3, Field2 = 4 }];

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        SerializableClass[]? deserializedObj = serializer.Deserialize<SerializableClass[]>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Length, Is.EqualTo(2));
        Assert.That(deserializedObj[0].Field1, Is.EqualTo(2));
        Assert.That(deserializedObj[0].Field2, Is.EqualTo(3));
        Assert.That(deserializedObj[1].Field1, Is.EqualTo(4));
        Assert.That(deserializedObj[1].Field2, Is.EqualTo(5));
    }

    [Test]
    public void SerializeListWithSurrogateStructForElementType()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateStruct));
        serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateStruct));

        List<SerializableStruct> obj = [new() { Field1 = 1, Field2 = 2 }, new() { Field1 = 3, Field2 = 4 }];

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        List<SerializableStruct>? deserializedObj = serializer.Deserialize<List<SerializableStruct>>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Count, Is.EqualTo(2));
        Assert.That(deserializedObj[0].Field1, Is.EqualTo(2));
        Assert.That(deserializedObj[0].Field2, Is.EqualTo(3));
        Assert.That(deserializedObj[1].Field1, Is.EqualTo(4));
        Assert.That(deserializedObj[1].Field2, Is.EqualTo(5));
    }

    [Test]
    public void SerializeListWithSurrogateClassForElementType()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateClass));
        serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClass));

        List<SerializableClass> obj = [new() { Field1 = 1, Field2 = 2 }, new() { Field1 = 3, Field2 = 4 }];

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        List<SerializableClass>? deserializedObj = serializer.Deserialize<List<SerializableClass>>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Count, Is.EqualTo(2));
        Assert.That(deserializedObj[0].Field1, Is.EqualTo(2));
        Assert.That(deserializedObj[0].Field2, Is.EqualTo(3));
        Assert.That(deserializedObj[1].Field1, Is.EqualTo(4));
        Assert.That(deserializedObj[1].Field2, Is.EqualTo(5));
    }

    [Test]
    public void SerializeHashSetWithSurrogateStructForElementType()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateStruct));
        serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateStruct));

        HashSet<SerializableStruct> obj = [new() { Field1 = 1, Field2 = 2 }, new() { Field1 = 3, Field2 = 4 }];

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        HashSet<SerializableStruct>? deserializedObj = serializer.Deserialize<HashSet<SerializableStruct>>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Count, Is.EqualTo(2));
        Assert.That(deserializedObj.Any(x => x.Field1 == 2 && x.Field2 == 3));
        Assert.That(deserializedObj.Any(x => x.Field1 == 4 && x.Field2 == 5));
    }

    [Test]
    public void SerializeHashSetWithSurrogateClassForElementType()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateClass));
        serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClass));

        HashSet<SerializableClass> obj = [new() { Field1 = 1, Field2 = 2 }, new() { Field1 = 3, Field2 = 4 }];

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        HashSet<SerializableClass>? deserializedObj = serializer.Deserialize<HashSet<SerializableClass>>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Count, Is.EqualTo(2));
        Assert.That(deserializedObj.Any(x => x.Field1 == 2 && x.Field2 == 3));
        Assert.That(deserializedObj.Any(x => x.Field1 == 4 && x.Field2 == 5));
    }

    [Test]
    public void SerializeDictionaryKeyWithSurrogateStructForElementType()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateStruct));
        serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateStruct));

        Dictionary<SerializableStruct, int> obj = new() { { new() { Field1 = 1, Field2 = 2 }, 1 } };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        Dictionary<SerializableStruct, int>? deserializedObj = serializer.Deserialize<Dictionary<SerializableStruct, int>>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Count, Is.EqualTo(1));
        Assert.That(deserializedObj.Keys.Any(x => x.Field1 == 2 && x.Field2 == 3));
    }

    [Test]
    public void SerializeDictionaryKeyWithSurrogateClassForElementType()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateClass));
        serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClass));

        Dictionary<SerializableClass, int> obj = new() { { new() { Field1 = 1, Field2 = 2 }, 1 } };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        Dictionary<SerializableClass, int>? deserializedObj = serializer.Deserialize<Dictionary<SerializableClass, int>>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Count, Is.EqualTo(1));
        Assert.That(deserializedObj.Keys.Any(x => x.Field1 == 2 && x.Field2 == 3));
    }

    [Test]
    public void SerializeDictionaryValueWithSurrogateStructForElementType()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateStruct));
        serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateStruct));

        Dictionary<int, SerializableStruct> obj = new() { { 1, new() { Field1 = 1, Field2 = 2 } } };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        Dictionary<int, SerializableStruct>? deserializedObj = serializer.Deserialize<Dictionary<int, SerializableStruct>>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Count, Is.EqualTo(1));
        Assert.That(deserializedObj.Values.Any(x => x.Field1 == 2 && x.Field2 == 3));
    }

    [Test]
    public void SerializeDictionaryValueWithSurrogateClassForElementType()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateClass));
        serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClass));

        Dictionary<int, SerializableClass> obj = new() { { 1, new() { Field1 = 1, Field2 = 2 } } };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        Dictionary<int, SerializableClass>? deserializedObj = serializer.Deserialize<Dictionary<int, SerializableClass>>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Count, Is.EqualTo(1));
        Assert.That(deserializedObj.Values.Any(x => x.Field1 == 2 && x.Field2 == 3));
    }

    [Test]
    public void SurrogateClassCanBeRegisteredSeparately()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateClass));
        serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClass));
        serializer.RegisterType(typeof(ClassWithOriginalAndSurrogateFields));

        ClassWithOriginalAndSurrogateFields obj = new() { Original = new() { Field1 = 1, Field2 = 2 }, Surrogate = new() { Field1 = "1", Field2 = "2" } };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ClassWithOriginalAndSurrogateFields? deserializedObj = serializer.Deserialize<ClassWithOriginalAndSurrogateFields>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Original, Is.Not.Null);
        Assert.That(deserializedObj.Surrogate, Is.Not.Null);
        Assert.That(deserializedObj.Original!.Field1, Is.EqualTo(2));
        Assert.That(deserializedObj.Original.Field2, Is.EqualTo(3));
        Assert.That(deserializedObj.Surrogate!.Field1, Is.EqualTo("1"));
        Assert.That(deserializedObj.Surrogate.Field2, Is.EqualTo("2"));
    }

    [Test]
    public void SerializingSurrogatesPreservesReferences()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(RegisteredSurrogateClass));
        serializer.RegisterType(typeof(RegisteredClass), typeof(RegisteredSurrogateClass));
        serializer.RegisterType(typeof(ObjectWithObjectReferences));

        RegisteredClass refTypeObj = new();
        ObjectWithObjectReferences obj = new() { Field1 = refTypeObj, Field2 = refTypeObj };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        ObjectWithObjectReferences? deserializedObj = serializer.Deserialize<ObjectWithObjectReferences>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field1, Is.EqualTo(deserializedObj.Field2));
    }

    [Test]
    public void SurrogateContainingOriginalTypeShouldExceedSerializationDepth()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClassContainingOriginalClass));

        SerializableClass obj = new() { Field1 = 1, Field2 = 2 };

        MemoryStream stream = new();
        // Original type gets swapped with surrogate which has nested original type that gets swapped, ad infinitum
        Assert.Throws<IcepackException>(() => {
            serializer.Serialize(obj, stream);
        });
        stream.Close();
    }

    [Test]
    public void SurrogateOfAGenericTypeParameterType()
    {
        Serializer serializer = new();
        serializer.RegisterType(typeof(SurrogateClass));
        serializer.RegisterType(typeof(SerializableClass), typeof(SurrogateClass));
        serializer.RegisterType(typeof(GenericClass<SerializableClass>));

        GenericClass<SerializableClass> obj = new() { Field = new() { Field1 = 1, Field2 = 2 } };

        MemoryStream stream = new();
        serializer.Serialize(obj, stream);
        stream.Position = 0;
        GenericClass<SerializableClass>? deserializedObj = serializer.Deserialize<GenericClass<SerializableClass>>(stream);
        stream.Close();

        Assert.That(deserializedObj, Is.Not.Null);
        Assert.That(deserializedObj!.Field, Is.Not.Null);
        Assert.That(deserializedObj.Field!.Field1, Is.EqualTo(2));
        Assert.That(deserializedObj.Field.Field2, Is.EqualTo(3));
    }

    [Test]
    public void SurrogateOfAChildClass()
    {
        // TODO: Implement
    }

    [Test]
    public void SurrogateOfAParentClass()
    {
        // TODO: Implement
    }
}
