<h1 align="center">
  <img align="center" src="Resources/LogoText.svg" style="width:300px;height:64px;" alt="ICEPACK">
</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/Icepack/">
    <img src="https://img.shields.io/nuget/v/icepack.svg?style=flat-square">
  </a>
</p>

<br>

Icepack is a lightweight binary serialization library for C#.

It was specifically developed to address limitations that other serialization libraries have when serializing inheritance-based hierarchies, and to make it easy for code frameworks to implement serializable base classes.

<br>

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
  * If a class was derived from another class that is now missing or is no longer a base class, the missing or former base class is ignored, and the serializer resumes deserializing fields from further ancestors.
  * The `PreviousName` attribute can be assigned to a field to indicate that the name of the field has changed, so that the correct field will be matched with what is in the serialized data.

# Limitations

* Only fields (both private and public) are serialized, not properties.
* `nuint`, `nint`, and delegates are not supported.
* Deserializing after changing the type of a serialized field results in undefined behaviour.
* Changing the name of a type will result in the serializer ignoring objects of that type.

# Examples

## Registering Types

Types must be registered before they can be serialized. Types can be lazy-registered by annotating them with the `SerializableType` attribute.

```
[SerializableType]
public class ExampleType { }
```

Types can also be registered via the serializer's `RegisterType` method. This is necessary in order to serialize types defined in third-party libraries.

```
var serializer = new Serializer();
serializer.RegisterType(typeof(ExampleType));
```

## Serialization and Deserialization

The serializer serializes and deserializes an object graph to and from a stream.

```
var serializer = new Serializer();

var obj = new ExampleType();

var stream = new MemoryStream();
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
