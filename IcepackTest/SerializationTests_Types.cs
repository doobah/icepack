using Icepack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest
{
    public partial class SerializationTests
    {
        [SerializableType]
        private class FlatClass
        {
            public int Field1;
            public string Field2;
            public float Field3;
        }

        [SerializableType]
        private struct SerializableStruct
        {
            public int Field1;
            public int Field2;
        }

        [SerializableType]
        private class HierarchicalObject
        {
            public int Field1;
            public HierarchicalObject Nested;
        }

        [SerializableType]
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

        [SerializableType]
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

        [SerializableType]
        private class ObjectWithIgnoredField
        {
            public int Field1 = 0;

            [IgnoreField]
            public int Field2 = 0;
        }

        [SerializableType]
        private class RegisteredClass
        {
        }

        [SerializableType]
        private class ObjectWithObjectReferences
        {
            public RegisteredClass Field1;
            public RegisteredClass Field2;
        }

        [SerializableType]
        private struct StructWithObjectReferences
        {
            public FlatClass Field1;
            public int Field2;
        }

        [SerializableType]
        private enum SerializableEnum
        {
            Option1,
            Option2,
            Option3
        }

        private class UnregisteredClass
        {
        }

        [SerializableType]
        private class ClassWithIntField
        {
            public int Field1;
        }

        [SerializableType]
        private class ClassWithMultipleObjectFields
        {
            public object Field1;
            public object Field2;
            public object Field3;
        }

        [SerializableType]
        private class ClassThatImplementsInterface : IInterface
        {
            private int field;

            public int Value
            {
                get { return field; }
                set { field = value; }

            }
        }

        [SerializableType]
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

        [SerializableType]
        private struct StructThatImplementsInterface : IInterface
        {
            private int field;

            public int Value
            {
                get { return field; }
                set { field = value; }
            }
        }

        [SerializableType]
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

        [SerializableType]
        private class ClassWithStructWithSerializationHooksField
        {
            public int Field1;
            public StructWithSerializationHooks Field2;
            public int Field3;
        }

        [SerializableType]
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

        [SerializableType]
        private class BaseClass
        {
            public int FieldBase;
        }

        [SerializableType]
        private class FormerBaseClass : BaseClass
        {
            public int FieldFormerBase1;
            public int FieldFormerBase2;
        }

        [SerializableType]
        private class DerivedClass : BaseClass
        {
            public int FieldDerived;
        }

        [SerializableType]
        private class ClassWithEnumField
        {
            public int Field1;
            public SerializableEnum Field2;
            public int Field3;
        }

        [SerializableType]
        private class ClassWithObjectField
        {
            public int Field1;
            public object Field2;
            public int Field3;
        }

        [SerializableType]
        private class ClassWithTypeField
        {
            public int Field1;
            public Type Field2;
            public int Field3;
        }

        [SerializableType]
        private class ClassWithRenamedField
        {
            public int Field1;
            [PreviousName("Field9000")]
            public int Field2;
            public int Field3;
        }

        [SerializableType]
        private class ClassWithReadonlyField
        {
            public readonly int Field;

            public ClassWithReadonlyField()
            {
                Field = 123;
            }
        }

        [SerializableType]
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

        [SerializableType]
        private class ClassWithNoDefaultConstructor
        {
            public readonly int Field;

            public ClassWithNoDefaultConstructor(int field)
            {
                Field = field;
            }
        }

        [SerializableType]
        private class ClassWithSerializedField
        {
            [SerializableField]
            public int Field1 = 0;

            public int Field2 = 0;
        }

        [SerializableType]
        private class GenericClass<T>
        {
            public T Field = default;
        }

        public delegate int Del(int arg);

        [SerializableType]
        private class ClassWithIntPtrField
        {
            public int Field1;
            public nint Field2;
            public int Field3;
        }

        [SerializableType]
        private class ClassWithDelegateField
        {
            public int Field1;
            public Del Field2;
            public int Field3;
        }

        private struct UnregisteredStruct
        {
        }

        [SerializableType]
        private class ClassWithUnregisteredStructFieldType
        {
            public int Field1;
            public UnregisteredStruct Field2;
            public int Field3;
        }

        [SerializableType]
        private struct RegisteredStruct
        {
        }

        [SerializableType]
        private class ClassWithRegisteredStructFieldType
        {
            public int Field1;
            public RegisteredStruct Field2;
            public int Field3;
        }
    }
}
