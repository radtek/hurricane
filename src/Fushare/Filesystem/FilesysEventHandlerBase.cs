using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Fushare.Filesystem {
  /// <summary>
  /// Defines common behavior of all FilesysEventHandles.
  /// </summary>
  public abstract class FilesysEventHandlerBase : IFilesysEventHandler {

    #region Fields
    protected readonly Uri FileUriBaseAddress = new Uri("file:///");
    public readonly ServerProxy _serverProxy; 
    #endregion

    #region Constructor
    public FilesysEventHandlerBase(ServerProxy serverProxy) {
      _serverProxy = serverProxy;
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

    protected bool TryMatchPath(string templateString, string pathString, 
      out UriTemplateMatch match) {
      var uriTemplate = new UriTemplate(templateString);
      var pathUri = new Uri(FileUriBaseAddress, pathString);
      var result = uriTemplate.Match(FileUriBaseAddress, pathUri);
      if (result == null) {
        match = null;
        return false;
      } else {
        match = result;
        return true;
      }
    }
  }
}
