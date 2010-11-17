using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GatorShare.Filesystem {
  /// <summary>
  /// Defines the interface that GatorShare implements to handle file system events.
  /// </summary>
  public interface IFilesysEventHandler {
    /// <summary>
    /// Handles the getting path status event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The 
    /// <see cref="GatorShare.Filesystem.GetPathStatusEventArgs"/> 
    /// instance containing the event data.</param>
    void HandleGettingPathStatus(IFilesys sender, 
      GetPathStatusEventArgs args);
    /// <summary>
    /// Handles the released file event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The 
    /// <see cref="GatorShare.Filesystem.ReleaseFileEventArgs"/> instance containing 
    /// the event data.</param>
    void HandleReleasedFile(IFilesys sender, ReleaseFileEventArgs args);
    /// <summary>
    /// Handles the reading file event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The 
    /// <see cref="GatorShare.Filesystem.ReadFileEventArgs"/> instance containing the 
    /// event data.</param>
    void HandleReadingFile(IFilesys sender, ReadFileEventArgs args);

    /// <summary>
    /// Handles the opening file event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The <see cref="GatorShare.Filesystem.OpenFileEventArgs"/> 
    /// instance containing the event data.</param>
    void HandleOpeningFile(IFilesys sender, OpenFileEventArgs args);
  }
}
