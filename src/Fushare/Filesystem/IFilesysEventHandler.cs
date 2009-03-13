using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Filesystem {
  /// <summary>
  /// Defines the interface that Fushare implements to handle file system events.
  /// </summary>
  public interface IFilesysEventHandler {
    void HandleGettingPathStatus(IFushareFilesys sender, 
      GetPathStatusEventArgs args);
    void HandleReleasedFile(IFushareFilesys sender, ReleaseFileEventArgs args);
    void HandleReadingFile(IFushareFilesys sender, ReadFileEventArgs args);
  }
}
