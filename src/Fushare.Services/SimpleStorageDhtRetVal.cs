using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  /// <summary>
  /// The value object returned by the GET operation of 
  /// <see cref="SimpleStorageDht"/>
  /// </summary>
  public class SimpleStorageDhtRetVal {
    /// <summary>
    /// List of values associated with the queried key.
    /// </summary>
    /// <remarks>Cannot be IList(T) otherwise JsonExSerializer doesn't recognize it.
    /// </remarks>
    public List<string> values;
  }
}
