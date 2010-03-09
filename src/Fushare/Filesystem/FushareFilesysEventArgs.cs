using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Filesystem {
  /// <summary>
  /// Base class for events in Fushare file system.
  /// </summary>
  public class FushareFilesysEventArgs : EventArgs {
    readonly VirtualRawPath _virtualRawPath;

    /// <summary>
    /// Gets or sets the virtual path.
    /// </summary>
    /// <value>The path.</value>
    public VirtualRawPath VritualRawPath {
      get { return _virtualRawPath; }
    }

    public FushareFilesysEventArgs(VirtualRawPath virtualRawPath) {
      _virtualRawPath = virtualRawPath;
    }
  }

  /// <summary>
  /// Event fired when file system reads a file.
  /// </summary>
  public class ReadFileEventArgs : FushareFilesysEventArgs {
    #region Fields
    readonly long _offset;
    readonly byte[] _buffer;
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the buffer that stores content read from file.
    /// </summary>
    /// <value>The buffer.</value>
    public byte[] Buffer { 
      get { return _buffer; }
    }

    /// <summary>
    /// Gets the offset in file at which the read starts.
    /// </summary>
    /// <value>The offset.</value>
    public long Offset { get { return _offset; } }
    /// <summary>
    /// Gets or sets the bytes read from the file.
    /// </summary>
    /// <value>The bytes read.</value>
    public int BytesRead { get; set; } 
    #endregion

    public ReadFileEventArgs(VirtualRawPath virtualRawPath, byte[] buffer, long offset) :
      base(virtualRawPath) {
      _buffer = buffer;
      _offset = offset;
    }
  }

  /// <summary>
  /// Event fired when file system releases a file after writing.
  /// </summary>
  public class ReleaseFileEventArgs : FushareFilesysEventArgs {
    public ReleaseFileEventArgs(VirtualRawPath virtualRawPath) : 
      base(virtualRawPath) { }
  }

  /// <summary>
  /// Event fired when file system tries to get the status of a path.
  /// </summary>
  public class GetPathStatusEventArgs : FushareFilesysEventArgs {
    public GetPathStatusEventArgs(VirtualRawPath virtualRawPath) : 
      base(virtualRawPath) { }
  }
}