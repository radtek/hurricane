using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MonoTorrent.Common;

namespace Fushare.Services.BitTorrent {
  public class BitTorrentService : IBitTorrentService {

    BitTorrentManager _manager;

    #region Contructors
    public BitTorrentService(BitTorrentManager manager) {
      _manager = manager;
    } 
    #endregion

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
      byte[] torrentDhtKey = ServiceUtil.GetDhtKeyBytes(nameSpace, name);
      
      ManualResetEvent waitHandle = new ManualResetEvent(false);
      var downloadPath =
        _manager.GetPathOfItemInDownloads(nameSpace, name);
      // @TODO Check integrity of the data -- How do we know the download it complete?
      // A possbile solution. Use the name <name.part> and change it to <name> after
      // download completes.
      Torrent torrent;
      // @TODO "Digging into collaborator", bad practice, I know.
      if (!_manager.CacheRegistry.IsInCacheRegistry(nameSpace, name)) {
        try {
          torrent = _manager.GetData(torrentDhtKey, nameSpace, downloadPath,
              waitHandle);
        } catch (TorrentException ex) {
          throw new ResourceNotFoundException(string.Format(
            "Torrent at key {0} (UrlBase64) is invalid.",
            UrlBase64.Encode(torrentDhtKey)), ex);
        }

        // Wait until downloading finishes
        waitHandle.WaitOne();

      } else {
        // If the data is already there, we don't need to download it again.
        // We already have the data, so we must already have the torrent.
        torrent = Torrent.Load(_manager.GetPathOfTorrentFile(nameSpace, name));
      }
      return MakeDataMetaInfo(downloadPath, torrent);
    }

    public void Get(string nameSpace, string name, string saveDirPath) {
      throw new NotImplementedException();
    }

    public void Publish(string nameSpace, string name) {
      Publish(nameSpace, name, true);
    }

    public void Publish(string path) {
      Publish(path, true);
    }

    public void Update(string nameSpace, string name) {
      Publish(nameSpace, name, false);
    }

    public void Update(string path) {
      Publish(path, false);
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
      byte[] torrentDhtKey = ServiceUtil.GetDhtKeyBytes(nameSpace, name);
      var savePath = _manager.GetPathOfItemInDownloads(nameSpace, name);
      if (unique) {
        _manager.PublishData(torrentDhtKey, nameSpace, savePath);
      } else {
        _manager.UpdateData(torrentDhtKey, nameSpace, savePath);
      }
    }

    /// <summary>
    /// Publishes a file or directory.
    /// </summary>
    /// <param name="path">The path to the file/directory to be published.</param>
    /// <remarks>Used when you have a specific path for the
    /// file/directory.</remarks>
    void Publish(string path, bool unique) {
      var isInCacheDir = _manager.IsInCacheDir(path);
      if (isInCacheDir) {
        string nameSpace;
        string name;
        _manager.GetNsAndNameOfItemInDownloads(path, out nameSpace,
          out name);
        Publish(nameSpace, name, unique);
      } else {
        var nameSpace = _manager.SelfNameSpace;
        var torrentDhtKey = ServiceUtil.GetDhtKeyBytes(nameSpace,
          IOUtil.GetFileOrDirectoryName(path));
        if (unique) {
          _manager.PublishData(torrentDhtKey, nameSpace, path);
        } else {
          _manager.UpdateData(torrentDhtKey, nameSpace, path);
        }
      }
    }

    private static DataMetaInfo MakeDataMetaInfo(string downloadPath,
      Torrent torrent) {
      DataMetaInfo ret = new DataMetaInfo();
      ret.DataUri = new Uri(downloadPath);
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
    #endregion
  }
}
