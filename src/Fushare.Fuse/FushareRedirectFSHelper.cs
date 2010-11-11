/*
Copyright (c) 2010 Jiangyan Xu <jiangyan@ufl.edu>, University of Florida

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Unix.Native;
using System.IO;
using Mono.Fuse;
using System.Security.AccessControl;

namespace Fushare.Filesystem {
  /// <summary>
  /// Helper class for the FUSE redirect file system.
  /// </summary>
  /// <remarks>Minimize the logic in the class as it's system dependent.</remarks>
  public class FushareRedirectFSHelper : RedirectFHFSHelper {
    readonly FusharePathFactory _pathFactory;
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FushareRedirectFSHelper));
    readonly FushareFileManager _fileManager;
    readonly FilesysContext _filesysContext;
    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="FushareRedirectFSHelper"/> class.
    /// </summary>
    /// <param name="baseDir">The base dir.</param>
    /// <param name="pathFactory">The path factory.</param>
    public FushareRedirectFSHelper(ShadowDirPath baseDir, FusharePathFactory 
      pathFactory, FushareFileManager fileManager, FilesysContext filesysContext) : base(baseDir.PathString) {
      _pathFactory = pathFactory;
      _fileManager = fileManager;
      _filesysContext = filesysContext;
      // Create the meta direcotry.
      Directory.CreateDirectory(_pathFactory.CreateShadowFullPath(
        new VirtualPath(Path.DirectorySeparatorChar.ToString()),
        FusharePathFactory.FilesysOp.Read).PathString);
    } 
    #endregion

    public override Errno GetPathStatus(string path, out Stat buf) {
      // Read the meta folder.
      var vrp = new VirtualRawPath(path);
      var ret = base.GetPathStatus(
        _pathFactory.CreateVirtualPath4Read(vrp), out buf);

      if ((buf.st_mode & FilePermissions.S_IFREG) != 0) {
        // Make a change to file size if is a file (S_IFREG)
        buf.st_size = _fileManager.GetFileLength(new VirtualPath(vrp));
      }
      return ret;
    }

    /// <summary>
    /// Gets the handle status. This method gets the status of the virtual file
    /// and replaces the size with that of the real data file.
    /// </summary>
    public override Errno GetHandleStatus(string path, OpenedPathInfo info, 
      out Stat buf) {
      Errno ret;
      ret = base.GetHandleStatus(path, info, out buf);
      if ((buf.st_mode & FilePermissions.S_IFREG) != 0) {
        // Make a change to file size if is a file (S_IFREG)
        buf.st_size = _fileManager.GetFileLength(
          VirtualPath.CreateFromRawString(path));
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
    public override Errno OpenHandle(string path, OpenedPathInfo info) {
      Errno retVal;
      if (info.OpenAccess == OpenFlags.O_RDONLY) {
        String readPath = _pathFactory.CreateVirtualPath4Read(new VirtualRawPath(path));
        retVal = base.OpenHandle(readPath, info);
      } else {
        // This is a write.
        // @TODO Write to an existing file is not supported by BitTorrent.
        // It has to be enforced somewhere.
        String writePath = _pathFactory.CreateVirtualPath4Write(new VirtualRawPath(path));
        retVal = base.OpenHandle(writePath, info);
      }

      // Add to filesys context.
      VirtualPath vp = VirtualPath.CreateFromRawString(path);
      VirtualFile vf = _fileManager.ReadVirtualFile(vp);
      FileAccess fa = IOUtil.OpenFlags2FileAccess(info.OpenAccess);
      var openFileInfo = new OpenFileInfo(info.Handle, vp, vf, fa);
      _filesysContext.AddOpenFile(info.Handle, openFileInfo);

      return retVal;
    }

    /// <summary>
    /// Creates the handle.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="info">The info.</param>
    /// <param name="mode">The mode.</param>
    /// <returns></returns>
    /// <remarks>This method is for FilesysOp.Write.</remarks>
    public override Errno CreateHandle(string path, OpenedPathInfo info, FilePermissions mode) {
      // Create the handle for the write.
      String writePath = 
        _pathFactory.CreateVirtualPath4Write(new VirtualRawPath(path));
      Errno retVal = base.CreateHandle(writePath, info, mode);

      // Create the VF so the reads afterwards knows about the file.
      VirtualPath vp = VirtualPath.CreateFromRawString(path);
      VirtualFile vf = CreateAndWriteVirtualFile(vp);
      var ofi = new OpenFileInfo(info.Handle, vp, vf, FileAccess.Write);
      _filesysContext.AddOpenFile(info.Handle, ofi);
      return retVal;
    }

    public override unsafe Errno WriteHandle(string path, OpenedPathInfo info, 
      byte[] buf, long offset, out int bytesWritten) {
      try {
        // "path" is not used in base.WriteHandle
        return base.WriteHandle(path, info, buf, offset, out bytesWritten);
      } catch (Exception ex) {
        Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
          "Exception caught when writing to file. {0}", ex));
        throw;
      }
    }

    public override Errno ReleaseHandle(string path, OpenedPathInfo info) {
      _filesysContext.RemoveOpenFile(info.Handle);
      VirtualPath vp = VirtualPath.CreateFromRawString(path);
      UpdateFileSizeInVirtualFile(vp);
      return base.ReleaseHandle(path, info);
    }

    public override Errno AccessPath(string path, AccessModes mask) {
      String readPath = _pathFactory.CreateVirtualPath4Read(new VirtualRawPath(path));
      return base.AccessPath(readPath, mask);
    }

    /// <summary>
    /// Creates a virtual file with the Physical location information in it.
    /// </summary>
    /// <param name="vp">The vp.</param>
    VirtualFile CreateAndWriteVirtualFile(VirtualPath vp) {
      ShadowFullPath readPath = _pathFactory.CreateShadowFullPath4Read(vp);
      ShadowFullPath writePath = _pathFactory.CreateShadwoFullPath4Write(vp);
      var virtualFile = new VirtualFile() {
        PhysicalUri = new Uri(writePath.PathString)
      };
      IOUtil.PrepareParentDirForPath(readPath.PathString);
      XmlUtil.WriteXml<VirtualFile>(virtualFile, readPath.PathString);

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("A virtual file is created at {0}", readPath.PathString));
      return virtualFile;
    }

    /// <summary>
    /// Updates the file size in virtual file.
    /// </summary>
    void UpdateFileSizeInVirtualFile(VirtualPath vp) {
      ShadowFullPath readPath = _pathFactory.CreateShadowFullPath4Read(vp);
      ShadowFullPath writePath = _pathFactory.CreateShadwoFullPath4Write(vp);
      var virtualFile = XmlUtil.ReadXml<VirtualFile>(readPath.PathString);
      // Get the size from the real file.
      long fileSize = new FileInfo(writePath.PathString).Length;
      virtualFile.FileSize = fileSize;
      XmlUtil.WriteXml<VirtualFile>(virtualFile, readPath.PathString);
    }
  }
}
