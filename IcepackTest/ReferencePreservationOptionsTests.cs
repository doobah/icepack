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

        [Test]
        public void SerializeObjectOfTypeWithoutPreservingReferences()
        {
            var serializer = new Serializer();

            var obj = new ReferencePreservationTestClass();
            obj.Field1 = obj.Field2 = new ClassWithReferencePreservationDisabled();
            obj.Field3 = obj.Field4 = new ClassWithReferencePreservationEnabled();

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            var deserializedObj = serializer.Deserialize<ReferencePreservationTestClass>(stream);
            stream.Close();

            Assert.AreNotEqual(deserializedObj.Field1, deserializedObj.Field2);
            Assert.AreEqual(deserializedObj.Field3, deserializedObj.Field4);
        }

        [Test]
        public void SerializeObjectOfTypeInObjectFieldWithoutPreservingReferences()
        {
            var serializer = new Serializer();

            var obj = new ReferencePreservationTestClassWithObjectFields();
            obj.Field1 = obj.Field2 = new ClassWithReferencePreservationDisabled();
            obj.Field3 = obj.Field4 = new ClassWithReferencePreservationEnabled();

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            var deserializedObj = serializer.Deserialize<ReferencePreservationTestClassWithObjectFields>(stream);
            stream.Close();

            Assert.AreNotEqual(deserializedObj.Field1, deserializedObj.Field2);
            Assert.AreEqual(deserializedObj.Field3, deserializedObj.Field4);
        }

        [Test]
        public void SerializeObjectOfTypeWithoutPreservingReferencesUsingPreRegistration()
        {
            var serializer = new Serializer();
            serializer.RegisterType(typeof(UnregisteredClass1), new TypeSerializationSettings(preserveReferences: false));
            serializer.RegisterType(typeof(UnregisteredClass2));

            var obj = new ReferencePreservationTestClassWithObjectFields();
            obj.Field1 = obj.Field2 = new UnregisteredClass1();
            obj.Field3 = obj.Field4 = new UnregisteredClass2();

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            var deserializedObj = serializer.Deserialize<ReferencePreservationTestClassWithObjectFields>(stream);
            stream.Close();

            Assert.AreNotEqual(deserializedObj.Field1, deserializedObj.Field2);
            Assert.AreEqual(deserializedObj.Field3, deserializedObj.Field4);
        }

        [Test]
        public void SerializeObjectOfTypeInObjectListWithoutPreservingReferences()
        {
            var serializer = new Serializer();

            var obj1 = new ClassWithReferencePreservationDisabled();
            var obj2 = new ClassWithReferencePreservationEnabled();
            var list = new List<object> { obj1, obj1, obj2, obj2 };

            var stream = new MemoryStream();
            serializer.Serialize(list, stream);
            stream.Position = 0;
            var deserializedObj = serializer.Deserialize<List<object>>(stream);
            stream.Close();

            Assert.AreNotEqual(deserializedObj[0], deserializedObj[1]);
            Assert.AreEqual(deserializedObj[2], deserializedObj[3]);
        }
    }
}
