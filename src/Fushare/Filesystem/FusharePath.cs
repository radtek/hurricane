using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare.Filesystem {
  /// <summary>
  /// Represents a path used in Fushare.
  /// </summary>
  /// <remarks>
  /// A major use of this path wrapper would be for methods to be able to 
  /// constrain the input path type and prevent mistakes.
  /// </remarks>
  public abstract class FusharePath {
    /// <summary>
    /// This should the a string path conforms to the PathType.
    /// </summary>
    protected readonly string _path_string;

    public FusharePath(string pathString) {
      _path_string = pathString;
    }

    public string PathString {
      get {
        return _path_string;
      }
    }
  }

  public class FuseRawPath : FusharePath {
    public FuseRawPath(string pathString) : base(pathString) { }
  }

  public class FusePath : FusharePath {
    public FusePath(string pathString) : base(pathString) { }
  }

  public class ShadowPath : FusharePath {
    public ShadowPath(string pathString) : base(pathString) { }
  }

  public class ShadowFullPath : FusharePath {
    public ShadowFullPath(string pathString) : base(pathString) { }
  }

}
