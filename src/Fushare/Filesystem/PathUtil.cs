using System;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using System.Web;

using Brunet;
using Fushare.Configuration;

namespace Fushare.Filesystem {

  /// <summary>
  /// Utiliy class that facilitates path operations.
  /// </summary>
  /// <remarks>
  /// Paths should start with "/" but shouldn't end with "/".<br>
  /// Definition of paths in Fushare:
  /// <list type="table">
  /// <item>
  /// <term>FUSE Path</term>
  /// <description>
  /// Virutal path represented in the FUSE system.
  /// </description>
  /// </item>
  /// <item>
  /// <term>Raw FUSE Path</term>
  /// <description>
  /// The same as FUSE Path except that it includes parameter string, if 
  /// present.
  /// </description>
  /// </item>
  /// <item>
  /// <term>Shadow Path</term>
  /// <description>
  /// Shadow path rooted in the shadow directory.
  /// </description>
  /// </item>
  /// <item>
  /// <term>Shadow Full Path</term>
  /// <description>
  /// Fully qualified shadow path rooted in the system root: "/"
  /// </description>
  /// </item>
  /// </list>
  /// Usage of this class:<br>
  /// Call PathUtil.Instance.Initialize before first.
  /// </remarks>
  public class PathUtil {
    #region Fields
    private string _shadow_root;
    private string _fuse_root;
    private static bool _initialized = false;
    private static PathUtil _instance = new PathUtil(); 
    #endregion

    /// <summary>
    /// Provides a char to mark the starting point of parameters in a path 
    /// string.
    /// </summary>
    public static readonly char ParameterStarterChar;
    /// <summary>
    /// Provides a char that connects the parameters
    /// </summary>
    public static readonly char ParameterSeparatorChar;
    /// <summary>
    /// Provides a char that assigns the value to a parameter. 
    /// Usually the equal sign.
    /// </summary>
    public static readonly char ParameterAssignmentOpChar;

    public string ShadowRoot {
      get {
        return _shadow_root;
      }
    }

    public string FuseRoot {
      get {
        return _fuse_root;
      }
    }
    
    static PathUtil() {
      // Initialize the path special chars.
      // @todo Do we need to read them from config files?
      if (Fushare.SysEnvironment.OSVersion == OS.Unix) {
        ParameterSeparatorChar = '&';
        ParameterStarterChar = '?';
        ParameterAssignmentOpChar = '=';
      } else {
        ParameterSeparatorChar = '&';
        // Windows doesn't allow '?' in paths
        ParameterStarterChar = '@';
        ParameterAssignmentOpChar = '=';
      }
    }

    /// <summary>
    /// Returns the singleton instance of PathUtil
    /// </summary>
    public static PathUtil Instance {
      get {
        return _instance;
      }
    }

    /// <summary>
    /// Initializes PathUtil
    /// </summary>
    public void Initialize(string fuseRootPath, string shadowRootPath) {
      CheckAndTrimPath(ref fuseRootPath);
      CheckAndTrimPath(ref shadowRootPath);
      _shadow_root = shadowRootPath;
      _fuse_root = fuseRootPath;
      _initialized = true;
    }

    /// <summary>
    /// Checks validity of path.
    /// </summary>
    /// <remarks>
    /// This checks the validity of a path in Fushare system in general and 
    /// doesn't target a specific type of paths.
    /// </remarks>
    /// <exception cref="ArgumentException">Path invalid</exception>
    private static void CheckAndTrimPath(ref string path) {
      string errorMsg;
      if (!Path.IsPathRooted(path)) {
        errorMsg = "Path should start with " + Path.DirectorySeparatorChar;
        throw new ArgumentException(errorMsg, "path");
      }
      path = path.TrimEnd(new char[] { Path.DirectorySeparatorChar });
    }

    /// <summary>
    /// Checks if the instance is initialized.
    /// </summary>
    private void CheckInitialized() {
      if (!_initialized) {
        throw new InvalidOperationException(
          "PathUtil not initialized. Call initialize first.");
      }
    }
    /// <summary>
    /// Returns the full shadow path of the given shadow path.
    /// </summary>
    public ShadowFullPath GetShadowFullPath(ShadowPath shadowPath) {
      CheckInitialized();
      string shadow_path_string = shadowPath.PathString;
      CheckAndTrimPath(ref shadow_path_string);
      shadow_path_string = shadow_path_string.Remove(0, 1);
      return new ShadowFullPath(Path.Combine(_shadow_root, shadow_path_string));
    }

    public static VirtualPath GetFusePathFromFuseRawPath(VirtualRawPath fuseRawPath) {
      string fuse_raw_string = fuseRawPath.PathString;
      CheckAndTrimPath(ref fuse_raw_string);
      int substr_length = fuse_raw_string.LastIndexOf(PathUtil.ParameterStarterChar);
      return new VirtualPath(fuse_raw_string.Substring(
        0, substr_length == -1 ? fuse_raw_string.Length : substr_length));
    }

    /// <summary>
    /// Parse the rawFusePath into DirectoryInfo or FileInfo and parameters.
    /// </summary>
    /// <param name="rawFusePath"></param>
    /// <param name="shadowFullPathInfo">FileInfo or DirectoryInfo of the 
    /// specified path, or null if nothing exists on the path.</param>
    public ShadowFullPath ParseFuseRawPath(VirtualRawPath fuseRawPath, out FileSystemInfo 
      shadowFullPathInfo, out NameValueCollection parameters) {
      if (FushareConfigHandler.ConfigObject.filesysConfig.shadowPathEqualsFusePath) {
        parameters = GetParamsFromRawFusePath(fuseRawPath);
        VirtualPath fuse_path = GetFusePathFromFuseRawPath(fuseRawPath);
        // shadow path = fuse path
        ShadowFullPath shadow_full_path = GetShadowFullPath(new ShadowPath(fuse_path.PathString));
        FileInfo file_info = new FileInfo(shadow_full_path.PathString);
        if (file_info.Exists) {
          shadowFullPathInfo = file_info;
        } else {
          DirectoryInfo dir_info = new DirectoryInfo(shadow_full_path.PathString);
          if (dir_info.Exists) {
            shadowFullPathInfo = dir_info;
          } else {
            shadowFullPathInfo = null;
          }
        }
        return shadow_full_path;
      } else {
        throw new InvalidOperationException("Cannot simple take fuse path as shadow path");
      }
    }

    /// <summary>
    /// Parses query string extracted from fusePath into a NameValueCollection.
    /// </summary>
    public static NameValueCollection GetParamsFromRawFusePath(VirtualRawPath fuseRawPath) {
      var fuseRawPathStr = fuseRawPath.PathString;
      CheckAndTrimPath(ref fuseRawPathStr);
      NameValueCollection ret;
      int starter_char_index = fuseRawPathStr.LastIndexOf(ParameterStarterChar);
      if (starter_char_index == -1) {
        // No parameter
        ret = new NameValueCollection();
      } else {
        string query_string = fuseRawPathStr.Substring(starter_char_index + 1);
        ret = ParseQueryString(query_string);
      }
      return ret;
    }

    #region Copied and Modified from Mono source. Revision: 97804
    public static NameValueCollection ParseQueryString(string query) {
      return ParseQueryString(query, Encoding.UTF8);
    }

    internal static NameValueCollection ParseQueryString(string query, Encoding encoding) {
      if (query == null)
        throw new ArgumentNullException("query");
      if (encoding == null)
        throw new ArgumentNullException("encoding");
      if (query.Length == 0 || (query.Length == 1 && query[0] == ParameterStarterChar))
        return new NameValueCollection();
      if (query[0] == ParameterStarterChar)
        query = query.Substring(1);

      NameValueCollection result = new NameValueCollection();
      ParseQueryString(query, encoding, result);
      return result;
    }

    internal static void ParseQueryString(string query, Encoding encoding, NameValueCollection result) {
      if (query.Length == 0)
        return;

      string decoded = HttpUtility.HtmlDecode(query);
      int decodedLength = decoded.Length;
      int namePos = 0;
      bool first = true;
      while (namePos <= decodedLength) {
        int valuePos = -1, valueEnd = -1;
        for (int q = namePos; q < decodedLength; q++) {
          if (valuePos == -1 && decoded[q] == ParameterAssignmentOpChar) {
            valuePos = q + 1;
          } else if (decoded[q] == ParameterSeparatorChar) {
            valueEnd = q;
            break;
          }
        }

        if (first) {
          first = false;
          if (decoded[namePos] == ParameterStarterChar)
            namePos++;
        }

        string name, value;
        if (valuePos == -1) {
          name = null;
          valuePos = namePos;
        } else {
          name = HttpUtility.UrlDecode(decoded.Substring(namePos, valuePos - namePos - 1), encoding);
        }
        if (valueEnd < 0) {
          namePos = -1;
          valueEnd = decoded.Length;
        } else {
          namePos = valueEnd + 1;
        }
        value = HttpUtility.UrlDecode(decoded.Substring(valuePos, valueEnd - valuePos), encoding);

        result.Add(name, value);
        if (namePos == -1)
          break;
      }
    } 
    #endregion
	}
}
