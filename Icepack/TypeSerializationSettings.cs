using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Contains options that control how objects of a type are serialized. </summary>
    public sealed class TypeSerializationSettings
    {
        /// <summary>
        /// Whether references to objects of this type should be preserved. Default is true. This overrides
        /// any field-specific setting.
        /// </summary>
        public bool PreserveReferences { get; }

        /// <summary> Creates a new <see cref="TypeSerializationSettings"/>. </summary>
        /// <param name="preserveReferences"> Whether references to objects of this type should be preserved. </param>
        public TypeSerializationSettings(bool preserveReferences = true)
        {
            PreserveReferences = preserveReferences;
        }

        internal static TypeSerializationSettings FromSerializableTypeAttribute(SerializableTypeAttribute attr)
        {
            return new TypeSerializationSettings(
                preserveReferences: attr.PreserveReferences
            );
        }
    }
}
