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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MonoTorrent.BEncoding;

namespace GatorShare.Services.BitTorrent {
  /// <summary>
  /// Represents the cache directory structure for BitTorrent and provides 
  /// </summary>
  public class BitTorrentCache {
    #region Fields
    public const string DownloadsDirName = "Downloads";
    public const string TorrentsDirName = "Torrents";
    public const string FastResumeFileName = "fastresume.data";
    public const string CacheRegistryFileName = "cacheRegistry.xml";
    readonly string _baseDirPath; 
    #endregion

    #region Properties
    /// <summary>
    /// The full path of the base directory assigned to this manager.
    /// </summary>
    /// <value></value>
    public string BaseDirPath {
      get { return _baseDirPath; }
    }

    public string DownloadsDirPath {
      get {
        return Path.Combine(BaseDirPath, DownloadsDirName);
      }
    }

    public string TorrentsDirPath {
      get {
        return GetTorrentsDirPath(BaseDirPath);
      }
    }

    public string FastResumeFilePath {
      get {
        return Path.Combine(BaseDirPath, FastResumeFileName);
      }
    }

    public string CacheRegistryFilePath {
      get {
        return Path.Combine(BaseDirPath, CacheRegistryFileName);
      }
    } 
    #endregion

    public BitTorrentCache(string baseDirPath) {
      IOUtil.CheckPathRooted(baseDirPath, "baseDirPath");
      _baseDirPath = baseDirPath;

      // Prepare directories
      if (!Directory.Exists(DownloadsDirPath))
        Directory.CreateDirectory(DownloadsDirPath);
      if (!Directory.Exists(TorrentsDirPath))
        Directory.CreateDirectory(TorrentsDirPath);

    }

    internal static string GetTorrentsDirPath(string baseDirPath) {
      return Path.Combine(baseDirPath, TorrentsDirName);
    }

    /// <summary>
    /// Gets the path of item an in downloads directory.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <returns>Path of the item already downloaded or to be downloaded.
    /// </returns>
    /// <remarks>It doesn't have to exist.</remarks>
    internal string GetPathOfItemInDownloads(string nameSpace, string name) {
      return Path.Combine(DownloadsDirPath, Path.Combine(nameSpace, name));
    }

    /// <summary>
    /// Gets the path of torrent file.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public string GetTorrentFilePath(string nameSpace, string torrentName) {
      // Torrent files have this .torrrent suffix.
      return Path.Combine(TorrentsDirPath,
        Path.Combine(nameSpace, torrentName + ".torrent"));
    }
  }
}
