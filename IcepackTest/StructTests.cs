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
    public class StructTests
    {
        [Test]
        public void SerializeBoxedStruct()
        {
            var serializer = new Serializer();

            var s = new SerializableStruct() { Field1 = 222, Field2 = 444 };
            var obj = new ClassWithObjectField() { Field1 = 123, Field2 = s, Field3 = 789 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithObjectField deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(s, deserializedObj.Field2);
            Assert.AreEqual(789, deserializedObj.Field3);
        }

        [Test]
        public void SerializeStructAssignedToInterfaceField()
        {
            var serializer = new Serializer();

            var s = new StructThatImplementsInterface() { Value = 99999 };
            var obj = new ClassWithInterfaceField() { Field1 = 123, Field2 = s, Field3 = 789 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithInterfaceField deserializedObj = serializer.Deserialize<ClassWithInterfaceField>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(s, deserializedObj.Field2);
            Assert.AreEqual(789, deserializedObj.Field3);
        }

        [Test]
        public void SerializeStructsInInterfaceArray()
        {
            var serializer = new Serializer();
            serializer.RegisterType(typeof(IInterface));

            var s1 = new StructThatImplementsInterface() { Value = 123 };
            var s2 = new StructThatImplementsInterface() { Value = 456 };
            var s3 = new StructThatImplementsInterface() { Value = 789 };

            var array = new IInterface[] { s1, s2, s3 };

            var stream = new MemoryStream();
            serializer.Serialize(array, stream);
            stream.Position = 0;
            IInterface[] deserializedObj = serializer.Deserialize<IInterface[]>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(s1, deserializedObj[0]);
            Assert.AreEqual(s2, deserializedObj[1]);
            Assert.AreEqual(s3, deserializedObj[2]);
        }

        [Test]
        public void SerializeStruct()
        {
            var serializer = new Serializer();

            var s = new SerializableStruct() { Field1 = 123, Field2 = 456 };

            var stream = new MemoryStream();
            serializer.Serialize(s, stream);
            stream.Position = 0;
            SerializableStruct deserializedStruct = serializer.Deserialize<SerializableStruct>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedStruct.Field1);
            Assert.AreEqual(456, deserializedStruct.Field2);
        }

        [Test]
        public void SerializeStructWithObjectReferences()
        {
            var serializer = new Serializer();

            var nestedObj = new FlatClass() { Field1 = 234, Field2 = "asdf", Field3 = 1.23f };
            var obj = new StructWithObjectReferences() { Field1 = nestedObj, Field2 = 123 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            StructWithObjectReferences deserializedObj = serializer.Deserialize<StructWithObjectReferences>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj.Field1);
            Assert.AreEqual(234, deserializedObj.Field1.Field1);
            Assert.AreEqual("asdf", deserializedObj.Field1.Field2);
            Assert.AreEqual(1.23f, deserializedObj.Field1.Field3);
            Assert.AreEqual(123, deserializedObj.Field2);
        }
    }
}
