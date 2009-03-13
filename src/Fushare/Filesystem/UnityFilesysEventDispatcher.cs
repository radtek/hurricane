using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;

namespace Fushare.Filesystem {
  /// <summary>
  /// Implements the dispather using UnityContainer.
  /// </summary>
  public class UnityFilesysEventDispatcher : FilesysEventDispatcher {
    IUnityContainer _container;

    public UnityFilesysEventDispatcher(IFushareFilesys fushareFilesys, 
      IUnityContainer container) : base(fushareFilesys) {
      _container = container;
    }

    protected override IFilesysEventHandler GetEventHandler(IFushareFilesys sender, 
      FushareFilesysEventArgs args) {
      if (IsPathIgnored(args.VritualPath)) {
        return new NopFilesysEventHandler();
      }
      var handlerName = GetHandlerName(args.VritualPath);
      if(string.IsNullOrEmpty(handlerName)) {
        return new NopFilesysEventHandler();
      } else {
        // The decision depends solely on the first segment of the path.
        return _container.Resolve<IFilesysEventHandler>(handlerName);
      }
    }

    private bool IsPathIgnored(FusharePath path) {
      return false;
    }

    private static string GetHandlerName(VirtualRawPath path) {
      if (path.Segments.Length > 0) {
        return path.Segments[0];
      } else {
        return null;
      }
    }
  }
}
