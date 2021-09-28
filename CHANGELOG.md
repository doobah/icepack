# v0.0.9



# v0.0.8

* Initialize capacity of lists, hashsets, dictionaries to equal length to avoid reallocation.
* Treat `string` similarly to other reference types. This is to keep objects a fixed size to make it easier to add support for handling deleted fields, etc, later on. It also allows for nullable string fields. Additionally, this significantly improves the efficiency of deserializing many instances of the same string.
* Change parent ID in type metadata to a flag indicating whether a type has a parent.
* Remove `ValueOnlyAttribute`. Serializing object references is already fairly performant, and serializing classes inline makes it hard to reason about the serialized size of an object, and adds a lot of unnecessary complexity.

# v0.0.7

* Precalculating serialization and deserialization operations for fields and types eliminates redundant type checks, greatly improving efficiency.

# v0.0.6

* Switched to a binary format. This uses BinaryReader/Writer and is about twice as fast.

# v0.0.5

* Switched the order of types and objecs in the output format.

# v0.0.4

* Index type metadata by type, not name. This speeds up serialization.
* Modify logic to not include an ID field for a type, this slightly improves deserialization time since the type ID can be inferred from the declaration order in the document.

# v0.0.3

* Modify logic to not include an ID field for an object, this improves deserialization time since the object ID can be inferred from the declaration order in the document.
* Rename some public types.

# v0.0.2

* Support for serializing structs, hashsets, and dictionaries.
* Allow marking reference fields to be serialized as value-only.
* Modify output format to not enclose values in double-quotes.
* Fixed a bug when serializing multiple references to the same object.
* All tests passing.

# v0.0.1

* Basic functionality in place, including serialization/deserialization, type registering, and support for arrays and lists.
