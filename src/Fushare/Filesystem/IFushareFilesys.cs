using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * @namespace Fushare::Filesystem
 * Contains models and utilities that related to Fushare's file system.
 */
namespace Fushare.Filesystem {
  /// <summary>
  /// Defines the user level file system behavior.
  /// </summary>
  public interface IFushareFilesys {
    /// <summary>
    /// Occurs when user released file. Handlers take action on it and possibility send
    /// it to network.
    /// </summary>
    event EventHandler<ReleaseFileEventArgs> ReleasedFile;
    /// <summary>
    /// Occurs when user is reading a file. Handlers store the read data in event args.
    /// </summary>
    event EventHandler<ReadFileEventArgs> ReadingFile;
    /// <summary>
    /// Occurs when user is getting path status. Event handlers ensures virtual files 
    /// be in place.
    /// </summary>
    event EventHandler<GetPathStatusEventArgs> GettingPathStatus;
    /// <summary>
    /// Occurs when the file is being opened.
    /// </summary>
    event EventHandler<OpenFileEventArgs> OpeningFile;
    /// <summary>
    /// Starts this file system daemon.
    /// </summary>
    void Start();
    /// <summary>
    /// Gets the shadow dir path.
    /// </summary>
    /// <value>The shadow dir path.</value>
    string ShadowDirPath { get; }
  }
}
