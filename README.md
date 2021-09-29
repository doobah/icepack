# Overview

Icepack is a lightweight serialization library for C#.

It was designed as part of a game development project, specifically to address limitations that other serialization libraries have when serializing inheritance hierarchies. Libraries such as MessagePack and Protobuf provide a way for the user to inform the serializer about class hierarchies, by assigning fixed IDs to child classes. A scenario where this does not work well is a game engine's entity/component system, where in order to build functionality on top of the engine, the user needs to extend some classes exposed by the engine. If the user then wishes to import third-party libraries that extend the same classes, it is very impractical to find out which IDs have already been used, and to stay backwards compatible. Icepack solves this by including type information as part of the serialization format, while avoiding the verbosity of serializers like Json.NET by automatically assigning IDs to types and field names, and storing these in lookup tables. The additional size overhead of the type and field names is reasonable for game development projects, where the serialized objects are likely to be large and composed of hierarchies containing many instances of the same types, for example, scenes or state machines. Icepack also avoids another limitation of other libraries, by relating every field to the class that it is declared in, so that there are no field name clashes between classes in the same inheritance hierarchy.

# Serialization Format

The Icepack serializer uses a `BinaryWriter` internally to generate its output, as a byte stream encoded in UTF-16, little endian:

```
- Compatibility version [ushort]
- The number of types [int]
- For each type:
  - The type's assembly qualified name [string]
  - If the type is a string:
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
  - If the type is a struct:
    - Category ID = 5 [byte]
    - Size of an instance [int]
    - The number of serializable fields in the type [int]
    - For each field:
      - The field name [string]
      - The size of the field in bytes [int]
  - If the type is a normal class:
    - Category ID = 6 [byte]
    - Size of an instance [int]
    - Whether the type has a parent [bool]
    - The number of serializable fields in the type [int]
    - For each field:
      - The field name [string]
      - The size of the field in bytes [int]
- The number of objects [int]
- For each reference-type object, include some metadata used to pre-instantiate the object:
  - The object's type ID [uint]
  - If the object is a string:
    - The value of the string [string]
  - If the object is an array, list, hashset, or dictionary:
    - Length [uint]
- A flag stating whether the root object is a value type [bool]
- If the root object is a value type:
  - The serialized form of the root object [?]
- For each object:
  - The serialized form of that object [?]
```

Structs have the format:

```
- Type ID [uint]
- For each field:
  - The serialized form of the field value [?]
```

Strings do not have any object data since since they are already serialized as metadata:

```
(Empty)
```

Arrays have the format:

```
- For each element:
  - The serialized form of that element [?]
```

Lists (based on `List<>`), and HashSets (based on `HashSet<>`) have the format:

```
- Length [int]
- For each element:
  - The serialized form of that element [?]
```

Dictionaries (based on `Dictionary<,>`) have the format:

```
- Length [int]
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
* The first object in the object list is the root object, this does not have to be a reference type, but the rest of the objects in the list do.
* Primitives are serialized as-is.
* The `string` type is automatically registered for serialization.
* Enums are serialized as their underlying integral type.
* `nuint` and `nint` are not supported.
* Fields of type `object` or `ValueType` are not supported.
* `span` and other exotic types are not supported.
* Boxed value types are not supported.
* Serializing interfaces is not supported.
* Serializing delegates is not supported.
* The compatibility version indicates which other versions of the Icepack serializer are able to deserialize the output.
* Currently only fields (both private and public) are serialized, not properties.

# Other Features

* Types can be included for serialization by calling the serializer's `RegisterType` method, or annotating the type with the `SerializableObject` attribute.
* Fields can be ignored by annotating them with the `IgnoreField` attribute.
* The `ISerializer` interface is provided to allow classes to execute additional logic before serialization and after deserialization.
* On deserialization, fields that have been added or removed since serialization will be ignored. Deserializing after changing the type of a serialized field results in undefined behaviour. A field that was serialized as an instance of a missing class is also ignored.

# Usage Example

The following example demonstrates some capabilities of Icepack:

```
using System;
using Icepack;

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

            string str = serializer.Serialize(rootObj);
            Console.WriteLine("___Serialized Output___");
            Console.WriteLine(str);
            Console.WriteLine("");

            ClassA deserializedObj = serializer.Deserialize<ClassA>(str);
            Console.WriteLine("___Deserialized Object___");
            Console.WriteLine(deserializedObj);
        }
    }
}
```

which gives the output:

```
___Serialized Output___
[[[Example.ClassA\, TestProject\, Version=1.0.0.0\, Culture=neutral\, PublicKeyToken=null,field1,field2],[Example.ClassZ\, TestProject\, Version=1.0.0.0\, Culture=neutral\, PublicKeyToken=null,field1]],[[0,[0,456,2],[1,qwer]],[0,[0,123,0],[1,asdf]]]]

___Deserialized Object___
[ClassA.field1=456, ClassA.field2=[ClassA.field1=123, ClassA.field2=, ClassZ.field1=asdf], ClassZ.field1=qwer]
```
