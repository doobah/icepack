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
    public class ReferencePreservationSettingsTests
    {
        [Test]
        public void SerializeWithoutPreservingReferences()
        {
            var serializer = new Serializer(new SerializerSettings(preserveReferences: false));

            var nestedObj = new RegisteredClass();
            var obj = new ObjectWithObjectReferences() { Field1 = nestedObj, Field2 = nestedObj };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            var deserializedObj = serializer.Deserialize<ObjectWithObjectReferences>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj.Field1);
            Assert.NotNull(deserializedObj.Field2);
            Assert.AreNotEqual(deserializedObj.Field1, deserializedObj.Field2);
        }

        [Test]
        public void SerializeCircularReferenceWithoutPreservingReferences()
        {
            var serializer = new Serializer(new SerializerSettings(preserveReferences: false));

            var obj = new HierarchicalObject();
            obj.Field1 = 123;
            obj.Nested = obj;

            var stream = new MemoryStream();
            IcepackException exception = Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });
            Assert.True(exception.Message.Contains("Exceeded maximum depth"));
            stream.Close();
        }
    }
}
