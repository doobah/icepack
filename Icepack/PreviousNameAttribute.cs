using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class PreviousNameAttribute : Attribute
    {
        public string Name { get; }

        public PreviousNameAttribute(string name)
        {
            Name = name;
        }
    }
}
