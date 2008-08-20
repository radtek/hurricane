using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare.Filesystem {
  /// <summary>
  /// Defines handler that processes requests from FUSE.
  /// </summary>
  public interface IPathHandler {
    void ProcessRequest(FuseContext context);
    bool IsReusable { get; }
  }
}
