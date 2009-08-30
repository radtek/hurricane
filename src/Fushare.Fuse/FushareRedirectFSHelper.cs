using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Unix.Native;
using System.IO;

namespace Fushare.Filesystem {
  public class FushareRedirectFSHelper : RedirectFHFSHelper {
    readonly FusharePathFactory _pathFactory;
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FushareRedirectFSHelper));

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="FushareRedirectFSHelper"/> class.
    /// </summary>
    /// <param name="baseDir">The base dir.</param>
    /// <param name="pathFactory">The path factory.</param>
    public FushareRedirectFSHelper(ShadowDirPath baseDir, FusharePathFactory pathFactory) :
      base(baseDir.PathString) {
      _pathFactory = pathFactory;

      // Create the meta direcotry.
      Directory.CreateDirectory(_pathFactory.CreateShadowFullPath(
        new VirtualPath(Path.DirectorySeparatorChar.ToString()),
        FusharePathFactory.FilesysOp.Read).PathString);
    } 
    #endregion

    public override Errno GetPathStatus(string path, out Stat buf) {
      var vrp = new VirtualRawPath(path);
      return base.GetPathStatus(_pathFactory.CreateVirtualPath(
        vrp, FusharePathFactory.FilesysOp.Read).PathString, out buf);
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

    public override Errno OpenHandle(string path, Mono.Fuse.OpenedPathInfo info) {
      return base.OpenHandle(_pathFactory.CreateVirtualPathForRead(
        new VirtualRawPath(path)), info);
    }

  }
}
