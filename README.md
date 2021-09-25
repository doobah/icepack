# Overview

Icepack is a lightweight serialization library for C#.

It was designed as part of a game development project, specifically to address limitations that other serialization libraries have when serializing inheritance hierarchies. Libraries such as MessagePack and Protobuf provide a way for the user to inform the serializer about class hierarchies, by assigning fixed IDs to child classes. A scenario where this does not work well is a game engine's entity/component system, where in order to build functionality on top of the engine, the user needs to extend some classes exposed by the engine. If the user then wishes to import third-party libraries that extend the same classes, it is very impractical to find out which IDs have already been used, and to stay backwards compatible. Icepack solves this by including type information as part of the serialization format, while avoiding the verbosity of serializers like Json.NET by automatically assigning IDs to types and field names, and storing these in lookup tables. The additional size overhead of the type and field names is reasonable for game development projects, where the serialized objects are likely to be large and composed of hierarchies containing many instances of the same types, for example, scenes or state machines. Icepack also avoids another limitation of other libraries, by relating every field to the class that it is declared in, so that there are no field name clashes between classes in the same inheritance hierarchy.

In terms of performance, Icepack is faster than string-based serializers like Json.NET. However, it has several of its own limitations such as:

* It is a binary format so is not human-readable.
* Currently only fields are serialized, although this could be fairly easily expanded to properties.
* The library lacks most of the extensibility/customization features of other libraries.

# Serialization Format

The Icepack serializer uses a BinaryWriter internally to generate its output, as a byte stream encoded in UTF-16, little endian:

```
- Compatibility version [ushort]
- The number of types [int]
- For each type:
  - The type's assembly qualified name [string]
  - The ID of the type's parent [uint]
  - The number of serializable fields in the type [int]
  - For each field:
    - The field name [string]
- The number of objects [int]
- A flag stating whether the root object is a reference type [bool]
- For each reference-type object:
  - The object's type ID [uint]
  - If the object is an array:
    - The length of the array [int]
- If the root object is a value type:
  - The serialized form of the root object [?]
- For each object:
  - The serialized form of that object [?] (see sub-formats below)
```

Structs have the format:

```
- Type ID [uint]
- For each field:
  - The serialized form of the field value [?]
```

Arrays, Lists (based on **List<>**), and HashSets (based on **HashSet<>**) have the format:

```
- Type ID [uint]
- Length [int]
- For each element:
  - The serialized form of that element [?]
```

Dictionaries (based on **Dictionary<,>**) have the format:

```
- Type ID [uint]
- Number of items [int]
- For each key/value pair:
  - The serialized form of the key [?]
  - The serialized form of the value [?]
```

Other classes have the format:

```
- Type ID [uint]
- Starting from the class type, and iterating up the inheritance chain until 'object':
  - Type ID [uint]
  - For each field:
    - The serialized form of the field value [?]
```

Other rules:

* Object references are serialized as their object ID (`uint`).
* The first object in the object list is the root object, this does not have to be a reference type, but the rest of the objects in the list do.
* Objects marked to be serialized as value-only are serialized inline and not included in the object list.
* Primitives are serialized as-is.
* Strings are prefixed with their length.
* Enums are serialized as their underlying integral type.
* `nuint` and `nint` are not supported.
* `span` and other exotic types are not supported.
* The compatibility version indicates which other versions of the Icepack serializer are able to deserialize the output.

# Other Features

* Types can be included for serialization by calling the serializer's `RegisterType` method, or annotating the type with the `SerializableObject` attribute.
* Fields can be ignored by annotating them with the `IgnoreField` attribute.
* Fields that are reference-type are serialized as references by default, but these can be serialized as value-only using the `ValueOnly` attribute.
* The `ISerializer` interface is provided to allow classes to execute additional logic before serialization and after deserialization.

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
