using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Fushare.Configuration;

namespace Fushare.Filesystem {
  /// <summary>
  /// Factory class to create IPathHandler objects.
  /// </summary>
  public class PathHandlerFactory {
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(PathHandlerFactory));
    private static PathHandlerFactory _instance;
    private Dictionary<Type, IPathHandler> _reusable_handlers = 
      new Dictionary<Type,IPathHandler>();

    public static PathHandlerFactory Instance {
      get {
        if (_instance == null) {
          _instance = new PathHandlerFactory();
        }
        return _instance;
      }
    }

    private PathHandlerFactory() { }

    public IPathHandler GetHandler(FuseMethod requestType, FuseRawPath path) {
      // FuseContext is not used currently in this factory
      return GetHandler(null, requestType, path);
    }

    #region IPathHandlerFactory Members

    public IPathHandler GetHandler(FuseContext context, FuseMethod requestType, 
      FuseRawPath path) {
      Type type;
      IPathHandler ret;
      //@todo Use regex.
      FusePath fuse_path = PathUtil.GetFusePathFromFuseRawPath(path);
      Logger.WriteLineIf(LogLevel.Verbose, FuseFS.FilesysLogProps,
        string.Format("Received request from FusePath {0}", fuse_path.PathString));
      if (fuse_path.PathString.EndsWith(".bt")) {

        if (requestType == FuseMethod.Read) {
          // Here we don't konw whether it is a file but for path manipulation 
          // purpose, it's OK.
          try {
            Brunet.Base32.Decode(Path.ChangeExtension(
                    new FileInfo(fuse_path.PathString).Name, null));
          } catch (Exception ex) {
            // There is a good chance that the path shouldn't be read from 
            // BitTorrent, so we catch it here and don't pass it along
            Logger.WriteLineIf(LogLevel.Verbose, FuseFS.FilesysLogProps,
              string.Format("Path can't be parsed at DhtKey. \n {0}", ex));
            return null;
          }
        }

        type = typeof(Fushare.Services.BitTorrent.BitTorrentPathHandler);
        if (_reusable_handlers.ContainsKey(type)) {
          ret = _reusable_handlers[type];
        } else {
          // @TODO Fix this. This is not right. You create a object with nothing from
          // the client, which means from the client's point of view, it's static. 
          BitTorrentConfigSection btconfig = 
            FushareConfigHandler.ConfigObject.bitTorrentConfig;
          string bt_base_dir = System.IO.Path.Combine(PathUtil.Instance.ShadowRoot, 
            btconfig.btTmpDirName);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("BitTorrent init properties: Base folder: {0}, client port : {1}, tracker port {2}",
            bt_base_dir, btconfig.clientListenPort, btconfig.dhtTrackerPort));
          ret = new Fushare.Services.BitTorrent.BitTorrentPathHandler(
            bt_base_dir,
            btconfig.clientListenPort,
            btconfig.dhtTrackerPort);
        }
      } else {
        Logger.WriteLineIf(LogLevel.Verbose, FuseFS.FilesysLogProps,
          string.Format("Factory didn't find a handler for this path: {0}", path.PathString));
        type = null;
        ret = null;
      }

      if (ret != null && ret.IsReusable) {
        _reusable_handlers.Add(ret.GetType(), ret);
      }
      return ret;
    }
    #endregion
  }
}
