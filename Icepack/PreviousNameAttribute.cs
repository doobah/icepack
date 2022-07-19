using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Allows the serializer to match a serialized field with the corresponding renamed one. </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class PreviousNameAttribute : Attribute
    {
        /// <summary> The previous name of the field. </summary>
        public string Name { get; }

        /// <summary> Creates a new <see cref="PreviousNameAttribute"/>. </summary>
        /// <param name="name"> The previous name of the field. </param>
        public PreviousNameAttribute(string name)
        {
            Name = name;
        }
    }
}
