using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using System.Collections;

namespace GSeries.Filesystem {
  /// <summary>
  /// Implements the dispather using UnityContainer.
  /// </summary>
  public class UnityFilesysEventDispatcher : FilesysEventDispatcher {
    readonly IUnityContainer _container;
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(UnityFilesysEventDispatcher));

    public UnityFilesysEventDispatcher(IFilesys fushareFilesys, 
      IUnityContainer container) : base(fushareFilesys) {
      _container = container;
    }

    /// <summary>
    /// Gets the event handler.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">
    /// The <see cref="GSeries.Filesystem.FilesysEventArgs"/> instance
    /// containing the event data.</param>
    /// <returns>
    /// The event handler. Returns <see cref="NopFilesysEventHandler"/>
    /// if no other handler matches.
    /// </returns>
    protected override IFilesysEventHandler GetEventHandler(IFilesys sender, 
      FilesysEventArgs args) {
      if (IsPathIgnored(args.VritualRawPath)) {
        return new NopFilesysEventHandler();
      }
      var handlerName = GetHandlerName(args.VritualRawPath);
      if(string.IsNullOrEmpty(handlerName)) {
        return new NopFilesysEventHandler();
      } else {
        // The decision depends solely on the first segment of the path.
        IFilesysEventHandler handler = null;
        try {
          handler = _container.Resolve<IFilesysEventHandler>(handlerName);
        } catch (ResolutionFailedException) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "Failed to resovle hanlder for the given path: {0}.", 
            args.VritualRawPath.PathString));
        }
        if (handler == null) {
          return new NopFilesysEventHandler();
        } else {
          return handler;
        }
      }
    }

    #region Private Methods
    private bool IsPathIgnored(FilesysPath path) {
      return false;
    }

    private static string GetHandlerName(VirtualRawPath path) {
      if (path.Segments.Length > 0) {
        return path.Segments[0];
      } else {
        return null;
      }
    } 
    #endregion
  }
}
