using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest;

public class RegistrationTests
{
    [Test]
    public void RegisterGenericClassWithUnregisteredParameterType()
    {
        Serializer serializer = new();
        Assert.DoesNotThrow(() => {
            serializer.RegisterType(typeof(GenericClass<UnregisteredClass>));
        });
    }

    [Test]
    public void PreRegisterListWithUnregisteredParameterType()
    {
        Serializer serializer = new();
        Assert.DoesNotThrow(() => {
            serializer.RegisterType(typeof(List<UnregisteredClass>));
        });
    }

    [Test]
    public void RegisterClassWithUnregisteredStructFieldType()
    {
        Serializer serializer = new();
        Assert.Throws<IcepackException>(() => {
            serializer.RegisterType(typeof(ClassWithUnregisteredStructFieldType));
        });
    }

    [Test]
    public void LazyRegisterAnnotatedListParameterType()
    {
        Serializer serializer = new();

        List<RegisteredClass> obj = [];

        MemoryStream stream = new();
        Assert.DoesNotThrow(() => {
            serializer.Serialize(obj, stream);
        });
        stream.Close();
    }

    [Test]
    public void LazyRegisterAnnotatedStructFieldType()
    {
        Serializer serializer = new();

        ClassWithRegisteredStructFieldType obj = new();

        MemoryStream stream = new();
        Assert.DoesNotThrow(() => {
            serializer.Serialize(obj, stream);
        });
        stream.Close();
    }

    /// <remarks>
    /// Serializing a List type with a non-annotated parameter type should pass as long as it has no elements of that type.
    /// Note that it may never have elements of that type if the type is an abstract base class.
    /// </remarks>
    [Test]
    public void LazyRegisterNonAnnotatedListParameterType()
    {
        Serializer serializer = new();

        List<UnregisteredClass> obj = [];

        MemoryStream stream = new();
        Assert.DoesNotThrow(() => {
            serializer.Serialize(obj, stream);
        });
        stream.Close();
    }

    [Test]
    public void LazyRegisterNonAnnotatedStructFieldType()
    {
        Serializer serializer = new();

        ClassWithUnregisteredStructFieldType obj = new();

        MemoryStream stream = new();
        Assert.Throws<IcepackException>(() => {
            serializer.Serialize(obj, stream);
        });
        stream.Close();
    }

    [Test]
    public void RegisterDependantStructBeforeDependency()
    {
        Serializer serializer = new();

        Assert.Throws<IcepackException>(() => {
            serializer.RegisterType(typeof(StructWithNestedStruct));
        });
    }

    [Test]
    public void RegisterDependencyStructBeforeDependant()
    {
        Serializer serializer = new();

        serializer.RegisterType(typeof(NestedStruct));
        serializer.RegisterType(typeof(StructWithNestedStruct));
    }

    [Test]
    public void RegisterUnsupportedTypes()
    {
        Serializer serializer = new();

        {
            IcepackException? exception = Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(IntPtr));
            });
            Assert.That(exception!.Message.StartsWith("Unsupported type"));
        }

        {
            IcepackException? exception = Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(UIntPtr));
            });
            Assert.That(exception!.Message.StartsWith("Unsupported type"));
        }

        {
            IcepackException? exception = Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(Delegate));
            });
            Assert.That(exception!.Message.StartsWith("Unsupported type"));
        }

        {
            IcepackException? exception = Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(Del));
            });
            Assert.That(exception!.Message.StartsWith("Unsupported type"));
        }

        {
            IcepackException? exception = Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(int*));
            });
            Assert.That(exception!.Message.StartsWith("Unsupported type"));
        }

        {
            IcepackException? exception = Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(Span<float>));
            });
            Assert.That(exception!.Message.StartsWith("Unsupported type"));
        }

        {
            IcepackException? exception = Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(List<>));
            });
            Assert.That(exception!.Message.StartsWith("Unsupported type"));
        }
    }
}
