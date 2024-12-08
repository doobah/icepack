using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack;

/// <summary>
/// Defines an interface for a type that is a substitute for another type during serialization. This is useful when
/// you need to serialize types that are defined in an assembly that you do not control.
/// </summary>
public interface ISerializationSurrogate
{
    /// <summary> Populates this object according to the state of the original object. </summary>
    /// <param name="original"> The object that is being substituted. </param>
    void Record(object original);

    /// <summary> Populates the original object according to the state of this object. </summary>
    /// <param name="original"> The object to be restored from this substitute. </param>
    /// <returns> The object to be restored from this substitute. </returns>
    object Restore(object original);
}
