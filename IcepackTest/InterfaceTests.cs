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
    public class InterfaceTests
    {
        [Test]
        public void SerializeInterface()
        {
            var serializer = new Serializer();

            IInterface obj = new ClassThatImplementsInterface() { Value = 123 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            IInterface deserializedObj = serializer.Deserialize<IInterface>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Value);
        }

        [Test]
        public void SerializeInterfaceArray()
        {
            var serializer = new Serializer();
            serializer.RegisterType(typeof(IInterface));

            IInterface obj1 = new ClassThatImplementsInterface() { Value = 123 };
            IInterface obj2 = new ClassThatImplementsInterface() { Value = 456 };
            IInterface obj3 = new ClassThatImplementsInterface() { Value = 789 };

            IInterface[] obj = new IInterface[3] { obj1, obj2, obj3 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            IInterface[] deserializedObj = serializer.Deserialize<IInterface[]>(stream);
            stream.Close();

            Assert.AreEqual(3, deserializedObj.Length);
            Assert.AreEqual(123, deserializedObj[0].Value);
            Assert.AreEqual(456, deserializedObj[1].Value);
            Assert.AreEqual(789, deserializedObj[2].Value);
        }

        [Test]
        public void SerializeClassWithInterfaceField()
        {
            var serializer = new Serializer();
            serializer.RegisterType(typeof(IInterface));

            IInterface intf = new ClassThatImplementsInterface() { Value = 456 };
            var obj = new ClassWithInterfaceField()
            {
                Field1 = 123,
                Field2 = intf,
                Field3 = 789
            };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithInterfaceField deserializedObj = serializer.Deserialize<ClassWithInterfaceField>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.NotNull(deserializedObj.Field2);
            Assert.AreEqual(456, deserializedObj.Field2.Value);
            Assert.AreEqual(789, deserializedObj.Field3);
        }
    }
}
