using Icepack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest;

#pragma warning disable 0649 // "never assigned to"

[SerializableType]
internal class FlatClass
{
    public int Field1;
    public string? Field2;
    public float Field3;
}

[SerializableType]
internal struct SerializableStruct
{
    public int Field1;
    public int Field2;
}

[SerializableType]
internal class SerializableClass
{
    public int Field1;
    public int Field2;
}

[SerializableType]
internal class HierarchicalObject
{
    public int Field1;
    public HierarchicalObject? Nested;
}

[SerializableType]
internal class ParentClass
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
internal class ChildClass : ParentClass
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
internal class ObjectWithIgnoredField
{
    public int Field1 = 0;

    [IgnoreField]
    public int Field2 = 0;
}

[SerializableType]
internal class RegisteredClass
{
}

[SerializableType]
internal class ObjectWithObjectReferences
{
    public RegisteredClass? Field1;
    public RegisteredClass? Field2;
}

[SerializableType]
internal struct StructWithObjectReferences
{
    public FlatClass Field1;
    public int Field2;
}

[SerializableType]
internal enum SerializableEnum
{
    Option1,
    Option2,
    Option3
}

internal class UnregisteredClass
{
}

[SerializableType]
internal class ClassWithIntField
{
    public int Field1;
}

[SerializableType]
internal class ClassWithMultipleObjectFields
{
    public object? Field1;
    public object? Field2;
    public object? Field3;
}

[SerializableType]
internal class ClassThatImplementsInterface : IInterface
{
    private int field;

    public int Value
    {
        get { return field; }
        set { field = value; }

    }
}

[SerializableType]
internal class ClassWithInterfaceField
{
    public int Field1;
    public IInterface? Field2;
    public int Field3;
}

internal interface IInterface
{
    int Value { get; set; }
}

[SerializableType]
internal struct StructThatImplementsInterface : IInterface
{
    private int field;

    public int Value
    {
        get { return field; }
        set { field = value; }
    }
}

[SerializableType]
internal class ClassWithSerializationHooks : ISerializationListener
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
internal class ClassWithStructWithSerializationHooksField
{
    public int Field1;
    public StructWithSerializationHooks Field2;
    public int Field3;
}

[SerializableType]
internal struct StructWithSerializationHooks : ISerializationListener
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

internal struct StructWithNestedStruct
{
    public NestedStruct Field;
}

internal struct NestedStruct
{
    public int Field;
}

[SerializableType]
internal class BaseClass
{
    public int FieldBase;
}

[SerializableType]
internal class FormerBaseClass : BaseClass
{
    public int FieldFormerBase1;
    public int FieldFormerBase2;
}

[SerializableType]
internal class DerivedClass : BaseClass
{
    public int FieldDerived;
}

[SerializableType]
internal class ClassWithEnumField
{
    public int Field1;
    public SerializableEnum Field2;
    public int Field3;
}

[SerializableType]
internal class ClassWithObjectField
{
    public int Field1;
    public object? Field2;
    public int Field3;
}

[SerializableType]
internal class ClassWithTypeField
{
    public int Field1;
    public Type? Field2;
    public int Field3;
}

[SerializableType]
internal class ClassWithRenamedField
{
    public int Field1;
    [PreviousName("Field9000")]
    public int Field2;
    public int Field3;
}

[SerializableType]
internal class ClassWithReadonlyField
{
    public readonly int Field;

    public ClassWithReadonlyField()
    {
        Field = 123;
    }
}

[SerializableType]
internal class ClassWithPrivateConstructor
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
internal class ClassWithNoDefaultConstructor(int field)
{
    public readonly int Field = field;
}

[SerializableType]
internal class ClassWithSerializedField
{
    [SerializableField]
    public int Field1 = 0;

    public int Field2 = 0;
}

[SerializableType]
internal class GenericClass<T>
{
    public T? Field = default;
}

internal delegate int Del(int arg);

[SerializableType]
internal class ClassWithIntPtrField
{
    public int Field1;
    public nint Field2;
    public int Field3;
}

[SerializableType]
internal class ClassWithDelegateField
{
    public int Field1;
    public Del? Field2;
    public int Field3;
}

internal struct UnregisteredStruct
{
}

[SerializableType]
internal class ClassWithUnregisteredStructFieldType
{
    public int Field1;
    public UnregisteredStruct Field2;
    public int Field3;
}

[SerializableType]
internal struct RegisteredStruct
{
}

[SerializableType]
internal class ClassWithRegisteredStructFieldType
{
    public int Field1;
    public RegisteredStruct Field2;
    public int Field3;
}

internal struct SurrogateStruct : ISerializationSurrogate
{
    public string? Field1;
    public string? Field2;

    public void Record(object obj)
    {
        SerializableStruct objToSave = (SerializableStruct)obj;
        Field1 = (objToSave.Field1 + 1).ToString();
        Field2 = (objToSave.Field2 + 1).ToString();
    }

    public object Restore(object obj)
    {
        SerializableStruct objToRestore = (SerializableStruct)obj;
        objToRestore.Field1 = int.Parse(Field1!);
        objToRestore.Field2 = int.Parse(Field2!);
        return objToRestore;
    }
}

internal class SurrogateClass : ISerializationSurrogate
{
    public string? Field1;
    public string? Field2;

    public void Record(object obj)
    {
        SerializableClass objToSave = (SerializableClass)obj;
        Field1 = (objToSave.Field1 + 1).ToString();
        Field2 = (objToSave.Field2 + 1).ToString();
    }

    public object Restore(object obj)
    {
        SerializableClass objToRestore = (SerializableClass)obj;
        objToRestore.Field1 = int.Parse(Field1!);
        objToRestore.Field2 = int.Parse(Field2!);
        return objToRestore;
    }
}

#pragma warning restore 0649 // "never assigned to"

