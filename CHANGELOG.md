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
