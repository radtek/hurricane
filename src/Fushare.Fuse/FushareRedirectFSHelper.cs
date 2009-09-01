using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Unix.Native;
using System.IO;

namespace Fushare.Filesystem {
  /// <summary>
  /// Helper class for the FUSE redirect file system.
  /// </summary>
  /// <remarks>Minimize the logic in the class as it's system dependent.</remarks>
  public class FushareRedirectFSHelper : RedirectFHFSHelper {
    readonly FusharePathFactory _pathFactory;
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FushareRedirectFSHelper));
    readonly FushareFileManager _fileManager;
    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="FushareRedirectFSHelper"/> class.
    /// </summary>
    /// <param name="baseDir">The base dir.</param>
    /// <param name="pathFactory">The path factory.</param>
    public FushareRedirectFSHelper(ShadowDirPath baseDir, FusharePathFactory 
      pathFactory, FushareFileManager fileManager) : base(baseDir.PathString) {
      _pathFactory = pathFactory;
      _fileManager = fileManager;
      // Create the meta direcotry.
      Directory.CreateDirectory(_pathFactory.CreateShadowFullPath(
        new VirtualPath(Path.DirectorySeparatorChar.ToString()),
        FusharePathFactory.FilesysOp.Read).PathString);
    } 
    #endregion

    public override Errno GetPathStatus(string path, out Stat buf) {
      var vrp = new VirtualRawPath(path);
      var ret = base.GetPathStatus(
        _pathFactory.CreateVirtualPath4Read(vrp), out buf);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format("Path Mode: {0}", buf.st_mode));
      if ((buf.st_mode & FilePermissions.S_IFREG) != 0) {
        // Make a change to file size if is a file (S_IFREG)
        buf.st_size = _fileManager.GetFileLength(new VirtualPath(vrp));
      }
      return ret;
    }
    
    /// <summary>
    /// Creates the directory. Both normal and meta directories are created.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="mode">The mode.</param>
    public override Errno CreateDirectory(string path, FilePermissions mode) {
      var result = base.CreateDirectory(path, mode);
      if (result != 0) {
        // First directory creation unsuccessful. No need to create the 2nd one.
        return result;
      } else {
        var metaPath = _pathFactory.CreateVirtualPath(new VirtualRawPath(path),
          FusharePathFactory.FilesysOp.Read);
        result = base.CreateDirectory(metaPath.PathString, mode);
        if (result != 0) {
          Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
            "Error occurred while creating {0}. Removing {1}... (Virtual paths)", 
            metaPath.PathString, path));
          // Error in the 2nd creation. Delete the first one.
          base.RemoveDirectory(path);
        }
        return result;
      }
    }

    /// <summary>
    /// Gets the path extended attribute. Reads the meta directory.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    /// <param name="bytesWritten">The bytes written.</param>
    public override Errno GetPathExtendedAttribute(string path, string name, 
      byte[] value, out int bytesWritten) {
      var vrp = new VirtualRawPath(path);
      return base.GetPathExtendedAttribute(_pathFactory.CreateVirtualPath(
        vrp, FusharePathFactory.FilesysOp.Read).PathString, name, value, 
        out bytesWritten);
    }

    /// <summary>
    /// Opens the handle.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="info">The info.</param>
    /// <returns>
    /// Does nothing but return as we don't use native API to read/write the file.
    /// </returns>
    public override Errno OpenHandle(string path, Mono.Fuse.OpenedPathInfo info) {
      // Do nothing. 
      return 0;
    }

  }
}
