using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GatorShare {
  /// <summary>
  /// Exception that is thrown when the service encounters non-transient error.
  /// </summary>
  /// <remarks>It should be used for an unrecoverable error which will cause
  /// operation to fail.</remarks>
  public class NonTransientException : Exception {
    public NonTransientException() : base() { }
    public NonTransientException(string message) : base(message) { }
    public NonTransientException(string message, Exception innerException) :
      base(message, innerException) { }
  }
}
