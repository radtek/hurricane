using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  /// <summary>
  /// The exception that is thrown when the provided Resource key already exists in Dht.
  /// </summary>
  public class DuplicateResourceKeyException : ResourceException {
    public DuplicateResourceKeyException() : base() { }
    public DuplicateResourceKeyException(string message) : base(message) { }
    public DuplicateResourceKeyException(string message, Exception innerException) :
      base(message, innerException) { }
  }
}
