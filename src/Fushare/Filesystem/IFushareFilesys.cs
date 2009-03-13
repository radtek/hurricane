using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Filesystem {
  /// <summary>
  /// Defines the User level file system behavior.
  /// </summary>
  public interface IFushareFilesys {
    event EventHandler<ReleaseFileEventArgs> ReleasedFile;
    event EventHandler<ReadFileEventArgs> ReadingFile;
    event EventHandler<GetPathStatusEventArgs> GettingPathStatus;
    void Start();
    string ShadowDirPath { get; }
  }

  public class FushareFilesysEventArgs : EventArgs {
    /// <summary>
    /// Gets or sets the virtual path.
    /// </summary>
    /// <value>The path.</value>
    public VirtualRawPath VritualPath { get; private set; }

    public FushareFilesysEventArgs(VirtualRawPath virtualPath) {
      VritualPath = virtualPath;
    }
  }

  public class ReadFileEventArgs : FushareFilesysEventArgs {
    public byte[] Buffer { get; set; }
    public long Offset { get; private set; }
    public int BytesWritten { get; set; }

    public ReadFileEventArgs(VirtualRawPath virtualPath, byte[] buffer, long offset) : 
      base(virtualPath) {
      Buffer = buffer;
      Offset = offset;
    }
  }

  public class ReleaseFileEventArgs : FushareFilesysEventArgs {
    public ReleaseFileEventArgs(VirtualRawPath virtualPath) : base(virtualPath) { }
  }

  public class GetPathStatusEventArgs : FushareFilesysEventArgs {
    public GetPathStatusEventArgs(VirtualRawPath virtualPath) : base(virtualPath) { }
  }
}
