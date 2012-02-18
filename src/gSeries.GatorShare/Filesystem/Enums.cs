using System;
using System.Collections.Generic;
using System.Text;

namespace GSeries.Filesystem {
  /// <summary>
  /// Enumerates the method that FUSE system uses to make the request to 
  /// Fushare.
  /// </summary>
  public enum FuseMethod {
    /// <summary>
    /// FUSE tries to read/get something.
    /// </summary>
    Read,
    /// <summary>
    /// FUSE tries to write/post something.
    /// </summary>
    Write
  }

  public enum PathType {
    RawFusePath,
    FusePath,
    ShadowPath,
    ShadowFullPath
  }
}
