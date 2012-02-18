using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GSeries.Services {
  /// <summary>
  /// Base class for exceptions in the GatorShare services.
  /// </summary>
  public class ServiceException : Exception {
    public ServiceException() : base() { }
    public ServiceException(string message) : base(message) { }
    public ServiceException(string message, Exception innerException) :
      base(message, innerException) { }

    public string NameSpace { get; set; }
    public string Name { get; set; }
  }
}
