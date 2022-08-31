using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest
{
    public class RegistrationTests
    {
        [Test]
        public void RegisterGenericClassWithUnregisteredParameterType()
        {
            var serializer = new Serializer();
            Assert.DoesNotThrow(() => {
                serializer.RegisterType(typeof(GenericClass<UnregisteredClass>));
            });
        }

        [Test]
        public void PreRegisterListWithUnregisteredParameterType()
        {
            var serializer = new Serializer();
            Assert.DoesNotThrow(() => {
                serializer.RegisterType(typeof(List<UnregisteredClass>));
            });
        }

        [Test]
        public void RegisterClassWithUnregisteredStructFieldType()
        {
            var serializer = new Serializer();
            Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(ClassWithUnregisteredStructFieldType));
            });
        }

        [Test]
        public void LazyRegisterAnnotatedListParameterType()
        {
            var serializer = new Serializer();

            List<RegisteredClass> obj = new List<RegisteredClass>();

            var stream = new MemoryStream();
            Assert.DoesNotThrow(() => {
                serializer.Serialize(obj, stream);
            });
            stream.Close();
        }

        [Test]
        public void LazyRegisterAnnotatedStructFieldType()
        {
            var serializer = new Serializer();

            ClassWithRegisteredStructFieldType obj = new ClassWithRegisteredStructFieldType();

            var stream = new MemoryStream();
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
            var serializer = new Serializer();

            List<UnregisteredClass> obj = new List<UnregisteredClass>();

            var stream = new MemoryStream();
            Assert.DoesNotThrow(() => {
                serializer.Serialize(obj, stream);
            });
            stream.Close();
        }

        [Test]
        public void LazyRegisterNonAnnotatedStructFieldType()
        {
            var serializer = new Serializer();

            ClassWithUnregisteredStructFieldType obj = new ClassWithUnregisteredStructFieldType();

            var stream = new MemoryStream();
            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });
            stream.Close();
        }

        [Test]
        public void RegisterDependantStructBeforeDependency()
        {
            var serializer = new Serializer();

            Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(StructWithNestedStruct));
            });
        }

        [Test]
        public void RegisterDependencyStructBeforeDependant()
        {
            var serializer = new Serializer();

            serializer.RegisterType(typeof(NestedStruct));
            serializer.RegisterType(typeof(StructWithNestedStruct));
        }

        [Test]
        public void RegisterUnsupportedTypes()
        {
            var serializer = new Serializer();

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(IntPtr));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(UIntPtr));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(Delegate));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(Del));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(int*));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(Span<float>));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(List<>));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }
        }
    }
}
