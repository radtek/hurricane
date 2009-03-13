using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Filesystem {
  /// <summary>
  /// The event handler that handles events occurred to the root of the virutal file 
  /// system.
  /// </summary>
  public class NopFilesysEventHandler : IFilesysEventHandler {

    #region IFilesysEventHandler Members

    public void HandleGettingPathStatus(IFushareFilesys sender, GetPathStatusEventArgs args) {
      // Do nothing.
    }

    public void HandleReleasedFile(IFushareFilesys sender, ReleaseFileEventArgs args) {
      throw new NotImplementedException();
    }

    public void HandleReadingFile(IFushareFilesys sender, ReadFileEventArgs args) {
      throw new NotImplementedException();
    }

    #endregion
  }
}
