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
    public class EnumTests
    {
        [Test]
        public void SerializeEnum()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            serializer.Serialize(SerializableEnum.Option2, stream);
            stream.Position = 0;
            SerializableEnum deserializedEnum = serializer.Deserialize<SerializableEnum>(stream);
            stream.Close();

            Assert.AreEqual(SerializableEnum.Option2, deserializedEnum);
        }

        [Test]
        public void SerializeClassWithEnumField()
        {
            var serializer = new Serializer();

            var obj = new ClassWithEnumField() { Field1 = 123, Field2 = SerializableEnum.Option2, Field3 = 789 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithEnumField deserializedObj = serializer.Deserialize<ClassWithEnumField>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(SerializableEnum.Option2, deserializedObj.Field2);
            Assert.AreEqual(789, deserializedObj.Field3);
        }

        [Test]
        public void SerializeBoxedEnum()
        {
            var serializer = new Serializer();

            var obj = new ClassWithObjectField() { Field1 = 123, Field2 = SerializableEnum.Option2, Field3 = 789 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithObjectField deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(SerializableEnum.Option2, deserializedObj.Field2);
            Assert.AreEqual(789, deserializedObj.Field3);
        }
    }
}
