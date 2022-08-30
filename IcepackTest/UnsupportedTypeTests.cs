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
    public class UnsupportedTypeTests
    {
        [Test]
        public void SerializeIntPtrInObjectField()
        {
            var serializer = new Serializer();

            ClassWithObjectField objWithNintField = new ClassWithObjectField();
            objWithNintField.Field2 = (nint)2;

            var stream = new MemoryStream();
            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(objWithNintField, stream);
            });
            stream.Close();
        }

        [Test]
        public void SerializeDelegateInObjectField()
        {
            var serializer = new Serializer();

            ClassWithObjectField objWithDelegateField = new ClassWithObjectField();
            objWithDelegateField.Field2 = delegate (int a) { return a; };

            var stream = new MemoryStream();
            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(objWithDelegateField, stream);
            });
            stream.Close();
        }

        [Test]
        public void SerializeClassWithIntPtrField()
        {
            var serializer = new Serializer();

            ClassWithIntPtrField obj = new ClassWithIntPtrField();
            obj.Field1 = 1;
            obj.Field2 = 2;
            obj.Field3 = 3;

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithIntPtrField deserializedObj = serializer.Deserialize<ClassWithIntPtrField>(stream);
            stream.Close();

            Assert.AreEqual(1, deserializedObj.Field1);
            Assert.AreEqual(default(nint), deserializedObj.Field2);
            Assert.AreEqual(3, deserializedObj.Field3);
        }

        [Test]
        public void SerializeClassWithDelegateField()
        {
            var serializer = new Serializer();

            ClassWithDelegateField obj = new ClassWithDelegateField();
            obj.Field1 = 1;
            obj.Field2 = delegate (int a) { return a; };
            obj.Field3 = 3;

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithDelegateField deserializedObj = serializer.Deserialize<ClassWithDelegateField>(stream);
            stream.Close();

            Assert.AreEqual(1, deserializedObj.Field1);
            Assert.Null(deserializedObj.Field2);
            Assert.AreEqual(3, deserializedObj.Field3);
        }
    }
}
