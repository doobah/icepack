using NUnit.Framework;
using Icepack;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Text;

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
#pragma warning disable IDE0044 // Add readonly modifier
            private int field;
#pragma warning restore IDE0044 // Add readonly modifier

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
#pragma warning disable IDE0044 // Add readonly modifier
            private int field;
#pragma warning restore IDE0044 // Add readonly modifier

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
        private struct StructWithObjectReferences
        {
            public FlatClass Field1;
            public int Field2;
        }

        [SerializableObject]
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
        private class ClassWithIntField
        {
            public int Field1;
        }

        [SerializableObject]
        private class ClassWithMultipleObjectFields
        {
            public object Field1;
            public object Field2;
            public object Field3;
        }

        [SerializableObject]
        private class ClassThatImplementsInterface : IInterface
        {
            private int field;

            public int Value
            {
                get { return field; }
                set { field = value; }

            }
        }

        [SerializableObject]
        private class ClassWithInterfaceField
        {
            public int Field1;
            public IInterface Field2;
            public int Field3;
        }

        private interface IInterface
        {
            int Value { get; set; }
        }

        [SerializableObject]
        private struct StructThatImplementsInterface : IInterface
        {
            private int field;

            public int Value
            {
                get { return field; }
                set { field = value; }
            }
        }

        [SerializableObject]
        private class ClassWithSerializationHooks : ISerializerListener
        {
            public int Field;

            public void OnBeforeSerialize()
            {
                Field *= 2;
            }

            public void OnAfterDeserialize()
            {
                Field++;
            }
        }

        [SerializableObject]
        private class ClassWithStructWithSerializationHooksField
        {
            public int Field1;
            public StructWithSerializationHooks Field2;
            public int Field3;
        }

        [SerializableObject]
        private struct StructWithSerializationHooks : ISerializerListener
        {
            public int Field;

            public void OnBeforeSerialize()
            {
                Field *= 2;
            }

            public void OnAfterDeserialize()
            {
                Field++;
            }
        }

        private struct StructWithNestedStruct
        {
            public NestedStruct Field;
        }

        private struct NestedStruct
        {
            public int Field;
        }

        [SerializableObject]
        private class BaseClass
        {
            public int FieldBase;
        }

        [SerializableObject]
        private class FormerBaseClass : BaseClass
        {
            public int FieldFormerBase1;
            public int FieldFormerBase2;
        }

        [SerializableObject]
        private class DerivedClass : BaseClass
        {
            public int FieldDerived;
        }

        [SerializableObject]
        private class ClassWithEnumField
        {
            public int Field1;
            public SerializableEnum Field2;
            public int Field3;
        }

        [SerializableObject]
        private class ClassWithObjectField
        {
            public int Field1;
            public object Field2;
            public int Field3;
        }

        [SerializableObject]
        private class ClassWithTypeField
        {
            public int Field1;
            public Type Field2;
            public int Field3;
        }

        [SerializableObject]
        private class ClassWithRenamedField
        {
            public int Field1;
            [PreviousName("Field9000")]
            public int Field2;
            public int Field3;
        }

        [SerializableObject]
        private class ClassWithReadonlyField
        {
            public readonly int Field;

            public ClassWithReadonlyField()
            {
                Field = 123;
            }
        }

        [SerializableObject]
        private class ClassWithPrivateConstructor
        {
            public readonly int Field;

            private ClassWithPrivateConstructor()
            {
                Field = 123;
            }

            public static ClassWithPrivateConstructor Create()
            {
                return new ClassWithPrivateConstructor();
            }
        }

        [SerializableObject]
        private class ClassWithNoDefaultConstructor
        {
            public readonly int Field;

            public ClassWithNoDefaultConstructor(int field)
            {
                Field = field;
            }
        }

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
            serializer.RegisterType(typeof(IInterface[]));

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
        public void CompatibilityMatch()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(1);            // Number of types
            writer.Write(typeof(int).AssemblyQualifiedName);
            writer.Write((byte)0);      // Immutable
            writer.Write(1);            // Number of objects
            writer.Write((uint)1);      // Type
            writer.Write(123);          // Root object
            writer.Close();

            stream.Position = 0;
            Assert.DoesNotThrow(() => {
                int output = serializer.Deserialize<int>(stream);
            });

            stream.Close();
        }

        [Test]
        public void CompatibilityVersionMismatch()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write((ushort)0);    // Compatibility version
            writer.Write(0);            // Number of types
            writer.Write(0);            // Number of objects
            writer.Write(true);         // Root object is value-type
            writer.Write(123);          // Root object
            writer.Close();

            stream.Position = 0;
            Assert.Throws<IcepackException>(() => {
                int output = serializer.Deserialize<int>(stream);
            });

            stream.Close();
        }

        [Test]
        public void DeserializeUnregisteredClass()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(1);            // Number of types
            writer.Write(typeof(UnregisteredClass).AssemblyQualifiedName);     // Type name
            writer.Write((byte)5);      // Category: class
            writer.Write(1);            // Size of type
            writer.Write(false);        // Type has no parent
            writer.Write(0);            // Number of fields
            writer.Write(1);            // Number of objects
            writer.Write((uint)1);      // Type ID
            writer.Write(false);        // Root object is reference-type
            writer.Write((uint)1);      // Type ID
            writer.Close();

            stream.Position = 0;
            Assert.Throws<IcepackException>(() => {
                serializer.Deserialize<UnregisteredClass>(stream);
            });

            stream.Close();
        }

        [Test]
        public void DeserializeRegisteredClassWithoutPriorSerialization()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(1);            // Number of types
            writer.Write(typeof(RegisteredClass).AssemblyQualifiedName);     // Type name
            writer.Write((byte)6);      // Category: class
            writer.Write(1);            // Size of type
            writer.Write((uint)0);      // Type has no parent
            writer.Write(0);            // Number of fields
            writer.Write(1);            // Number of objects
            writer.Write((uint)1);      // Type ID
            writer.Close();

            stream.Position = 0;
            RegisteredClass deserializedObj = serializer.Deserialize<RegisteredClass>(stream);

            Assert.NotNull(deserializedObj);
        }

        [Test]
        public void DeserializeClassWithAdditionalField()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(1);            // Number of types
            writer.Write(typeof(FlatClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(8);            // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(2);            // Number of fields
            writer.Write("Field1");
            writer.Write(4);            // Size of int
            writer.Write("Field3");
            writer.Write(4);            // Size of float
            writer.Write(1);            // Number of objects
            writer.Write((uint)1);      // Type ID of root object
            
            writer.Write(123);          // Field1
            writer.Write(2.34f);        // Field3
            writer.Close();

            stream.Position = 0;
            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream);

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(null, deserializedObj.Field2);
            Assert.AreEqual(2.34f, deserializedObj.Field3);
        }

        [Test]
        public void DeserializeClassWithDeletedReferenceField()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(2);            // Number of types

            writer.Write(typeof(string).AssemblyQualifiedName);
            writer.Write((byte)0);      // Category: string

            writer.Write(typeof(FlatClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(16);           // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(4);            // Number of fields
            writer.Write("Field1");
            writer.Write(4);            // Size of int
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field2andHalf");
            writer.Write(4);            // Size of object reference
            writer.Write("Field3");
            writer.Write(4);            // Size of float

            writer.Write(3);            // Number of objects
            writer.Write((uint)2);      // Type ID of root object
            writer.Write((uint)1);      // Type ID of object 2
            writer.Write("asdf");       // String value
            writer.Write((uint)1);      // Type ID of object 3
            writer.Write("some stuff"); // String value

            writer.Write(123);          // Field1
            writer.Write((uint)2);      // Field2
            writer.Write((uint)3);      // Field2andHalf
            writer.Write(2.34f);        // Field3
            writer.Close();

            stream.Position = 0;
            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream);

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual("asdf", deserializedObj.Field2);
            Assert.AreEqual(2.34f, deserializedObj.Field3);
        }

        [Test]
        public void DeserializeClassWithDeletedStructField()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types

            writer.Write(typeof(string).AssemblyQualifiedName);
            writer.Write((byte)0);      // Category: string
            
            writer.Write(typeof(SerializableStruct).AssemblyQualifiedName);
            writer.Write((byte)5);      // Category: struct
            writer.Write(12);           // Type size (int + int + 4)
            writer.Write(2);            // 2 fields
            writer.Write("Field1");
            writer.Write(4);            // int
            writer.Write("Field2");
            writer.Write(4);            // int
            
            writer.Write(typeof(FlatClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(16);           // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(4);            // Number of fields
            writer.Write("Field1");
            writer.Write(4);            // Size of int
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field2andHalf");
            writer.Write(12);           // Size of struct
            writer.Write("Field3");
            writer.Write(4);            // Size of float
            writer.Write(2);            // Number of objects
            writer.Write((uint)3);      // Type ID of root object
            writer.Write((uint)1);      // Type ID of object 2
            writer.Write("asdf");       // String value

            writer.Write(123);          // Field1
            writer.Write((uint)2);      // Field2
            writer.Write((uint)2);      // Field2andHalf type ID
            writer.Write(222);          // struct Field1
            writer.Write(444);          // struct Field2
            writer.Write(2.34f);        // Field3
            writer.Close();

            stream.Position = 0;
            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream);

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual("asdf", deserializedObj.Field2);
            Assert.AreEqual(2.34f, deserializedObj.Field3);
        }

        [Test]
        public void DeserializeClassWithDeletedClassType()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types
            
            writer.Write("MissingClassName");
            writer.Write((byte)6);      // Category: class
            writer.Write(0);            // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(0);            // Number of fields

            writer.Write(typeof(RegisteredClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(0);            // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(0);            // Number of fields
            
            writer.Write(typeof(ClassWithMultipleObjectFields).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(12);           // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(3);            // Number of fields            
            writer.Write("Field1");
            writer.Write(4);            // Size of object reference
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field3");
            writer.Write(4);            // Size of object reference

            writer.Write(4);            // Number of objects
            writer.Write((uint)3);      // Type ID of root object
            writer.Write((uint)2);      // Type ID of object 2
            writer.Write((uint)1);      // Type ID of object 3
            writer.Write((uint)2);      // Type ID of object 4

            writer.Write((uint)2);      // Field1
            writer.Write((uint)3);      // Field2
            writer.Write((uint)4);      // Field3
            writer.Close();

            stream.Position = 0;
            ClassWithMultipleObjectFields deserializedObj = serializer.Deserialize<ClassWithMultipleObjectFields>(stream);

            Assert.NotNull(deserializedObj);
            Assert.NotNull(deserializedObj.Field1);
            Assert.IsTrue(deserializedObj.Field1.GetType() == typeof(RegisteredClass));
            Assert.Null(deserializedObj.Field2);
            Assert.NotNull(deserializedObj.Field3);
            Assert.IsTrue(deserializedObj.Field3.GetType() == typeof(RegisteredClass));
        }

        [Test]
        public void DeserializeClassWithDeletedArrayType()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types

            writer.Write("MissingClassName[]");
            writer.Write((byte)1);      // Category: array
            writer.Write(4);            // Item size

            writer.Write(typeof(ClassWithIntField).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(4);            // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(1);            // Number of fields
            writer.Write("Field1");     // Field1
            writer.Write(4);            // Field1 size

            writer.Write(typeof(ClassWithMultipleObjectFields).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(12);           // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(3);            // Number of fields            
            writer.Write("Field1");
            writer.Write(4);            // Size of object reference
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field3");
            writer.Write(4);            // Size of object reference

            writer.Write(4);            // Number of objects
            writer.Write((uint)3);      // Type ID of root object
            writer.Write((uint)2);      // Type ID of object 2
            writer.Write((uint)1);      // Type ID of object 3
            writer.Write(3);            // Array length
            writer.Write((uint)2);      // Type ID of object 4

            writer.Write((uint)2);      // Field1
            writer.Write((uint)3);      // Field2
            writer.Write((uint)4);      // Field3

            writer.Write(123);          // Field1

            writer.Write(1);            // Array[0]
            writer.Write(2);            // Array[1]
            writer.Write(3);            // Array[2]

            writer.Write(456);          // Field1
            writer.Close();

            stream.Position = 0;
            ClassWithMultipleObjectFields deserializedObj = serializer.Deserialize<ClassWithMultipleObjectFields>(stream);

            Assert.NotNull(deserializedObj);
            Assert.NotNull(deserializedObj.Field1);
            Assert.IsTrue(deserializedObj.Field1.GetType() == typeof(ClassWithIntField));
            Assert.AreEqual(123, ((ClassWithIntField)deserializedObj.Field1).Field1);
            Assert.Null(deserializedObj.Field2);
            Assert.NotNull(deserializedObj.Field3);
            Assert.IsTrue(deserializedObj.Field3.GetType() == typeof(ClassWithIntField));
            Assert.AreEqual(456, ((ClassWithIntField)deserializedObj.Field3).Field1);
        }

        [Test]
        public void DeserializeClassWithDeletedDictionaryType()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types

            writer.Write("MissingClassName<,>");
            writer.Write((byte)4);      // Category: dictionary
            writer.Write(1);            // Key size
            writer.Write(4);            // Item size

            writer.Write(typeof(RegisteredClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(0);            // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(0);            // Number of fields

            writer.Write(typeof(ClassWithMultipleObjectFields).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(12);           // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(3);            // Number of fields            
            writer.Write("Field1");
            writer.Write(4);            // Size of object reference
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field3");
            writer.Write(4);            // Size of object reference

            writer.Write(4);            // Number of objects
            writer.Write((uint)3);      // Type ID of root object
            writer.Write((uint)2);      // Type ID of object 2
            writer.Write((uint)1);      // Type ID of object 3
            writer.Write(3);            // Dictionary length
            writer.Write((uint)2);      // Type ID of object 4

            writer.Write((uint)2);      // Field1
            writer.Write((uint)3);      // Field2
            writer.Write((uint)4);      // Field3

            writer.Write((byte)1);      // Key 0
            writer.Write(2);            // Value 0
            writer.Write((byte)3);      // Key 1
            writer.Write(4);            // Value 1
            writer.Write((byte)5);      // Key 2
            writer.Write(6);            // Value 2

            writer.Close();

            stream.Position = 0;
            ClassWithMultipleObjectFields deserializedObj = serializer.Deserialize<ClassWithMultipleObjectFields>(stream);

            Assert.NotNull(deserializedObj);
            Assert.NotNull(deserializedObj.Field1);
            Assert.IsTrue(deserializedObj.Field1.GetType() == typeof(RegisteredClass));
            Assert.Null(deserializedObj.Field2);
            Assert.NotNull(deserializedObj.Field3);
            Assert.IsTrue(deserializedObj.Field3.GetType() == typeof(RegisteredClass));
        }

        [Test]
        public void DeserializeClassWithMissingParentType()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types

            writer.Write(typeof(BaseClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(4);            // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(1);            // Number of fields
            writer.Write("FieldBase");
            writer.Write(4);            // Field size

            writer.Write("MissingParentClass");
            writer.Write((byte)6);      // Category: class
            writer.Write(4);            // Type size
            writer.Write((uint)1);      // Has a parent
            writer.Write(1);            // Number of fields
            writer.Write("FieldMissing");
            writer.Write(4);            // Field size

            writer.Write(typeof(DerivedClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(4);            // Type size
            writer.Write((uint)2);      // Has a parent
            writer.Write(1);            // Number of fields
            writer.Write("FieldDerived");
            writer.Write(4);            // Field size

            writer.Write(1);            // Number of objects
            writer.Write((uint)3);      // Type ID of root object

            writer.Write(123);          // Derived class: Field1
            writer.Write(456);          // Parent class: Field1
            writer.Write(789);          // Grandparent class: Field1

            writer.Close();

            stream.Position = 0;
            DerivedClass deserializedObj = serializer.Deserialize<DerivedClass>(stream);

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.FieldDerived);
            Assert.AreEqual(789, deserializedObj.FieldBase);
        }

        [Test]
        public void DeserializeClassNoLongerDerivedFromOtherType()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types

            writer.Write(typeof(BaseClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(4);            // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(1);            // Number of fields
            writer.Write("FieldBase");
            writer.Write(4);            // Field size

            writer.Write(typeof(FormerBaseClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(8);            // Type size
            writer.Write((uint)1);      // Has a parent
            writer.Write(2);            // Number of fields
            writer.Write("FieldFormerBase1");
            writer.Write(4);            // Field size
            writer.Write("FieldFormerBase2");
            writer.Write(4);            // Field size

            writer.Write(typeof(DerivedClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(4);            // Type size
            writer.Write((uint)2);      // Has a parent
            writer.Write(1);            // Number of fields
            writer.Write("FieldDerived");
            writer.Write(4);            // Field size

            writer.Write(1);            // Number of objects
            writer.Write((uint)3);      // Type ID of root object

            writer.Write(123);          // Derived class: Field1
            writer.Write(456);          // Parent class: Field1
            writer.Write(654);          // Parent class: Field2
            writer.Write(789);          // Grandparent class: Field1

            writer.Close();

            stream.Position = 0;
            DerivedClass deserializedObj = serializer.Deserialize<DerivedClass>(stream);

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.FieldDerived);
            Assert.AreEqual(789, deserializedObj.FieldBase);
        }

        [Test]
        public void DeserializeClassWithDeletedEnumType()
        {
            var serializer = new Serializer();
            
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types

            writer.Write(typeof(ClassWithObjectField).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(12);           // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(3);            // Number of fields            
            writer.Write("Field1");
            writer.Write(4);            // Size of int
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field3");
            writer.Write(4);            // Size of int

            writer.Write(typeof(short).AssemblyQualifiedName);
            writer.Write((byte)0);      // Category: immutable

            writer.Write("MissingEnumName");
            writer.Write((byte)7);      // Category: enum
            writer.Write((uint)2);      // Underlying type ID

            writer.Write(2);            // Number of objects
            writer.Write((uint)1);      // Type ID of root object
            writer.Write((uint)3);      // Type ID: enum
            writer.Write((short)456);   // Enum value

            writer.Write(123);          // Field1
            writer.Write((uint)2);      // Field2
            writer.Write(789);          // Field3

            writer.Close();

            stream.Position = 0;
            ClassWithObjectField deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.Null(deserializedObj.Field2);
            Assert.AreEqual(789, deserializedObj.Field3);
        }

        [Test]
        public void DeserializeClassWithDeletedType()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types

            writer.Write(typeof(ClassWithTypeField).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(12);           // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(3);            // Number of fields            
            writer.Write("Field1");
            writer.Write(4);            // Size of int
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field3");
            writer.Write(4);            // Size of int

            writer.Write(typeof(Type).AssemblyQualifiedName);
            writer.Write((byte)8);      // Category: type

            writer.Write("MissingTypeName");
            writer.Write((byte)8);      // Category: type

            writer.Write(2);            // Number of objects
            writer.Write((uint)1);      // Type ID of root object
            writer.Write((uint)2);      // Type ID of Type object
            writer.Write((uint)3);      // Value of Type object

            writer.Write(123);          // Field1
            writer.Write((uint)2);      // Field2
            writer.Write(789);          // Field3

            writer.Close();

            stream.Position = 0;
            ClassWithTypeField deserializedObj = serializer.Deserialize<ClassWithTypeField>(stream);

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.Null(deserializedObj.Field2);
            Assert.AreEqual(789, deserializedObj.Field3);
        }

        [Test]
        public void DeserializeClassWithRenamedField()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(1);            // Number of types

            writer.Write(typeof(ClassWithRenamedField).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(12);           // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(3);            // Number of fields            
            writer.Write("Field1");
            writer.Write(4);            // Size of int
            writer.Write("Field9000");
            writer.Write(4);            // Size of int
            writer.Write("Field3");
            writer.Write(4);            // Size of int

            writer.Write(1);            // Number of objects
            writer.Write((uint)1);      // Type ID of root object

            writer.Write(123);          // Field1
            writer.Write(456);          // Field9000
            writer.Write(789);          // Field3

            writer.Close();

            stream.Position = 0;
            ClassWithRenamedField deserializedObj = serializer.Deserialize<ClassWithRenamedField>(stream);

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
            serializer.RegisterType(typeof(IInterface[]));

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
            serializer.RegisterType(typeof(IInterface[]));

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
            var serializer = new Serializer(new SerializerSettings() { PreserveReferences = false });

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
            var serializer = new Serializer(new SerializerSettings() { PreserveReferences = false });

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
    }
}