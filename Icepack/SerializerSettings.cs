using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Options for the serializer. </summary>
    public sealed class SerializerSettings
    {
        /// <summary>
        /// Whether references will be preserved. True by default. If false, references to the same object
        /// will be serialized as separate objects.
        /// </summary>
        public bool PreserveReferences { get; }

        /// <summary>
        /// The maximum depth allowed when references are not preserved. If the depth exceeds this value, it is assumed that there is a
        /// circular reference, and an exception will be thrown. Default is 1000.
        /// </summary>
        public int MaxDepth { get; }

        /// <summary>
        /// Whether fields are serialized by default. If true, a field is always serialized unless annotated with <see cref="IgnoreFieldAttribute"/>.
        /// If false, a field is only serialized if annotated with <see cref="SerializableFieldAttribute"/>. Default is true.
        /// </summary>
        public bool SerializeByDefault { get; }

        /// <summary> Creates a new <see cref="SerializerSettings"/>. </summary>
        /// <param name="preserveReferences">
        /// Whether references will be preserved. True by default. If false, references to the same object
        /// will be serialized as separate objects. Overrides any type or field specific setting.
        /// </param>
        /// <param name="maxDepth">
        /// The maximum depth allowed when references are not preserved. If the depth exceeds this value, it is assumed that there is a
        /// circular reference, and an exception will be thrown. Default is 1000.
        /// </param>
        /// <param name="serializeByDefault">
        /// Whether fields are serialized by default. If true, a field is always serialized unless annotated with <see cref="IgnoreFieldAttribute"/>.
        /// If false, a field is only serialized if annotated with <see cref="SerializableFieldAttribute"/>. Default is true.
        /// </param>
        public SerializerSettings(
            bool preserveReferences = true,
            int maxDepth = 1000,
            bool serializeByDefault = true
        )
        { 
            PreserveReferences = preserveReferences;
            MaxDepth = maxDepth;
            SerializeByDefault = serializeByDefault;
        }
    }
}
