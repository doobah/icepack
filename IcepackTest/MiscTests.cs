using NUnit.Framework;
using Icepack;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

namespace IcepackTest
{
    public class MiscTests
    {
        [Test]
        public void SerializeClassWithPrivateConstructor()
        {
            var serializer = new Serializer();

            ClassWithPrivateConstructor obj = ClassWithPrivateConstructor.Create();

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithPrivateConstructor deserializedObj = serializer.Deserialize<ClassWithPrivateConstructor>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Field);
        }

        [Test]
        public void SerializeClassWithReadonlyField()
        {
            var serializer = new Serializer();

            var obj = new ClassWithReadonlyField();

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithReadonlyField deserializedObj = serializer.Deserialize<ClassWithReadonlyField>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Field);
        }

        [Test]
        public void SerializeFlatObject()
        {
            var serializer = new Serializer();

            var obj = new FlatClass() { Field1 = 123, Field2 = "asdf", Field3 = 6.78f };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual("asdf", deserializedObj.Field2);
            Assert.AreEqual(6.78f, deserializedObj.Field3);
        }

        [Test]
        public void SerializeFlatObjectWithNullString()
        {
            var serializer = new Serializer();

            var obj = new FlatClass() { Field1 = 123, Field2 = null, Field3 = 6.78f };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(null, deserializedObj.Field2);
            Assert.AreEqual(6.78f, deserializedObj.Field3);
        }

        [Test]
        public void SerializeHierarchicalObject()
        {
            var serializer = new Serializer();

            var nestedObj = new HierarchicalObject() { Field1 = 123, Nested = null };
            var rootObj = new HierarchicalObject() { Field1 = 456, Nested = nestedObj };

            var stream = new MemoryStream();
            serializer.Serialize(rootObj, stream);
            stream.Position = 0;
            HierarchicalObject deserializedObj = serializer.Deserialize<HierarchicalObject>(stream);
            stream.Close();

            Assert.AreEqual(456, deserializedObj.Field1);
            Assert.NotNull(deserializedObj.Nested);

            HierarchicalObject deserializedNestedObj = deserializedObj.Nested;

            Assert.AreEqual(123, deserializedNestedObj.Field1);
            Assert.Null(deserializedNestedObj.Nested);
        }

        [Test]
        public void SerializeDerivedObjectWithSameFieldNameAsParent()
        {
            var serializer = new Serializer();

            var obj = new ChildClass(123, 456);

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ChildClass deserializedObj = serializer.Deserialize<ChildClass>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Field);
            Assert.AreEqual(456, deserializedObj.ParentField);
        }

        [Test]
        public void SerializeObjectWithIgnoredField()
        {
            var serializer = new Serializer();

            var obj = new ObjectWithIgnoredField() { Field1 = 123, Field2 = 456 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ObjectWithIgnoredField deserializedObj = serializer.Deserialize<ObjectWithIgnoredField>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(0, deserializedObj.Field2);
        }

        [Test]
        public void SerializeObjectReferences()
        {
            var serializer = new Serializer();

            var nestedObj = new RegisteredClass();
            var obj = new ObjectWithObjectReferences() { Field1 = nestedObj, Field2 = nestedObj };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ObjectWithObjectReferences deserializedObj = serializer.Deserialize<ObjectWithObjectReferences>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj.Field1);
            Assert.NotNull(deserializedObj.Field2);
            Assert.AreEqual(deserializedObj.Field1, deserializedObj.Field2);
        }

        [Test]
        public void SerializeBoxedInt()
        {
            var serializer = new Serializer();

            var obj = new ClassWithObjectField() { Field1 = 123, Field2 = 456, Field3 = 789 };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithObjectField deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(456, deserializedObj.Field2);
            Assert.AreEqual(789, deserializedObj.Field3);
        }

        [Test]
        public void SerializeReferenceTypeInObjectField()
        {
            var serializer = new Serializer();

            var obj = new ClassWithMultipleObjectFields()
            {
                Field1 = new RegisteredClass(),
                Field2 = new ClassWithIntField() { Field1 = 123 },
                Field3 = new RegisteredClass()
            };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithMultipleObjectFields deserializedObj = serializer.Deserialize<ClassWithMultipleObjectFields>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.NotNull(deserializedObj.Field1);
            Assert.IsTrue(deserializedObj.Field1.GetType() == typeof(RegisteredClass));
            Assert.NotNull(deserializedObj.Field2);
            Assert.IsTrue(deserializedObj.Field2.GetType() == typeof(ClassWithIntField));
            Assert.AreEqual(123, ((ClassWithIntField)deserializedObj.Field2).Field1);
            Assert.NotNull(deserializedObj.Field3);
            Assert.IsTrue(deserializedObj.Field3.GetType() == typeof(RegisteredClass));
        }

        [Test]
        public void SerializeClassWithNoDefaultConstructor()
        {
            var serializer = new Serializer();

            ClassWithNoDefaultConstructor obj = new ClassWithNoDefaultConstructor(123);

            var stream = new MemoryStream();
            IcepackException exception = Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });
            Assert.True(exception.Message.Contains("does not have a default constructor"));
            stream.Close();
        }

        [Test]
        public void SerializeClassWithoutSerializeByDefault()
        {
            var serializer = new Serializer(new SerializerSettings(serializeByDefault: false));

            ClassWithSerializedField obj = new ClassWithSerializedField();
            obj.Field1 = 123;
            obj.Field2 = 456;

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ClassWithSerializedField deserializedObj = serializer.Deserialize<ClassWithSerializedField>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(0, deserializedObj.Field2);
        }

        [Test]
        public void SerializeGenericClass()
        {
            var serializer = new Serializer();

            GenericClass<string> obj = new GenericClass<string>();
            obj.Field = "asdf";

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            GenericClass<string> deserializedObj = serializer.Deserialize<GenericClass<string>>(stream);
            stream.Close();

            Assert.AreEqual("asdf", deserializedObj.Field);
        }

        [Test]
        public void SerializeNull()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            serializer.Serialize(null, stream);
            stream.Position = 0;
            object deserializedObj = serializer.Deserialize<object>(stream);
            stream.Close();

            Assert.Null(deserializedObj);
        }

        [Test]
        public void SerializeObject()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            serializer.Serialize(new object(), stream);
            stream.Position = 0;
            object deserializedObj = serializer.Deserialize<object>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
        }
    }
}
