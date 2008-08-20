using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare.Filesystem {
  /// <summary>
  /// Encapsulates all FUSE-specific information about an individual FUSE request.
  /// </summary>
  public class FuseContext {
    private FuseRequest _request;
    private FuseResponse _response;

    public FuseRequest Request {
      get {
        return _request;
      }
    }

    public FuseResponse Response {
      get {
        return _response;
      }
    }

    public FuseContext(FuseRequest request, FuseResponse response) {
      _request = request;
      _response = response;
    }
  }
}
