using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MonoTorrent.Common;
using System.Web.Configuration;

namespace Fushare.Services.BitTorrent {
  public class BitTorrentService : IBitTorrentService {

    BitTorrentManager _manager;
    PieceLevelTorrentManager _pieceLevelManager;
    CacheRegistry _cacheRegistry;

    #region Contructors
    
    public BitTorrentService(BitTorrentManager manager, 
      PieceLevelTorrentManager pieceLevelManager) {
      _manager = manager;
      _pieceLevelManager = pieceLevelManager;
      _cacheRegistry = _manager.CacheRegistry;
    }

    #endregion

    public void Start() {
      _manager.Start();
      _pieceLevelManager.Start();
    }

    #region IBitTorrentService Members

    

    /// <summary>
    /// Gets a file or directory.
    /// </summary>
    /// <param name="nameSpace">namespace of the name</param>
    /// <param name="name">name</param>
    /// <returns>
    /// The meta info of the file(s) downloaded.
    /// </returns>
    public DataMetaInfo Get(string nameSpace, string name) {
      string downloadPath;
      var torrentBytes = _manager.GetData(nameSpace, name, out downloadPath);
      return MakeDataMetaInfo(downloadPath, torrentBytes, false);
    }

    /// <summary>
    /// Gets part of a file downloaded via BitTorrent.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="bytesToRead">The number of bytes to read.</param>
    /// <returns></returns>
    public byte[] Get(string nameSpace, string name, long offset, int bytesToRead) {
      var ret = _pieceLevelManager.GetDataBlock(
        nameSpace, name, offset, bytesToRead);
      return ret;
    }

    public void Get(string nameSpace, string name, string saveDirPath) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Publishes a file or directory.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <remarks>Uses the name of the directory of file as the publishing name.
    /// Used when you want to publish file/directory already in the Cache folder.
    /// </remarks>
    public void Publish(string nameSpace, string name) {
      Publish(nameSpace, name, true);
    }

    /// <summary>
    /// Updates a file or directory by the specified path.
    /// </summary>
    /// <param name="path">The path to the file/directory to be published.</param>
    /// <remarks>Used when you have a specific path for the
    /// file/directory.</remarks>
    public void Publish(string path) {
      Publish(path, true);
    }

    /// <summary>
    /// Same as Publish except that you don't get an exception when the key is
    /// duplicated. You overwrite the existing key.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    public void Update(string nameSpace, string name) {
      Publish(nameSpace, name, false);
    }

    /// <summary>
    /// Same as Publish except that you don't get an exception when the key is
    /// duplicated. You overwrite the existing key.
    /// </summary>
    /// <param name="path">The path to the file/directory to be published.</param>
    public void Update(string path) {
      Publish(path, false);
    }

    /// <summary>
    /// Checks if the specified file exists without downloading it.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <returns>The meta information about the file.</returns>
    public DataMetaInfo Peek(string nameSpace, string name) {
      string downloadPath;
      byte[] torrentBytes = _manager.PeekData(nameSpace, name, out downloadPath);
      return MakeDataMetaInfo(downloadPath, torrentBytes, true);
    }

    #endregion

    #region Internal/Private Methods
    /// <summary>
    /// Publishes a file or directory.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <remarks>Uses the name of the directory of file as the publishing name.
    /// Used when you want to publish file/directory already in the Cache folder.
    /// </remarks>
    void Publish(string nameSpace, string name, bool unique) {
      if (unique) {
        _manager.PublishData(nameSpace, name);
      } else {
        _manager.UpdateData(nameSpace, name);
      }
    }

    /// <summary>
    /// Publishes a file or directory.
    /// </summary>
    /// <param name="path">The path to the file/directory to be published.</param>
    /// <remarks>Used when you have a specific path for the
    /// file/directory.</remarks>
    void Publish(string path, bool unique) {
      var isInCacheDir = IsInCacheDir(path);
      if (isInCacheDir) {
        string nameSpace;
        string name;
        GetNsAndNameOfItemInDownloads(path, out nameSpace,
          out name);
        Publish(nameSpace, name, unique);
      } else {
        // If not in cache dir, use the namespace of the manager.
        var nameSpace = _manager.SelfNameSpace;
        var name = IOUtil.GetFileOrDirectoryName(path);
        if (unique) {
          _manager.PublishData(nameSpace, name);
        } else {
          _manager.UpdateData(nameSpace, name);
        }
      }
    }

    void GetNsAndNameOfItemInDownloads(string path, out string nameSpace, 
      out string name) {
      IOUtil.CheckPathRooted(path);
      if (!_cacheRegistry.IsInCacheDir(path, true)) {
        throw new ArgumentException("Path not in Cache(Downloads) directory.");
      }
      name = IOUtil.GetFileOrDirectoryName(path, true);
      nameSpace = IOUtil.GetParent(path, true).Name;
    }

    private static DataMetaInfo MakeDataMetaInfo(string downloadPath,
      byte[] torrentBytes, bool onDemand) {
      DataMetaInfo ret = new DataMetaInfo();
      ret.DataUri = new Uri(downloadPath);
      ret.TorrentBytes = torrentBytes;
      ret.OnDemand = onDemand;
      // @TODO: For now we keep the "Files" field but with torrent bytes included,
      // it's redundant.
      Torrent torrent = Torrent.Load(torrentBytes);
      if (torrent.Files.Length > 1) {
        // In a single file case, MonoTorrent adds this single file to the list but in 
        // BitTorrent protocol, the files key shouldn't present, so the list should be 
        // empty.
        foreach (var torrentFile in torrent.Files) {
          ret.Files.Add(new Uri(torrentFile.Path, UriKind.Relative));
        }
      }
      return ret;
    }

    /// <summary>
    /// Determines whether the specified path is in cache dir.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>
    /// 	<c>true</c> if the specified path is in cache dir; otherwise, <c>false</c>.
    /// </returns>
    bool IsInCacheDir(string path) {
      // There is no reason here that we don't check the path.
      return _cacheRegistry.IsInCacheDir(path, true);
    }

    /// <summary>
    /// Gets the service info.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns></returns>
    public BitTorrentServiceInfo GetServiceInfo(string filter) {
      // Ignore filter for now.
      var ub = new UriBuilder();
      ub.Scheme = "file";
      ub.Path = Path.Combine(WebConfigurationManager.AppSettings[
        "BitTorrentManagerBaseDirPath"], BitTorrentManager.DownloadsDirName);
      return new BitTorrentServiceInfo () {
        ServerCacheUri = ub.Uri
      };
    }

    #endregion
  }
}
