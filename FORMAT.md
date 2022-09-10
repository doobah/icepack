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
    - The type ID of the parent type (or 0 if no parent) [uint]
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
  - For each field:
    - The serialized form of the field value [?]
```

Other rules:

* Immutable types (primitive, string, decimal) as well as Type objects do not have any object data since they are serialized as metadata.
* Object references are serialized as their object ID (`uint`).
* The first object in the object list is the root object.
* Primitives are serialized as-is.
* Built-in primitive types, along with `string`, `decimal`, and `Type` are automatically registered for serialization.
* Enum literal fields are serialized as their underlying integral type.
* Interfaces are serialized as object references.
* The compatibility version indicates which other versions of the serializer are able to deserialize the output.
* A type object is serialized as the ID of a type in the serialized type table. This means that the type value itself must be a registered type.
* On serializing an enum, the serializer adds the underlying type to the type table before the enum type itself.
