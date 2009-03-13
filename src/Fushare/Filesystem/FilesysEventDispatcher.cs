using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Filesystem {
  /// <summary>
  /// Serves as the base class for classes that dispatch file system events to 
  /// corresponding handlers.
  /// </summary>
  public abstract class FilesysEventDispatcher {
    public IFushareFilesys FushareFilesys { get; private set; }

    public FilesysEventDispatcher(IFushareFilesys fushareFilesys) {
      FushareFilesys = fushareFilesys;
      FushareFilesys.GettingPathStatus += 
        new EventHandler<GetPathStatusEventArgs>(FushareFilesys_GettingPathStatus);
      FushareFilesys.ReadingFile += 
        new EventHandler<ReadFileEventArgs>(FushareFilesys_ReadingFile);
      FushareFilesys.ReleasedFile += 
        new EventHandler<ReleaseFileEventArgs>(FushareFilesys_ReleasedFile);
    }

    #region Dispatching Methods
    void FushareFilesys_ReleasedFile(object sender, ReleaseFileEventArgs e) {
      GetEventHandler(sender as IFushareFilesys, e).HandleReleasedFile(
        sender as IFushareFilesys, e);
    }

    void FushareFilesys_ReadingFile(object sender, ReadFileEventArgs e) {
      GetEventHandler(sender as IFushareFilesys, e).HandleReadingFile(
        sender as IFushareFilesys, e);
    }

    void FushareFilesys_GettingPathStatus(object sender, GetPathStatusEventArgs e) {
      GetEventHandler(sender as IFushareFilesys, e).HandleGettingPathStatus(
        sender as IFushareFilesys, e);
    } 
    #endregion

    #region Abstract Methods
    protected abstract IFilesysEventHandler GetEventHandler(IFushareFilesys sender,
      FushareFilesysEventArgs args); 
    #endregion
  }
}
