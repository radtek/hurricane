using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GSeries.Filesystem {
  /// <summary>
  /// Serves as the base class for classes that dispatch file system events to 
  /// corresponding handlers.
  /// </summary>
  public abstract class FilesysEventDispatcher {
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FilesysEventDispatcher));

    public IFilesys Filesys { get; private set; }

    public FilesysEventDispatcher(IFilesys fushareFilesys) {
      Filesys = fushareFilesys;
      Filesys.GettingPathStatus += 
        new EventHandler<GetPathStatusEventArgs>(FushareFilesys_GettingPathStatus);
      Filesys.ReadingFile += 
        new EventHandler<ReadFileEventArgs>(FushareFilesys_ReadingFile);
      Filesys.ReleasedFile += 
        new EventHandler<ReleaseFileEventArgs>(FushareFilesys_ReleasedFile);
      Filesys.OpeningFile += new EventHandler<OpenFileEventArgs>(FushareFilesys_OpeningFile);
    }

    #region Dispatching Methods
    void FushareFilesys_ReleasedFile(object sender, ReleaseFileEventArgs e) {
      GetEventHandler(sender as IFilesys, e).HandleReleasedFile(
        sender as IFilesys, e);
    }

    void FushareFilesys_ReadingFile(object sender, ReadFileEventArgs e) {
      GetEventHandler(sender as IFilesys, e).HandleReadingFile(
        sender as IFilesys, e);
    }

    void FushareFilesys_GettingPathStatus(object sender, GetPathStatusEventArgs e) {
      GetEventHandler(sender as IFilesys, e).HandleGettingPathStatus(
        sender as IFilesys, e);
    }

    void FushareFilesys_OpeningFile(object sender, OpenFileEventArgs e) {
      GetEventHandler(sender as IFilesys, e).HandleOpeningFile(sender as IFilesys, e);
    }

    #endregion

    #region Abstract Methods
    /// <summary>
    /// Gets the event handler.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">
    /// The <see cref="GSeries.Filesystem.FilesysEventArgs"/> instance 
    /// containing the event data.</param>
    /// <returns>The event handler. Returns <see cref="NopFilesysEventHandler"/> 
    /// if no other handler matches.</returns>
    protected abstract IFilesysEventHandler GetEventHandler(IFilesys sender,
      FilesysEventArgs args); 
    #endregion
  }
}
