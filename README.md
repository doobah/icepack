# Icepack

[![NuGet version (Icepack)](https://img.shields.io/nuget/v/icepack.svg?style=flat-square)](https://www.nuget.org/packages/Icepack/)

Icepack is a lightweight serialization library for C#.

It was designed as part of a game development project, specifically to address limitations that other serialization libraries have when serializing inheritance hierarchies. Libraries such as MessagePack and Protobuf provide a way for the user to inform the serializer about class hierarchies, by assigning fixed IDs to child classes. A scenario where this does not work well is a game engine's entity/component system, where in order to build functionality on top of the engine, the user needs to extend some classes exposed by the engine. If the user then wishes to import third-party libraries that extend the same classes, it is very impractical to find out which IDs have already been used, and to stay backwards compatible. Icepack solves this by including type information as part of the serialization format, while avoiding the verbosity of serializers like Json.NET by automatically assigning IDs to types and field names, and storing these in lookup tables. The additional size overhead of the type and field names is reasonable for game development projects, where the serialized object graphs are likely to be large and composed of many instances of the same types, for example, scenes or state machines. Icepack also avoids clashes between identically-named fields in different classes in the same inheritance hierarchy, by relating every field to the class that it is declared in.

# Serialization Format

The Icepack serializer uses a `BinaryWriter` internally to generate its output, as a byte stream encoded in UTF-16, little endian:

```
- Compatibility version [ushort]
- The number of types [int]
- For each type:
  - The type's assembly qualified name [string]
  - If the type is an immutable type (primitive, string, decimal):
    - Category ID = 0 [byte]
  - If the type is an array:
    - Category ID = 1 [byte]
    - Size of an item [int]
  - If the type is a list:
    - Category ID = 2 [byte]
    - Size of an item [int]
  - If the type is a hashset:
    - Category ID = 3 [byte]
    - Size of an item [int]
  - If the type is a dictionary:
    - Category ID = 4 [byte]
    - Size of a key [int]
    - Size of an item [int]
  - If the type is a regular struct:
    - Category ID = 5 [byte]
    - Size of an instance [int]
    - The number of serializable fields in the type [int]
    - For each field:
      - The field name [string]
      - The size of the field in bytes [int]
  - If the type is a regular class:
    - Category ID = 6 [byte]
    - Size of an instance [int]
    - Whether the type has a parent [bool]
    - The number of serializable fields in the type [int]
    - For each field:
      - The field name [string]
      - The size of the field in bytes [int]
  - If the type is an enum:
    - Category ID = 7 [byte]
    - The type ID of the underlying type [uint]
  - If the type is Type:
    - Category ID = 8 [byte]
- The number of objects [int]
- For each object, include some metadata used to pre-instantiate the object:
  - The object's type ID [uint]
  - If the object is an immutable type:
    - The value of the object [?]
  - If the object is an array, list, hashset, or dictionary:
    - Length [uint]
  - If the object is an enum:
    - The underlying value of the enum [?]
  - If the object is a Type:
    - The ID of the type [uint]
- For each object:
  - The serialized form of that object [?]
```

Structs have the format:

```
- Type ID [uint]
- For each field:
  - The serialized form of the field value [?]
```

Immutable types (primitive, string, decimal) as well as Type objects do not have any object data since they are serialized as metadata:

```
(Empty)
```

Arrays, Lists (based on `List<>`), and HashSets (based on `HashSet<>`) have the format:

```
- For each element:
  - The serialized form of that element [?]
```

Dictionaries (based on `Dictionary<,>`) have the format:

```
- For each key/value pair:
  - The serialized form of the key [?]
  - The serialized form of the value [?]
```

Other classes have the format:

```
- Starting from the class type, and iterating up the inheritance chain until 'object':
  - Type ID [uint]
  - For each field:
    - The serialized form of the field value [?]
```

Other rules:

* Object references are serialized as their object ID (`uint`).
* The first object in the object list is the root object.
* Primitives are serialized as-is.
* Built-in primitive types, along with `string`, `decimal`, and `Type` are automatically registered for serialization.
* Enum literal fields are serialized as their underlying integral type.
* Interfaces are serialized as object references.
* The compatibility version indicates which other versions of the serializer are able to deserialize the output.
* A type object is serialized as the ID of a type in the serialized type table. This means that the type value itself must be a registered type.
* On serializing an enum, the serializer adds the underlying type to the type table before the enum type itself.

# Other Features

* Types can be included for serialization by calling the serializer's `RegisterType` method, or annotating the type with the `SerializableObject` attribute.
* Fields can be ignored by annotating them with the `IgnoreField` attribute.
* The `ISerializerListener` interface is provided to allow classes and structs to execute additional logic before serialization and after deserialization.
* Readonly fields are supported.
* The serializer can call private constructors.
* Arrays, lists, hashsets, and dictionaries are registered automatically, but their generic parameter types must be registered separately.
* Deserialization is somewhat resilient to changes to the data types since serialization:
  * Fields that have been added/removed to/from a class since serialization will be ignored.
  * A field that was serialized as a reference to an instance of a missing class is ignored.
  * If a class was derived from another class that is now missing or is no longer a base class, the missing or former base class is ignored, and the serializer resumes deserializing fields from further ancestors.
  * The `PreviousName` attribute can be assigned to a field to indicate that the name of the field has changed, so that the correct field will be matched with what is in the serialized data.

# Limitations

* Currently only fields (both private and public) are serialized, not properties.
* `nuint` and `nint` are not supported.
* `span` and other exotic types are not supported.
* Deserializing after changing the type of a serialized field results in undefined behaviour.
* Changing the name of a type will result in the serializer ignoring objects of that type.

# Usage Example

The following example demonstrates some capabilities of Icepack:

```
using System;
using Icepack;
using System.IO;

namespace Example
{
    [SerializableObject]
    abstract class ClassZ
    {
        private string field1;

        public ClassZ(string field1)
        {
            this.field1 = field1;
        }

        public override string ToString()
        {
            return $"ClassZ.field1={field1}";
        }

        public ClassZ() { }
    }

    [SerializableObject]
    class ClassA : ClassZ
    {
        private int field1;
        private ClassA field2;

        public ClassA(int field1, ClassA field2, string baseField1) : base(baseField1)
        {
            this.field1 = field1;
            this.field2 = field2;
        }

        public ClassA() : base() { }

        public override string ToString()
        {
            return $"[ClassA.field1={field1}, ClassA.field2={field2}, {base.ToString()}]";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Serializer serializer = new Serializer();

            ClassA nestedObj = new ClassA(123, null, "asdf");
            ClassA rootObj = new ClassA(456, nestedObj, "qwer");

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(rootObj, stream);

            Console.WriteLine("___Serialized Output___");
            Console.WriteLine(Convert.ToHexString(stream.ToArray()));
            Console.WriteLine("");

            stream.Position = 0;
            ClassA deserializedObj = serializer.Deserialize<ClassA>(stream);
            Console.WriteLine("___Deserialized Object___");
            Console.WriteLine(deserializedObj);
        }
    }
}
```

which gives the output:

```
___Serialized Output___
010003000000A4014500780061006D0070006C0065002E0043006C0061007300730041002C0020005400650073007400500072006F006A006500630074002C002000560065007200730069006F006E003D0031002E0030002E0030002E0030002C002000430075006C0074007500720065003D006E00650075007400720061006C002C0020005000750062006C00690063004B006500790054006F006B0065006E003D006E0075006C006C00060800000001020000000C6600690065006C0064003100040000000C6600690065006C006400320004000000A4014500780061006D0070006C0065002E0043006C006100730073005A002C0020005400650073007400500072006F006A006500630074002C002000560065007200730069006F006E003D0031002E0030002E0030002E0030002C002000430075006C0074007500720065003D006E00650075007400720061006C002C0020005000750062006C00690063004B006500790054006F006B0065006E003D006E0075006C006C00060400000000010000000C6600690065006C006400310004000000D001530079007300740065006D002E0053007400720069006E0067002C002000530079007300740065006D002E0050007200690076006100740065002E0043006F00720065004C00690062002C002000560065007200730069006F006E003D0035002E0030002E0030002E0030002C002000430075006C0074007500720065003D006E00650075007400720061006C002C0020005000750062006C00690063004B006500790054006F006B0065006E003D00370063006500630038003500640037006200650061003700370039003800650000040000000100000001000000030000000871007700650072000300000008610073006400660001000000C8010000020000000200000003000000010000007B000000000000000200000004000000

___Deserialized Object___
[ClassA.field1=456, ClassA.field2=[ClassA.field1=123, ClassA.field2=, ClassZ.field1=asdf], ClassZ.field1=qwer]
```
