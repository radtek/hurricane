using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Fushare.Filesystem {
  public class FuseController {
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FuseController));
    private static FuseController _instance;

    public static FuseController Instance {
      get {
        if (_instance == null) {
          _instance = new FuseController();
        }
        return _instance;
      }
    }

    /// <summary>
    /// Executes the request from FUSE file system.
    /// </summary>
    /// <returns>True if the request is considered executed. False otherwise.</returns>
    public bool Execute(VirtualRawPath path, FuseMethod method) {
      bool ret;
      IPathHandler handler = PathHandlerFactory.Instance.GetHandler(method, path);
      if (handler != null) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Got IPathHandler: {0} for VirtualRawPath: {1}", 
          handler.GetType(), path.PathString));
        FuseRequest request = new FuseRequest(path, method);
        FuseResponse response = new FuseResponse();
        FuseContext context = new FuseContext(request, response);
        handler.ProcessRequest(context);
        ret = true;
      } else {
        ret = false;
      }
      return ret;
    }
  }
}
