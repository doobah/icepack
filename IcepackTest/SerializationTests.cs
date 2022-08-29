using NUnit.Framework;
using Icepack;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

namespace IcepackTest
{
    public partial class SerializationTests
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
        public void SerializeArray()
        {
            var serializer = new Serializer();

            int[] array = new int[] { 1, 2, 3 };

            var stream = new MemoryStream();
            serializer.Serialize(array, stream);
            stream.Position = 0;
            int[] deserializedArray = serializer.Deserialize<int[]>(stream);
            stream.Close();

            Assert.NotNull(deserializedArray);
            Assert.AreEqual(3, deserializedArray.Length);
            Assert.AreEqual(1, deserializedArray[0]);
            Assert.AreEqual(2, deserializedArray[1]);
            Assert.AreEqual(3, deserializedArray[2]);
        }

        [Test]
        public void SerializeList()
        {
            var serializer = new Serializer();

            var list = new List<string>() { "qwer", "asdf", "zxcv" };

            var stream = new MemoryStream();
            serializer.Serialize(list, stream);
            stream.Position = 0;
            List<string> deserializedList = serializer.Deserialize<List<string>>(stream);
            stream.Close();

            Assert.NotNull(deserializedList);
            Assert.AreEqual(3, deserializedList.Count);
            Assert.AreEqual("qwer", deserializedList[0]);
            Assert.AreEqual("asdf", deserializedList[1]);
            Assert.AreEqual("zxcv", deserializedList[2]);
        }

        [Test]
        public void SerializeHashSet()
        {
            var serializer = new Serializer();

            var set = new HashSet<string>() { "qwer", "asdf", "zxcv" };

            var stream = new MemoryStream();
            serializer.Serialize(set, stream);
            stream.Position = 0;
            HashSet<string> deserializedSet = serializer.Deserialize<HashSet<string>>(stream);
            stream.Close();

            Assert.NotNull(deserializedSet);
            Assert.AreEqual(3, deserializedSet.Count);
            Assert.True(deserializedSet.Contains("qwer"));
            Assert.True(deserializedSet.Contains("asdf"));
            Assert.True(deserializedSet.Contains("zxcv"));
        }

        [Test]
        public void SerializeDictionary()
        {
            var serializer = new Serializer();

            var dictionary = new Dictionary<int, string>() { { 1, "asdf" }, { 2, "zxcv" } };

            var stream = new MemoryStream();
            serializer.Serialize(dictionary, stream);
            stream.Position = 0;
            Dictionary<int, string> deserializedDictionary = serializer.Deserialize<Dictionary<int, string>>(stream);
            stream.Close();

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
        public void SerializeUnregisteredType()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();

            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(typeof(UnregisteredClass), stream);
            });

            stream.Close();
        }

        [Test]
        public void SerializeUnregisteredTypeInTypeField()
        {
            var serializer = new Serializer();

            var obj = new ClassWithTypeField() { Field1 = 123, Field2 = typeof(UnregisteredClass), Field3 = 789 };

            var stream = new MemoryStream();

            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });

            stream.Close();
        }

        [Test]
        public void SerializeUnregisteredTypeInObjectField()
        {
            var serializer = new Serializer();

            var obj = new ClassWithObjectField() { Field1 = 123, Field2 = typeof(UnregisteredClass), Field3 = 789 };

            var stream = new MemoryStream();

            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });

            stream.Close();
        }

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

        [Test]
        public void SerializeUnregisteredClass()
        {
            var serializer = new Serializer();
            var obj = new UnregisteredClass();

            var stream = new MemoryStream();
            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });
            stream.Close();
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
            var obj = new ClassWithInterfaceField() {
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

        [Test]
        public void RegisterDependantStructBeforeDependency()
        {
            var serializer = new Serializer();

            Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(StructWithNestedStruct));
            });
        }

        [Test]
        public void RegisterDependencyStructBeforeDependant()
        {
            var serializer = new Serializer();

            serializer.RegisterType(typeof(NestedStruct));
            serializer.RegisterType(typeof(StructWithNestedStruct));
        }

        [Test]
        public void SerializeWithoutPreservingReferences()
        {
            var serializer = new Serializer(new SerializerSettings(preserveReferences: false));

            var nestedObj = new RegisteredClass();
            var obj = new ObjectWithObjectReferences() { Field1 = nestedObj, Field2 = nestedObj };

            var stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            stream.Position = 0;
            ObjectWithObjectReferences deserializedObj = serializer.Deserialize<ObjectWithObjectReferences>(stream);
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
        public void RegisterUnsupportedTypes()
        {
            var serializer = new Serializer();

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(IntPtr));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(UIntPtr));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(Delegate));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(Del));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(int *));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(Span<float>));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }

            {
                IcepackException exception = Assert.Throws<IcepackException>(() => {
                    serializer.RegisterType(typeof(List<>));
                });
                Assert.True(exception.Message.StartsWith("Unsupported type"));
            }
        }

        [Test]
        public void SerializeUnsupportedTypeInObjectField()
        {
            var serializer = new Serializer();

            {
                ClassWithObjectField objWithNintField = new ClassWithObjectField();
                objWithNintField.Field2 = (nint)2;

                var stream = new MemoryStream();
                Assert.Throws<IcepackException>(() => {
                    serializer.Serialize(objWithNintField, stream);
                });
                stream.Close();
            }

            {
                ClassWithObjectField objWithDelegateField = new ClassWithObjectField();
                objWithDelegateField.Field2 = delegate (int a) { return a; };

                var stream = new MemoryStream();
                Assert.Throws<IcepackException>(() => {
                    serializer.Serialize(objWithDelegateField, stream);
                });
                stream.Close();
            }
        }

        [Test]
        public void SerializeClassWithUnsupportedFieldType()
        {
            var serializer = new Serializer();

            {
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

            {
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

        [Test]
        public void RegisterGenericClassWithUnregisteredParameterType()
        {
            var serializer = new Serializer();
            Assert.DoesNotThrow(() => {
                serializer.RegisterType(typeof(GenericClass<UnregisteredClass>));
            });
        }

        [Test]
        public void RegisterListWithUnregisteredParameterType()
        {
            var serializer = new Serializer();
            Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(List<UnregisteredClass>));
            });
        }

        [Test]
        public void RegisterClassWithUnregisteredStructFieldType()
        {
            var serializer = new Serializer();
            Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(ClassWithUnregisteredStructFieldType));
            });
        }

        [Test]
        public void LazyRegisterAnnotatedListParameterType()
        {
            var serializer = new Serializer();

            List<RegisteredClass> obj = new List<RegisteredClass>();

            var stream = new MemoryStream();
            Assert.DoesNotThrow(() => {
                serializer.Serialize(obj, stream);
            });
            stream.Close();
        }

        [Test]
        public void LazyRegisterAnnotatedStructFieldType()
        {
            var serializer = new Serializer();

            ClassWithRegisteredStructFieldType obj = new ClassWithRegisteredStructFieldType();

            var stream = new MemoryStream();
            Assert.DoesNotThrow(() => {
                serializer.Serialize(obj, stream);
            });
            stream.Close();
        }

        [Test]
        public void LazyRegisterNonAnnotatedListParameterType()
        {
            var serializer = new Serializer();

            List<UnregisteredClass> obj = new List<UnregisteredClass>();

            var stream = new MemoryStream();
            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });
            stream.Close();
        }

        [Test]
        public void LazyRegisterNonAnnotatedStructFieldType()
        {
            var serializer = new Serializer();

            ClassWithUnregisteredStructFieldType obj = new ClassWithUnregisteredStructFieldType();

            var stream = new MemoryStream();
            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });
            stream.Close();
        }
    }
}
