using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace GSeries.Filesystem {
  /// <summary>
  /// Represents a path used in GatorShare.
  /// </summary>
  /// <remarks>
  /// A major use of this path wrapper would be for methods to be able to 
  /// constrain the input path type and prevent mistakes.
  /// </remarks>
  public abstract class FilesysPath {
    protected readonly string _pathString;
    
    /// <summary>
    /// This should the a string path conforms to the PathType.
    /// </summary>
    /// <remarks>
    /// Paths should start with "/" (be rooted) but shouldn't end with "/". ("\" on 
    /// Windows.) Missing "/" in the head causes exception but when in the tail it is 
    /// simply trimmed.
    /// </remarks>
    public string PathString {
      get { return _pathString; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilesysPath"/> class.
    /// </summary>
    /// <param name="pathString">
    /// The path string. It is stored no matter what form of the string is. (Whether it 
    /// represents a virtual path or real path)
    /// </param>
    public FilesysPath(string pathString) {
      IOUtil.CheckPathRooted(pathString);
      _pathString = pathString.TrimEnd(Path.DirectorySeparatorChar);
    }

    public string[] Segments {
      get {
        return PathString.Split(new char[] { Path.DirectorySeparatorChar }, 
          StringSplitOptions.RemoveEmptyEntries);
      }
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="GSeries.Filesystem.FilesysPath"/> 
    /// to <see cref="System.String"/>.
    /// </summary>
    /// <param name="path">The path object.</param>
    /// <returns>The path string.</returns>
    public static explicit operator string(FilesysPath path) {
      return path.PathString;
    }

    /// <summary>
    /// The hash code is its path string's hash code.
    /// </summary>
    /// <returns>
    /// A hash code for this instance.
    /// </returns>
    public override int GetHashCode() {
      return _pathString.GetHashCode();
    }

    public override string ToString() {
      return _pathString;
    }

  }

  /// <summary>
  /// The base class for virtual path types.
  /// </summary>
  public abstract class VirtualPathBase : FilesysPath {
    public VirtualPathBase(string pathString) : base(pathString) { }

    /// <summary>
    /// Trims the raw path args.
    /// </summary>
    /// <param name="rawPath">The raw path.</param>
    /// <returns></returns>
    protected static string TrimRawPathArgs(string rawPath) {
      // Uri methods help strip the parameters.
      return new Uri(string.Format(
      "http://localhost/{0}", rawPath)).LocalPath;
    }
  }

  /// <summary>
  /// The same as <see cref="VirtualPath"/> except that it includes parameter 
  /// string, if present.
  /// </summary>
  public class VirtualRawPath : VirtualPathBase {
    public VirtualRawPath(string pathString) : base(pathString) { }

    /// <summary>
    /// Returns the virtual path out of the raw path.
    /// </summary>
    /// <value>The virtual path.</value>
    public VirtualPath VirtualPath {
      get {
        string virtualPathStr = TrimRawPathArgs(_pathString);
        return new VirtualPath(virtualPathStr);
      }
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="GSeries.Filesystem.VirtualRawPath"/> 
    /// to <see cref="GSeries.Filesystem.VirtualPath"/>.
    /// </summary>
    /// <param name="vrp">The VirtualRawPath.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator VirtualPath(VirtualRawPath vrp) {
      return vrp.VirtualPath;
    }
  }

  /// <summary>
  /// The virutal path represented in the user level file system.
  /// </summary>
  public class VirtualPath : VirtualPathBase {
    public VirtualPath(string pathString) : base(pathString) { }

    /// <summary>
    /// Creates the virtual path from raw string.
    /// </summary>
    /// <param name="virtualRawPath">The virtual raw path.</param>
    /// <remarks>
    /// This is a convenience method.
    /// </remarks>
    public static VirtualPath CreateFromRawString(
      string virtualRawPath) {
      return new VirtualPath(new VirtualRawPath(virtualRawPath));
    }

    public VirtualPath(VirtualRawPath vrp)
      : base(TrimRawPathArgs(vrp.PathString)) {
    }
  }

  /// <summary>
  /// Unused.
  /// </summary>
  [Obsolete("Unused")]
  public class ShadowPath : FilesysPath {
    public ShadowPath(string pathString) : base(pathString) { }

  }

  /// <summary>
  /// Fully qualified shadow path rooted in the system root: "/"
  /// </summary>
  public class ShadowFullPath : FilesysPath {
    [Obsolete("Unsafe to use this constructor")]
    public ShadowFullPath(string pathString) : 
      base(pathString) { }

    internal ShadowFullPath(string shadowDirPath, VirtualPath vp)
      : base(
      Path.Combine(shadowDirPath, vp.PathString.TrimStart(
      Path.DirectorySeparatorChar))) { }

  }

  /// <summary>
  /// Full path of a meta file in a shadow directory.
  /// </summary>
  public class ShadowMetaFullPath : ShadowFullPath {
    const string MetaDirName = "meta";
    internal ShadowMetaFullPath(string shadowDirPath, VirtualPath vp)
      : base(Path.Combine(shadowDirPath, MetaDirName), vp) { }
  }

  /// <summary>
  /// The path to the shadow directory. This gives a strong type to the path so that 
  /// dependency injection tools can automatically use it.
  /// </summary>
  public class ShadowDirPath {
    public readonly string PathString;

    public ShadowDirPath(string path) {
      IOUtil.CheckPathRooted(path);
      PathString = path;
    }
  }

  /// <summary>
  /// The virtual path for vitual files which contain meta info for the real files.
  /// </summary>
  public class VirtualMetaPath : VirtualPath {
    const string MetaDirName = "meta";

    static string PrefixMetaDir(string path) {
      return string.Format("{0}{1}{0}{2}", Path.DirectorySeparatorChar, 
        MetaDirName, path);
    }

    public VirtualMetaPath(VirtualRawPath vrp)
      : base(string.Format(PrefixMetaDir(TrimRawPathArgs(vrp.PathString)))) {
    }
  }
}
