using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Fushare.Filesystem {
  /// <summary>
  /// Represents a path used in Fushare.
  /// </summary>
  /// <remarks>
  /// A major use of this path wrapper would be for methods to be able to 
  /// constrain the input path type and prevent mistakes.
  /// </remarks>
  public abstract class FusharePath {
    private string _pathString;
    
    /// <summary>
    /// This should the a string path conforms to the PathType.
    /// </summary>
    /// <remarks>
    /// Paths should start with "/" but shouldn't end with "/". ("\" on Windows.) "/" in 
    /// the head causes exception but when in the tail it is simply trimmed.
    /// </remarks>
    public string PathString {
      get {
        return _pathString;
      }
      protected set {
        IOUtil.CheckPathRooted(value);
        _pathString = value.TrimEnd(Path.DirectorySeparatorChar);
      }
    }

    public FusharePath() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FusharePath"/> class.
    /// </summary>
    /// <param name="pathString">
    /// The path string. It is stored no matter what form of the string is. (Whether it 
    /// represents a virtual path or real path)
    /// </param>
    public FusharePath(string pathString) {
      PathString = pathString;
    }

    public string[] Segments {
      get {
        return PathString.Split(new char[] { Path.DirectorySeparatorChar }, 
          StringSplitOptions.RemoveEmptyEntries);
      }
    }

    public static explicit operator string(FusharePath path) {
      return path.PathString;
    }
  }

  /// <summary>
  /// The same as <see cref="VirtualPath"/> except that it includes parameter 
  /// string, if present.
  /// </summary>
  public class VirtualRawPath : FusharePath {
    public VirtualRawPath(string pathString) : base(pathString) { }
  }

  /// <summary>
  /// The virutal path represented in the user level file system.
  /// </summary>
  public class VirtualPath : FusharePath {
    public VirtualPath(string pathString) : base(pathString) { }

    public VirtualPath(VirtualRawPath vrp) {
      // Uri methods help strip the parameters.
      var uri = new Uri(vrp.PathString, UriKind.Relative);
      var paramsStrippedPath = uri.LocalPath;
      PathString = paramsStrippedPath;
    }
  }


  public class ShadowPath : FusharePath {
    public ShadowPath(string pathString) : base(pathString) { }
  }

  /// <summary>
  /// Fully qualified shadow path rooted in the system root: "/"
  /// </summary>
  public class ShadowFullPath : FusharePath {

    public ShadowFullPath(string pathString) : 
      base(pathString) { }

    public ShadowFullPath(string shadowDirPath, VirtualPath vp) {
      PathString = Path.Combine(shadowDirPath, vp.PathString);
    }

  }

}
