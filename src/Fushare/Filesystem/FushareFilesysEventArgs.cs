using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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
    readonly IntPtr _handle;
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

    public IntPtr Handle {
      get { return _handle; }
    } 

    #endregion

    public ReadFileEventArgs(VirtualRawPath virtualRawPath, byte[] buffer, 
      long offset, IntPtr handle) :
      base(virtualRawPath) {
      _buffer = buffer;
      _offset = offset;
      _handle = handle;
    }
  }

  /// <summary>
  /// The event fired when a file is opened.
  /// </summary>
  public class OpenFileEventArgs : FushareFilesysEventArgs {
    readonly IntPtr _handle;
    readonly FileAccess _fileAccess;

    public FileAccess FileAccess {
      get { return _fileAccess; }
    }

    public IntPtr Handle {
      get { return _handle; }
    }

    public OpenFileEventArgs(VirtualRawPath virtualRawPath, IntPtr handle, 
      FileAccess fileAccess) : 
      base(virtualRawPath) {
      _handle = handle;
      _fileAccess = fileAccess;
    }
  }

  /// <summary>
  /// Event fired when file system releases a file after writing.
  /// </summary>
  public class ReleaseFileEventArgs : FushareFilesysEventArgs {
    readonly IntPtr _handle;

    public IntPtr Handle {
      get { return _handle; }
    }

    public ReleaseFileEventArgs(VirtualRawPath virtualRawPath, IntPtr handle) : 
      base(virtualRawPath) {
      _handle = handle;
    }
  }

  /// <summary>
  /// Event fired when file system tries to get the status of a path.
  /// </summary>
  public class GetPathStatusEventArgs : FushareFilesysEventArgs {
    public GetPathStatusEventArgs(VirtualRawPath virtualRawPath) : 
      base(virtualRawPath) { }
  }
}