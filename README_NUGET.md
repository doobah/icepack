# ![ICEPACK](https://raw.githubusercontent.com/okaniku/icepack/master/Resources/LogoText.png)

Icepack is a lightweight binary serialization library for C#.

It was originally developed for a game development project to address limitations that other serialization libraries have when serializing inheritance-based hierarchies, and to make it easy for code frameworks to implement serializable base classes.

# Features

* Object references are preserved by default, although this can be disabled globally.
* The serializer serializes all fields by default, but can be configured to only serialize fields annotated with the `SerializableField` attribute. Fields can be ignored by the serializer by annotating them with the `IgnoreField` attribute.
* The format slightly favours deserialization performance over serialization performance. This is often important for games since the end-user is usually more affected by load times.
* Types can be included for serialization by calling the serializer's `RegisterType` method, or annotating the type with the `SerializableType` attribute.
* Fields can be ignored by annotating them with the `IgnoreField` attribute.
* The `ISerializationListener` interface is provided to allow classes and structs to execute additional logic before serialization and after deserialization.
* Readonly fields are supported.
* Interfaces are supported.
* Boxed value types are supported.
* The serializer can handle derived and base classes that define fields with the same name.
* The serializer can call private constructors.
* Arrays, and `List<T>`, `HashSet<T>`, and `Dictionary<T>` are registered automatically, but their generic parameter types must be registered explicitly.
* Deserialization is somewhat resilient to changes to the data types since serialization:
  * Fields that have been added/removed to/from a class since serialization will be ignored.
  * A field that was serialized as a reference to an instance of a missing class is ignored.
  * If a class was derived from another class that is now missing or is no longer a base class, the missing or former base class is ignored, and the serializer resumes deserializing fields from other ancestors.
  * The `PreviousName` attribute can be assigned to a field to indicate that the name of the field has changed, so that the correct field will be matched with what is in the serialized data.
* Surrogate types.

# Limitations

* Only fields (both private and public) are serialized, not properties.
* `nuint`, `nint`, delegates, pointers, Span, and generic type definitions are not supported.
* Deserializing after changing the type of a serialized field results in undefined behaviour.
* Changing the name or namespace of a type will result in the serializer ignoring objects of that type.

# Examples

## Registering Types

Types must be registered before they can be serialized. Types can be lazy-registered by annotating them with the `SerializableType` attribute.

```
[SerializableType]
public class ExampleType { }
```

Types can also be registered via the serializer's `RegisterType` method. This is needed to serialize types defined in third-party libraries.

```
Serializer serializer = new();
serializer.RegisterType(typeof(ExampleType));
```

## Serialization and Deserialization

The serializer serializes and deserializes an object graph to and from a stream.

```
Serializer serializer = new();

ExampleType obj = new();

MemoryStream stream = new();
serializer.Serialize(obj, stream);
stream.Position = 0;
ExampleType? deserializedObj = serializer.Deserialize<ExampleType>(stream);
stream.Close();
```

## Serialization Callbacks

The `ISerializationListener` is used to execute logic immediately before serialization and after deserialization.

```
[SerializableType]
public class ExampleClass : ISerializationListener
{
  public int Field;

  public void OnBeforeSerialize()
  {
    Console.WriteLine("Before serializing.");
  }

  public void OnAfterDeserialize()
  {
    Console.WriteLine("After deserialized.");
  }
}
```

## Surrogate Types

A type can be registered with a surrogate type which replaces the original type during serialization/deserialization.
This is useful when you need to serialize types that you cannot modify. This feature has some limitations, including:

* Only the surrogate of the object type is applied, surrogates of base types are not applied.
* A surrogate of a reference type must be a reference type, while a surrogate of a value type must be a value type.
* Surrogate types cannot have surrogate types.
* A surrogate type cannot reference the original type.

```
[SerializableType]
internal struct SerializableStruct
{
  public int Field;
}

[SerializableType]
public struct SurrogateStruct : ISerializationSurrogate
{
  public int Field;

  public void Record(object obj)
  {
    SerializableStruct objToRecord = (SerializableStruct)obj;
    Field = objToRecord.Field;
  }

  public object Restore(object obj)
  {
    SerializableStruct objToRestore = (SerializableStruct)obj;
    objToRestore.Field = int.Parse(Field);
    return objToRestore;
  }
}

Serializer serializer = new();
serializer.RegisterType(typeof(SerializableStruct), typeof(SurrogateStruct));

SerializableStruct obj = new() { Field = 123 };

MemoryStream stream = new();
serializer.Serialize(obj, stream);
stream.Position = 0;
SerializableStruct? deserializedObj = serializer.Deserialize<SerializableStruct>(stream);
stream.Close();
```
