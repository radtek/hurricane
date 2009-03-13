using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  public class DhtException : ResourceException {
    public DhtException() : base() { }
    public DhtException(string message) : base(message) { }
    public DhtException(string message, Exception innerException) : 
      base(message, innerException) { }
  }
}
