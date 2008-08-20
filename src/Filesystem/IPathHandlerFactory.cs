using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare.Filesystem {
  /// <summary>
  /// Returns an instance of a class that implements the IPathHandler 
  /// interface.
  /// </summary>
  public interface IPathHandlerFactory {
    IPathHandler GetHandler(FuseContext context, FuseMethod requestType, 
      string rawFusePath);
  }
}
