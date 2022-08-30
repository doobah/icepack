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
    public class HookTests
    {
        [Test]
        public void SerializeClassWithSerializationHooks()
        {
            var serializer = new Serializer();

            var obj = new ClassWithSerializationHooks() { Field = 123 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithSerializationHooks deserializedObj = serializer.Deserialize<ClassWithSerializationHooks>(stream);
            stream.Close();

            Assert.AreEqual(247, deserializedObj.Field);
        }

        [Test]
        public void SerializeStructWithSerializationHooks()
        {
            var serializer = new Serializer();

            var obj = new StructWithSerializationHooks() { Field = 123 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            StructWithSerializationHooks deserializedObj = serializer.Deserialize<StructWithSerializationHooks>(stream);
            stream.Close();

            Assert.AreEqual(247, deserializedObj.Field);
        }

        [Test]
        public void SerializeFieldWithStructWithSerializationHooks()
        {
            var serializer = new Serializer();

            var s = new StructWithSerializationHooks() { Field = 123 };
            var obj = new ClassWithStructWithSerializationHooksField() { Field1 = 111, Field2 = s, Field3 = 333 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithStructWithSerializationHooksField deserializedObj = serializer.Deserialize<ClassWithStructWithSerializationHooksField>(stream);
            stream.Close();

            var expectedStruct = new StructWithSerializationHooks() { Field = 247 };

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(111, deserializedObj.Field1);
            Assert.AreEqual(expectedStruct, deserializedObj.Field2);
            Assert.AreEqual(333, deserializedObj.Field3);
        }
    }
}
