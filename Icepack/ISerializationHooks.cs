using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Defines an interface for an object that performs logic before serialization, and after deserialization. </summary>
    public interface ISerializationHooks
    {
        /// <summary> Called before the object is serialized. </summary>
        void OnBeforeSerialize();

        /// <summary> Called after the object is deserialized. </summary>
        void OnAfterDeserialize();
    }
}
