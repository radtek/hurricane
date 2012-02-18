using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GSeries.Filesystem {
  /// <summary>
  /// The event handler that handles events occurred to the root of the virutal file 
  /// system.
  /// </summary>
  public class NopFilesysEventHandler : IFilesysEventHandler {

    #region IFilesysEventHandler Members

    public void HandleGettingPathStatus(IFilesys sender, GetPathStatusEventArgs args) {
      // Do nothing.
    }

    public void HandleReleasedFile(IFilesys sender, ReleaseFileEventArgs args) {
      // Do nothing.
    }

    public void HandleReadingFile(IFilesys sender, ReadFileEventArgs args) {
      throw new NotImplementedException();
    }

    public void HandleOpeningFile(IFilesys sender, OpenFileEventArgs args) {
      throw new NotImplementedException();
    }

    #endregion
  }
}
