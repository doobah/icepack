using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> An exception thrown by the serializer. </summary>
    public sealed class IcepackException : Exception
    {
        /// <summary> Create a new <see cref="IcepackException"/>. </summary>
        /// <param name="message"> The details of the exception. </param>
        /// <param name="innerException"> The exception that caused this one. </param>
        public IcepackException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
