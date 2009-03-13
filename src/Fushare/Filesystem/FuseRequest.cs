using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Fushare.Filesystem {
  /// <summary>
  /// Values of the request sent by FUSE filesystem.
  /// </summary>
  public class FuseRequest {
    #region Fields
    private FuseMethod _fuse_method;
    private FuseRawPath _raw_fuse_path; 
    #endregion

    public NameValueCollection Params {
      get {
        return PathUtil.GetParamsFromRawFusePath(_raw_fuse_path);
      }
    }

    public FuseMethod FuseMethod {
      get {
        return _fuse_method;
      }
    }

    /// <summary>
    /// The raw URL includes the query string, if present.
    /// </summary>
    public FuseRawPath FuseRawPath {
      get {
        return _raw_fuse_path;
      }
    }

    public FusePath FusePath {
      get {
        return new FusePath(_raw_fuse_path.PathString.Substring(0, _raw_fuse_path.PathString.LastIndexOf(
          PathUtil.ParameterStarterChar)));
      }
    }
    
    public FuseRequest(FuseRawPath path, FuseMethod method) {
      _raw_fuse_path = path;
      _fuse_method = method;
    }

  }
}
