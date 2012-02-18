using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;

namespace GSeries.Filesystem {
  /// <summary>
  /// Defines common behavior of all FilesysEventHandles.
  /// </summary>
  public abstract class FilesysEventHandlerBase : IFilesysEventHandler {

    #region Fields
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FilesysEventHandlerBase));
    // Mono doesn't allow use of file scheme in matching.
    protected readonly Uri UriBaseAddress = new Uri("http://localhost/");
    protected readonly ServerProxy _serverProxy;
    protected readonly FilesysManager _fileManager;
    protected readonly PathFactory _pathFactory;
    protected readonly FilesysContext _filesysContext;
    #endregion

    #region Constructor
    public FilesysEventHandlerBase(ServerProxy serverProxy, FilesysManager 
      fileManager, PathFactory pathFactory, FilesysContext filesysContext) {
      _serverProxy = serverProxy;
      _fileManager = fileManager;
      _pathFactory = pathFactory;
      _filesysContext = filesysContext;
    }
    #endregion

    #region IFilesysEventHandler Members

    public abstract void HandleGettingPathStatus(IFilesys sender,
      GetPathStatusEventArgs args);

    public abstract void HandleReleasedFile(IFilesys sender,
      ReleaseFileEventArgs args);

    public abstract void HandleReadingFile(IFilesys sender,
      ReadFileEventArgs args);

    public abstract void HandleOpeningFile(IFilesys sender,
      OpenFileEventArgs args);

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
