using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

namespace Fushare.Filesystem {
  /// <summary>
  /// Defines methods related to IO in Fushare file system.
  /// </summary>
  public class FushareFileManager {
    #region Fields
    readonly FusharePathFactory _pathFactory;
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FushareFileManager)); 
    #endregion

    public FushareFileManager(FusharePathFactory pathFactory) {
      _pathFactory = pathFactory;
    }

    /// <summary>
    /// Reads from the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <returns>The number of bytes read.</returns>
    public int Read(VirtualPath path, byte[] buffer, long offset) {
      var shadowPath = _pathFactory.CreateShadowFullPath4Read(path);
      if (File.Exists(shadowPath.PathString)) {
        var vf = XmlUtil.ReadXml<VirtualFile>(shadowPath.PathString);
        return Read(vf, buffer, offset);
      } else {
        // Get it from server.
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Reads data given the specified virtual file.
    /// </summary>
    /// <param name="virtualFile">The virtual file.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <returns>The number of bytes read.</returns>
    public int Read(VirtualFile virtualFile, byte[] buffer, long offset) {
      var fileUri = virtualFile.PhysicalUri;
      if (fileUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase)) {
        // Local disk
        var filePath = fileUri.LocalPath;
        Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
          "Virtual file points to local path {0}. Reading it...", filePath));
        var actualRead = IOUtil.Read(filePath, buffer, offset, buffer.Length);
        return actualRead;
      } else {
        // Other types of services.
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Gets the length of the physical file which is read from the virtual file.
    /// </summary>
    /// <param name="virtualPath">The virtual path to the virtual file.</param>
    /// <returns>The file size.</returns>
    public long GetFileLength(VirtualPath virtualPath) {
      var shadowPath = _pathFactory.CreateShadowFullPath4Read(virtualPath);
      if (File.Exists(shadowPath.PathString)) {
        var vf = XmlUtil.ReadXml<VirtualFile>(shadowPath.PathString);
        return vf.FileSize;
      } else {
        throw new ArgumentException("Virtual file not exists.");
      }
    }
  }
}
