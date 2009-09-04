﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using Fushare.Services.BitTorrent;
using Mono.Unix;

namespace Fushare.Filesystem {
  /// <summary>
  /// Defines methods related to IO in Fushare file system.
  /// </summary>
  public class FushareFileManager {
    #region Fields
    readonly FusharePathFactory _pathFactory;
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FushareFileManager)); 
    readonly ServerProxy _serverProxy;
    #endregion

    public FushareFileManager(FusharePathFactory pathFactory, 
      ServerProxy serverProxy) {
      _pathFactory = pathFactory;
      _serverProxy = serverProxy;
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
        if (SysEnvironment.OSVersion == OS.Unix) {
          var symlink = new UnixSymbolicLinkInfo(toFullPath);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "Creating Symlink from {0} to {1}", toFullPath, fromFullPath.PathString));
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
