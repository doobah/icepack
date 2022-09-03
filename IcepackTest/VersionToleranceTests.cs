using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Icepack;
using System.IO;

namespace IcepackTest
{
    public class VersionToleranceTests
    {
        [Test]
        public void CompatibilityMatch()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(1);            // Number of types
            writer.Write(typeof(int).AssemblyQualifiedName!);
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
            writer.Write(typeof(UnregisteredClass).AssemblyQualifiedName!);     // Type name
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
            writer.Write(typeof(RegisteredClass).AssemblyQualifiedName!);     // Type name
            writer.Write((byte)6);      // Category: class
            writer.Write(1);            // Size of type
            writer.Write((uint)0);      // Type has no parent
            writer.Write(0);            // Number of fields
            writer.Write(1);            // Number of objects
            writer.Write((uint)1);      // Type ID
            writer.Close();

            stream.Position = 0;
            RegisteredClass deserializedObj = serializer.Deserialize<RegisteredClass>(stream)!;

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
            writer.Write(typeof(FlatClass).AssemblyQualifiedName!);
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
            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream)!;

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

            writer.Write(typeof(string).AssemblyQualifiedName!);
            writer.Write((byte)0);      // Category: string

            writer.Write(typeof(FlatClass).AssemblyQualifiedName!);
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
            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream)!;

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

            writer.Write(typeof(string).AssemblyQualifiedName!);
            writer.Write((byte)0);      // Category: string

            writer.Write(typeof(SerializableStruct).AssemblyQualifiedName!);
            writer.Write((byte)5);      // Category: struct
            writer.Write(12);           // Type size (int + int + 4)
            writer.Write(2);            // 2 fields
            writer.Write("Field1");
            writer.Write(4);            // int
            writer.Write("Field2");
            writer.Write(4);            // int

            writer.Write(typeof(FlatClass).AssemblyQualifiedName!);
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
            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream)!;

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

            writer.Write(typeof(RegisteredClass).AssemblyQualifiedName!);
            writer.Write((byte)6);      // Category: class
            writer.Write(0);            // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(0);            // Number of fields

            writer.Write(typeof(ClassWithMultipleObjectFields).AssemblyQualifiedName!);
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
            ClassWithMultipleObjectFields deserializedObj = serializer.Deserialize<ClassWithMultipleObjectFields>(stream)!;

            Assert.NotNull(deserializedObj);
            Assert.NotNull(deserializedObj.Field1);
            Assert.IsTrue(deserializedObj.Field1!.GetType() == typeof(RegisteredClass));
            Assert.Null(deserializedObj.Field2);
            Assert.NotNull(deserializedObj.Field3);
            Assert.IsTrue(deserializedObj.Field3!.GetType() == typeof(RegisteredClass));
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

            writer.Write(typeof(ClassWithIntField).AssemblyQualifiedName!);
            writer.Write((byte)6);      // Category: class
            writer.Write(4);            // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(1);            // Number of fields
            writer.Write("Field1");     // Field1
            writer.Write(4);            // Field1 size

            writer.Write(typeof(ClassWithMultipleObjectFields).AssemblyQualifiedName!);
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
            ClassWithMultipleObjectFields deserializedObj = serializer.Deserialize<ClassWithMultipleObjectFields>(stream)!;

            Assert.NotNull(deserializedObj);
            Assert.NotNull(deserializedObj.Field1);
            Assert.IsTrue(deserializedObj.Field1!.GetType() == typeof(ClassWithIntField));
            Assert.AreEqual(123, ((ClassWithIntField)deserializedObj.Field1).Field1);
            Assert.Null(deserializedObj.Field2);
            Assert.NotNull(deserializedObj.Field3);
            Assert.IsTrue(deserializedObj.Field3!.GetType() == typeof(ClassWithIntField));
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

            writer.Write(typeof(RegisteredClass).AssemblyQualifiedName!);
            writer.Write((byte)6);      // Category: class
            writer.Write(0);            // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(0);            // Number of fields

            writer.Write(typeof(ClassWithMultipleObjectFields).AssemblyQualifiedName!);
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
            ClassWithMultipleObjectFields deserializedObj = serializer.Deserialize<ClassWithMultipleObjectFields>(stream)!;

            Assert.NotNull(deserializedObj);
            Assert.NotNull(deserializedObj.Field1);
            Assert.IsTrue(deserializedObj.Field1!.GetType() == typeof(RegisteredClass));
            Assert.Null(deserializedObj.Field2);
            Assert.NotNull(deserializedObj.Field3);
            Assert.IsTrue(deserializedObj.Field3!.GetType() == typeof(RegisteredClass));
        }

        [Test]
        public void DeserializeClassWithMissingParentType()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types

            writer.Write(typeof(BaseClass).AssemblyQualifiedName!);
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

            writer.Write(typeof(DerivedClass).AssemblyQualifiedName!);
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
            DerivedClass deserializedObj = serializer.Deserialize<DerivedClass>(stream)!;

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

            writer.Write(typeof(BaseClass).AssemblyQualifiedName!);
            writer.Write((byte)6);      // Category: class
            writer.Write(4);            // Type size
            writer.Write((uint)0);      // Has no parent
            writer.Write(1);            // Number of fields
            writer.Write("FieldBase");
            writer.Write(4);            // Field size

            writer.Write(typeof(FormerBaseClass).AssemblyQualifiedName!);
            writer.Write((byte)6);      // Category: class
            writer.Write(8);            // Type size
            writer.Write((uint)1);      // Has a parent
            writer.Write(2);            // Number of fields
            writer.Write("FieldFormerBase1");
            writer.Write(4);            // Field size
            writer.Write("FieldFormerBase2");
            writer.Write(4);            // Field size

            writer.Write(typeof(DerivedClass).AssemblyQualifiedName!);
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
            DerivedClass deserializedObj = serializer.Deserialize<DerivedClass>(stream)!;

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

            // Deserialize type metadata

            writer.Write(3);            // Number of types

            writer.Write(typeof(ClassWithObjectField).AssemblyQualifiedName!);
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

            writer.Write(typeof(short).AssemblyQualifiedName!);
            writer.Write((byte)0);      // Category: immutable

            writer.Write("MissingEnumName");
            writer.Write((byte)7);      // Category: enum
            writer.Write((uint)2);      // Underlying type ID

            // Deserialize object metadata

            writer.Write(2);            // Number of objects
            writer.Write((uint)1);      // Type ID of root object
            writer.Write((uint)3);      // Type ID: enum
            writer.Write((short)456);   // Enum value

            // Deserialize object data

            writer.Write(123);          // Field1
            writer.Write((uint)2);      // Field2
            writer.Write(789);          // Field3

            writer.Close();

            stream.Position = 0;
            ClassWithObjectField deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream)!;

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

            writer.Write(typeof(ClassWithTypeField).AssemblyQualifiedName!);
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

            writer.Write(typeof(Type).AssemblyQualifiedName!);
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
            ClassWithTypeField deserializedObj = serializer.Deserialize<ClassWithTypeField>(stream)!;

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

            writer.Write(typeof(ClassWithRenamedField).AssemblyQualifiedName!);
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
            ClassWithRenamedField deserializedObj = serializer.Deserialize<ClassWithRenamedField>(stream)!;

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(456, deserializedObj.Field2);
            Assert.AreEqual(789, deserializedObj.Field3);
        }
    }
}
