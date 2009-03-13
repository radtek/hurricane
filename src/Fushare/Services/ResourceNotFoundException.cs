using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  public class ResourceNotFoundException : ResourceException{
    public ResourceNotFoundException() : base() { }
    public ResourceNotFoundException(string message) : base(message) { }
    public ResourceNotFoundException(string message, Exception innerException) :
      base(message, innerException) { }
  }
}
