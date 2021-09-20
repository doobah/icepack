# Overview

Icepack is a lightweight serialization library for C#.

It was designed as part of a game development project, specifically to address limitations that other serialization libraries have when serializing inheritance hierarchies. Libraries such as MessagePack and Protobuf provide a way for the user to inform the serializer about class hierarchies, by assigning fixed IDs to child classes. A scenario where this does not work well is a game engine's entity/component system, where in order to build functionality on top of the engine, the user needs to extend some classes exposed by the engine. If the user then wishes to import third-party libraries that extend the same classes, it is very impractical to find out which IDs have already been used, and to stay backwards compatible. Icepack solves this by including type information as part of the serialization format, while avoiding the verbosity of serializers like Json.NET by automatically assigning IDs to types and field names, and storing these in lookup tables. The additional size overhead of the type and field names is reasonable for game development projects, where the serialized objects are likely to be large and composed of hierarchies containing many instances of the same types, for example, scenes or state machines. Icepack also avoids another limitation of Json.NET, by relating every field to the class that it is declared in, so that there are no field name clashes between classes in the same inheritance hierarchy.

In terms of performance, Icepack is similar to Json.NET, and in some cases faster, especially when serializing object references. However, it has several of its own limitations such as:

* The lack of key/value pairs in the output makes it much less human-readable than JSON.
* Currently only fields are serialized, although this could be fairly easily expanded to properties.
* The library lacks most of the extensibility/customization features of other libraries.

# Serialization Format

The format of an Icepack document is (not including whitespace):

```
[ objects, types ]
```

The format of `objects` is:

```  
[ object 1, object 2, ... ]
```
    
where

* The first element is the root object
* All elements in the format are either an array, e.g. `[ item 1, item 2 ]`, or a string, e.g. `some text`.
* Arrays can be nested.
* The supported types and their formats are:
  * **All numeric types:** A string representation of the number.
  * **Boolean:** `0` if false, `1` if true.
  * **Enum:** A string representation of the underlying numeric value.
  * **String:** The string text. The characters `,]\` are escaped with a `\` character.
  * **Object reference:** The ID of the referenced object.
  * **Struct:**
    ```
    [ type id, field 1, field 2, ... ]
    ```
  * **Array/List/HashSet:**
    ```
    [ type id, element 1, element 2, ... ]
    ``` 
  * **Dictionary:**
    ```
    [ type id, key 1, key 2, ..., value 1, value 2, ... ]
    ```
  * **A regular class:**
    ```
    [
      type id,
      [ id of class type, field 1, field 2, ... ],
      [ id of base class type, field 1, field 2, ... ],
      [ id of base-base class type, field 1, field 2, ... ],
      ...
    ]
    ```

The format of `types` is:

```
[
  [ type 1 id, type 2 name, field 1 name, field 2 name, ... ],
  [ type 2 id, type 2 name, field 1 name, field 2 name, ... ],
  ...
]
```

# Usage Example

The following example demonstrates some capabilities of Icepack:

```
using System;
using Icepack;

namespace Example
{
    [IcepackObject]
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

    [IcepackObject]
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
[[[1,[1,456,2],[2,qwer]],[1,[1,123,0],[2,asdf]]],[[1,Example.ClassA\, TestProject\, Version=1.0.0.0\, Culture=neutral\, PublicKeyToken=null,field1,field2],[2,Example.ClassZ\, TestProject\, Version=1.0.0.0\, Culture=neutral\, PublicKeyToken=null,field1]]]

___Deserialized Object___
[ClassA.field1=456, ClassA.field2=[ClassA.field1=123, ClassA.field2=, ClassZ.field1=asdf], ClassZ.field1=qwer]
```
