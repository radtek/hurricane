using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Filesystem {
  /// <summary>
  /// Defines the interface that Fushare implements to handle file system events.
  /// </summary>
  public interface IFilesysEventHandler {
    /// <summary>
    /// Handles the getting path status event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The 
    /// <see cref="Fushare.Filesystem.GetPathStatusEventArgs"/> 
    /// instance containing the event data.</param>
    void HandleGettingPathStatus(IFushareFilesys sender, 
      GetPathStatusEventArgs args);
    /// <summary>
    /// Handles the released file event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The 
    /// <see cref="Fushare.Filesystem.ReleaseFileEventArgs"/> instance containing 
    /// the event data.</param>
    void HandleReleasedFile(IFushareFilesys sender, ReleaseFileEventArgs args);
    /// <summary>
    /// Handles the reading file event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The 
    /// <see cref="Fushare.Filesystem.ReadFileEventArgs"/> instance containing the 
    /// event data.</param>
    void HandleReadingFile(IFushareFilesys sender, ReadFileEventArgs args);
  }
}
