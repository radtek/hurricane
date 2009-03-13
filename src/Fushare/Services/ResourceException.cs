using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  /// <summary>
  /// Thrown if there is error occurres when consuming the resource.
  /// </summary>
  public class ResourceException : Exception {
    public ResourceException() : base() { }
    public ResourceException(string message) : base(message) { }
    public ResourceException(string message, Exception innerException) :
      base(message, innerException) { }

    /// <summary>
    /// Gets or sets the resource key.
    /// </summary>
    /// <value>The resource key.</value>
    /// <remarks>Could be a key, name, uri, etc.</remarks>
    public string ResourceKey {
      get {
        return _resourceKey;
      }
      set {
        _resourceKey = value;
      }
    }

    string _resourceKey = string.Empty;

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      sb.Append(base.ToString());
      sb.Append(System.Environment.NewLine);
      sb.Append(string.Format("Resource Key: {0}", ResourceKey));
      return sb.ToString();
    }
  }
}
