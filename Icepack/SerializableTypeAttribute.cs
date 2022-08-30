﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Allows a class or struct to be serialized/deserialized. </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = true)]
    public sealed class SerializableTypeAttribute : Attribute
    {
        /// <summary>
        /// Whether references to objects of this type should be preserved. Default is true. This overrides
        /// any field-specific setting.
        /// </summary>
        public bool PreserveReferences { get; }

        /// <summary> Creates a new <see cref="SerializableTypeAttribute"/>. </summary>
        /// <param name="preserveReferences"> Whether references to objects of this type should be preserved. </param>
        public SerializableTypeAttribute(bool preserveReferences = true)
        {
            PreserveReferences = preserveReferences;
        }
    }
}
