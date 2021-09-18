
* field attribute: isReference, areItemsReference
* deserialize
* serialization callbacks: onbeforeserialize, onafterdeserialize
* serialize dictionary, hashset

///////////////////////////////////////

serialized format:

  [objects, types]

where

  objects:
  
    [ object 1, object 2, ... ]
    
    where
    
      first element is the root object

      each object has the format:

        all numeric types:  JSON number          

        enum:               JSON number          

        bool:               JSON boolean          

        string:             JSON string          

        object reference:   JSON number        

        struct:        
          [ type id, field 1 val, field 2 val, ... ]

        object:
          [
            id,
            type id,
            [id of containing type, field 1 val, field 2 val],
            [id of containing type, field 1 val]
          ]
          
      an object with a surrogate type will be formatted as that type
      
      a field with a custom formatter will be formatted accordingly

  types:

    [
      [ type id, type name, field 1 name, field 2 name, ... ],
      ...
    ]

Rules:

* Serializing boxed value types is not supported
* Value types do not require a [SerializableObject] attribute to be serializable