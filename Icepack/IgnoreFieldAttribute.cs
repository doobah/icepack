using System;

namespace Icepack
{
    /// <summary> Marks a field to be ignored during serialization/deserialization. </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class IgnoreFieldAttribute : Attribute { }
}
