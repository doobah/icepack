using NUnit.Framework;
using Icepack;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace IcepackTest
{
    public class SerializationTests
    {
        [SerializableObject]
        private class FlatClass
        {
            public int Field1;
            public string Field2;
            public float Field3;
        }

        [SerializableObject]
        private struct SerializableStruct
        {
            public int Field1;
            public int Field2;
        }

        [SerializableObject]
        private class HierarchicalObject
        {
            public int Field1;
            public HierarchicalObject Nested;
        }

        [SerializableObject]
        private class ParentClass
        {
            private int field;

            public ParentClass(int field)
            {
                this.field = field;
            }

            public ParentClass() { }

            public int ParentField
            {
                get { return field; }
            }
        }

        [SerializableObject]
        private class ChildClass : ParentClass
        {
            private int field;

            public ChildClass(int field, int parentField) : base(parentField)
            {
                this.field = field;
            }

            public ChildClass() { }

            public int Field
            {
                get { return field; }
            }
        }

        [SerializableObject]
        private class ObjectWithIgnoredField
        {
            public int Field1 = 0;

            [IgnoreField]
            public int Field2 = 0;
        }

        [SerializableObject]
        private class RegisteredClass
        {
        }

        [SerializableObject]
        private class ObjectWithObjectReferences
        {
            public RegisteredClass Field1;
            public RegisteredClass Field2;
        }

        [SerializableObject]
        private class ClassWithNoReferenceFields
        {
            [ValueOnly]
            public RegisteredClass Field1;
            [ValueOnly]
            public RegisteredClass Field2;
        }

        private enum SerializableEnum
        {
            Option1,
            Option2,
            Option3
        }

        private class UnregisteredClass
        {
        }

        [SerializableObject]
        private class ClassWithSerializationHooks : ISerializerListener
        {
            [IgnoreField]
            public int RuntimeField;

            public int SerializedField;

            public void OnBeforeSerialize()
            {
                SerializedField = RuntimeField * 2;
            }

            public void OnAfterDeserialize()
            {
                RuntimeField = SerializedField / 2;
            }
        }

        [Test]
        public void SerializeFlatObject()
        {
            Serializer serializer = new Serializer();

            FlatClass obj = new FlatClass() { Field1 = 123, Field2 = "asdf", Field3 = 6.78f };

            string str = serializer.Serialize(obj);
            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(str);

            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual("asdf", deserializedObj.Field2);
            Assert.AreEqual(6.78f, deserializedObj.Field3);
        }

        [Test]
        public void SerializeHierarchicalObject()
        {
            Serializer serializer = new Serializer();

            HierarchicalObject nestedObj = new HierarchicalObject() { Field1 = 123, Nested = null };
            HierarchicalObject rootObj = new HierarchicalObject() { Field1 = 456, Nested = nestedObj };

            string str = serializer.Serialize(rootObj);
            HierarchicalObject deserializedObj = serializer.Deserialize<HierarchicalObject>(str);

            Assert.AreEqual(456, deserializedObj.Field1);
            Assert.NotNull(deserializedObj.Nested);

            HierarchicalObject deserializedNestedObj = deserializedObj.Nested;

            Assert.AreEqual(123, deserializedNestedObj.Field1);
            Assert.Null(deserializedNestedObj.Nested);
        }

        [Test]
        public void SerializeDerivedObjectWithSameFieldNameAsParent()
        {
            Serializer serializer = new Serializer();

            ChildClass obj = new ChildClass(123, 456);

            string str = serializer.Serialize(obj);
            ChildClass deserializedObj = serializer.Deserialize<ChildClass>(str);

            Assert.AreEqual(123, deserializedObj.Field);
            Assert.AreEqual(456, deserializedObj.ParentField);
        }

        [Test]
        public void SerializeObjectWithIgnoredField()
        {
            Serializer serializer = new Serializer();

            ObjectWithIgnoredField obj = new ObjectWithIgnoredField() { Field1 = 123, Field2 = 456 };

            string str = serializer.Serialize(obj);

            Assert.IsFalse(str.Contains("Field2"));

            ObjectWithIgnoredField deserializedObj = serializer.Deserialize<ObjectWithIgnoredField>(str);

            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(0, deserializedObj.Field2);
        }

        [Test]
        public void SerializeObjectReferences()
        {
            Serializer serializer = new Serializer();

            RegisteredClass nestedObj = new RegisteredClass();
            ObjectWithObjectReferences obj = new ObjectWithObjectReferences() { Field1 = nestedObj, Field2 = nestedObj };

            string str = serializer.Serialize(obj);
            ObjectWithObjectReferences deserializedObj = serializer.Deserialize<ObjectWithObjectReferences>(str);

            Assert.NotNull(deserializedObj.Field1);
            Assert.NotNull(deserializedObj.Field2);
            Assert.AreEqual(deserializedObj.Field1, deserializedObj.Field2);            
        }

        [Test]
        public void SerializeArray()
        {
            Serializer serializer = new Serializer();
            serializer.RegisterType(typeof(int[]));

            int[] array = new int[] { 1, 2, 3 };

            string str = serializer.Serialize(array);
            int[] deserializedArray = serializer.Deserialize<int[]>(str);

            Assert.NotNull(deserializedArray);
            Assert.AreEqual(3, deserializedArray.Length);
            Assert.AreEqual(1, deserializedArray[0]);
            Assert.AreEqual(2, deserializedArray[1]);
            Assert.AreEqual(3, deserializedArray[2]);
        }

        [Test]
        public void SerializeList()
        {
            Serializer serializer = new Serializer();
            serializer.RegisterType(typeof(List<string>));

            List<string> list = new List<string>() { "qwer", "asdf", "zxcv" };

            string str = serializer.Serialize(list);
            List<string> deserializedList = serializer.Deserialize<List<string>>(str);

            Assert.NotNull(deserializedList);
            Assert.AreEqual(3, deserializedList.Count);
            Assert.AreEqual("qwer", deserializedList[0]);
            Assert.AreEqual("asdf", deserializedList[1]);
            Assert.AreEqual("zxcv", deserializedList[2]);
        }

        [Test]
        public void SerializeHashSet()
        {
            Serializer serializer = new Serializer();
            serializer.RegisterType(typeof(HashSet<string>));

            HashSet<string> set = new HashSet<string>() { "qwer", "asdf", "zxcv" };

            string str = serializer.Serialize(set);
            HashSet<string> deserializedSet = serializer.Deserialize<HashSet<string>>(str);

            Assert.NotNull(deserializedSet);
            Assert.AreEqual(3, deserializedSet.Count);
            Assert.True(deserializedSet.Contains("qwer"));
            Assert.True(deserializedSet.Contains("asdf"));
            Assert.True(deserializedSet.Contains("zxcv"));
        }

        [Test]
        public void SerializeDictionary()
        {
            Serializer serializer = new Serializer();
            serializer.RegisterType(typeof(Dictionary<int, string>));

            Dictionary<int, string> dictionary = new Dictionary<int, string>() { { 1, "asdf" }, { 2, "zxcv" } };

            string str = serializer.Serialize(dictionary);
            Dictionary<int, string> deserializedDictionary = serializer.Deserialize<Dictionary<int, string>>(str);

            Assert.NotNull(deserializedDictionary);
            Assert.AreEqual(2, deserializedDictionary.Count);
            Assert.True(deserializedDictionary.ContainsKey(1));
            Assert.True(deserializedDictionary.ContainsKey(2));
            Assert.AreEqual("asdf", deserializedDictionary[1]);
            Assert.AreEqual("zxcv", deserializedDictionary[2]);
        }

        [Test]
        public void SerializeEnum()
        {
            Serializer serializer = new Serializer();

            string str = serializer.Serialize(SerializableEnum.Option2);
            SerializableEnum deserializedEnum = serializer.Deserialize<SerializableEnum>(str);

            Assert.AreEqual(SerializableEnum.Option2, deserializedEnum);
        }

        [Test]
        public void SerializeStruct()
        {
            Serializer serializer = new Serializer();

            SerializableStruct s = new SerializableStruct() { Field1 = 123, Field2 = 456 };

            string str = serializer.Serialize(s);
            SerializableStruct deserializedStruct = serializer.Deserialize<SerializableStruct>(str);

            Assert.AreEqual(123, deserializedStruct.Field1);
            Assert.AreEqual(456, deserializedStruct.Field2);
        }

        [Test]
        public void SerializeStringWithSpecialCharacter()
        {
            Serializer serializer = new Serializer();

            string str = serializer.Serialize("\"");
            string deserializedStr = serializer.Deserialize<string>(str);

            Assert.AreEqual("\"", deserializedStr);
        }

        [Test]
        public void SerializeUnregisteredClass()
        {
            Serializer serializer = new Serializer();
            UnregisteredClass obj = new UnregisteredClass();

            Assert.Throws<IcepackException>(() => {
                string str = serializer.Serialize(obj);
            });
        }

        [Test]
        public void DeserializeUnregisteredClass()
        {
            Serializer serializer = new Serializer();

            string typeName = Toolbox.EscapeString(typeof(UnregisteredClass).AssemblyQualifiedName);

            Assert.Throws<IcepackException>(() => {
                serializer.Deserialize<UnregisteredClass>($"[[[0,[0]]],[[{typeName}]]]");
            },
            $"Type {typeName} is not registered for serialization!");
        }

        [Test]
        public void DeserializeRegisteredClassWithoutPriorSerialization()
        {
            Serializer serializer = new Serializer();

            string typeName = Toolbox.EscapeString(typeof(RegisteredClass).AssemblyQualifiedName);

            RegisteredClass deserializedObj = serializer.Deserialize<RegisteredClass>($"[[[0,[0]]],[[{typeName}]]]");

            Assert.NotNull(deserializedObj);
        }

        [Test]
        public void SerializeClassWithNoReferenceAttribute()
        {
            Serializer serializer = new Serializer();

            RegisteredClass nestedObj = new RegisteredClass();
            ClassWithNoReferenceFields obj = new ClassWithNoReferenceFields() { Field1 = nestedObj, Field2 = nestedObj };

            string str = serializer.Serialize(obj);
            ClassWithNoReferenceFields deserializedObj = serializer.Deserialize<ClassWithNoReferenceFields>(str);

            Assert.NotNull(deserializedObj.Field1);
            Assert.NotNull(deserializedObj.Field2);
            Assert.AreNotEqual(deserializedObj.Field1, deserializedObj.Field2);
        }

        [Test]
        public void SerializeArrayWithNoReferenceAttribute()
        {
            Serializer serializer = new Serializer();
            serializer.RegisterType(typeof(RegisteredClass[]), true);

            RegisteredClass nestedObj = new RegisteredClass();
            RegisteredClass[] obj = new RegisteredClass[] { nestedObj, nestedObj };

            string str = serializer.Serialize(obj);
            RegisteredClass[] deserializedArray = serializer.Deserialize<RegisteredClass[]>(str);

            Assert.NotNull(deserializedArray[0]);
            Assert.NotNull(deserializedArray[1]);
            Assert.AreNotEqual(deserializedArray[0], deserializedArray[1]);
        }

        [Test]
        public void SerializeClassWithSerializationHooks()
        {
            Serializer serializer = new Serializer();

            ClassWithSerializationHooks obj = new ClassWithSerializationHooks() { RuntimeField = 123 };

            string str = serializer.Serialize(obj);

            Assert.True(str.Contains("246"));
            Assert.False(str.Contains("123"));

            ClassWithSerializationHooks deserializedObj = serializer.Deserialize<ClassWithSerializationHooks>(str);

            Assert.AreEqual(123, deserializedObj.RuntimeField);
        }
    }
}