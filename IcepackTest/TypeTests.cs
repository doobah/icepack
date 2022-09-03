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
    public class TypeTests
    {
        [Test]
        public void SerializeTypeInObjectField()
        {
            var serializer = new Serializer();

            var obj = new ClassWithObjectField() { Field1 = 123, Field2 = typeof(int), Field3 = 789 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithObjectField deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(typeof(int), deserializedObj.Field2);
            Assert.AreEqual(789, deserializedObj.Field3);
        }

        [Test]
        public void SerializeTypeInTypeField()
        {
            var serializer = new Serializer();

            var obj = new ClassWithTypeField() { Field1 = 123, Field2 = typeof(int), Field3 = 789 };

            var stream = new MemoryStream();

            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithTypeField deserializedObj = serializer.Deserialize<ClassWithTypeField>(stream);

            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(typeof(int), deserializedObj.Field2);
            Assert.AreEqual(789, deserializedObj.Field3);
        }

        [Test]
        public void SerializeType()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            serializer.Serialize(typeof(int), stream);
            stream.Position = 0;
            Type deserializedObj = serializer.Deserialize<Type>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(typeof(int), deserializedObj);
        }

        [Test]
        public void SerializeNonRegisteredType()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(typeof(UnregisteredClass), stream);
            });
        }
    }
}
