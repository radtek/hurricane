using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;

namespace Fushare.Filesystem {
  /// <summary>
  /// Defines common behavior of all FilesysEventHandles.
  /// </summary>
  public abstract class FilesysEventHandlerBase : IFilesysEventHandler {

    #region Fields
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FilesysEventHandlerBase));
    // Mono doesn't allow use of file scheme in matching.
    protected readonly Uri UriBaseAddress = new Uri("http://localhost/");
    protected readonly ServerProxy _serverProxy;
    protected readonly FushareFileManager _fileManager;
    protected readonly FusharePathFactory _pathFactory;
    #endregion

    #region Constructor
    public FilesysEventHandlerBase(ServerProxy serverProxy, FushareFileManager 
      fileManager, FusharePathFactory pathFactory) {
      _serverProxy = serverProxy;
      _fileManager = fileManager;
      _pathFactory = pathFactory;
    }
    #endregion

    #region IFilesysEventHandler Members

    public abstract void HandleGettingPathStatus(IFushareFilesys sender,
      GetPathStatusEventArgs args);

    public abstract void HandleReleasedFile(IFushareFilesys sender,
      ReleaseFileEventArgs args);

    public abstract void HandleReadingFile(IFushareFilesys sender,
      ReadFileEventArgs args);

    #endregion

    /// <summary>
    /// Tries to match the path with the templateString
    /// </summary>
    /// <param name="templateString">The template string.</param>
    /// <param name="pathString">The path string.</param>
    /// <param name="match">The match.</param>
    /// <returns>True if successful.</returns>
    protected bool TryMatchPath(string templateString, string pathString, 
      out UriTemplateMatch match) {
      var uriTemplate = new UriTemplate(templateString);
      var pathUri = new Uri(UriBaseAddress, pathString);
      try {
        match = uriTemplate.Match(UriBaseAddress, pathUri);
      } catch (Exception ex) {
        Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
          "Failed to match path. Exception: {0}", ex));
        match = null;
      }
      if (match == null) {
        return false;
      } else {
        return true;
      }
    }
  }
}
