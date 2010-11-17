using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using GatorShare.Services.BitTorrent;
using Mono.Unix;

namespace GatorShare.Filesystem {
  /// <summary>
  /// Defines methods related to IO in the file system.
  /// </summary>
  public class FilesysManager {
    #region Fields
    readonly PathFactory _pathFactory;
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FilesysManager)); 
    readonly ServerProxy _serverProxy;
    #endregion

    public FilesysManager(PathFactory pathFactory, 
      ServerProxy serverProxy) {
      _pathFactory = pathFactory;
      _serverProxy = serverProxy;
    }

    /// <summary>
    /// Reads the virtual file  from the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public VirtualFile ReadVirtualFile(VirtualPath path) {
      var shadowPath = _pathFactory.CreateShadowFullPath4Read(path);
      if (File.Exists(shadowPath.PathString)) {
        var vf = XmlUtil.ReadXml<VirtualFile>(shadowPath.PathString);
        return vf;
      } else {
        // This is an erroneous situation where you think the file exists but no virtual file 
        // presents.
        throw new Exception();
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
    /// Copies the file at the given path to the server, which could be anywhere from
    /// the local machine to a remote one.
    /// </summary>
    /// <param name="virtualPath">The virtual path.</param>
    public void CopyToServer(VirtualPath virtualPath) {
      var fromFullPath = _pathFactory.CreateShadwoFullPath4Write(virtualPath);
      byte[] infoBytes = _serverProxy.Get(new Uri("/BitTorrent/Info"));
      var infoObj = XmlUtil.FromXml<BitTorrentServiceInfo>(
        Encoding.UTF8.GetString(infoBytes));
      if (infoObj.ServerCacheUri.IsLoopback) {
        // Server on the same machine, we copy from file system.
        var relativePath = virtualPath.PathString.Substring(
          virtualPath.PathString.IndexOf(Path.DirectorySeparatorChar, 1));
        var toFullPath = UriUtil.CombinePaths(infoObj.ServerCacheUri.LocalPath,
          new Uri(relativePath, UriKind.Relative));
        IOUtil.PrepareParentDirForPath(toFullPath);
        if (SysEnvironment.OSVersion == OS.Unix) {
          // In case of Unix, we actually use symbolic link instead of copying.
          var symlink = new UnixSymbolicLinkInfo(toFullPath);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "Creating Symlink: {0} -> {1}", toFullPath, fromFullPath.PathString));
          // Linking toPath to fromPath == Copy fromPath to toPath.
          symlink.CreateSymbolicLinkTo(fromFullPath.PathString);
        } else {
          throw new NotImplementedException("Only Unix hosts are currently supported.");
        }
      } else {
        throw new NotImplementedException("Only local machine is currently supported.");
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
