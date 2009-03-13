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

    public BitTorrentService(BitTorrentManager manager) {
      _manager = manager;
    }

    #region IBitTorrentService Members

    /// <summary>
    /// Gets a file or directory.
    /// </summary>
    /// <param name="nameSpace">namespace of the name</param>
    /// <param name="name">name</param>
    /// <returns>
    /// The web path (part of URL) to the file or directory already downloaded.
    /// </returns>
    public string Get(string nameSpace, string name) {
      byte[] torrentDhtKey = Util.GetDhtKeyBytes(nameSpace, name);
      
      ManualResetEvent waitHandle = new ManualResetEvent(false);
      var downloadPath = 
        _manager.GetPathOfItemInDownloads(nameSpace, name);
      // The file/dir hasn't been downloaded yet. So, don't check path.
      var saveDir = Util.GetParent(downloadPath, false).FullName;
      try {
        _manager.GetData(torrentDhtKey, nameSpace, saveDir,
            waitHandle);
      } catch (TorrentException ex) {
        throw new ResourceNotFoundException("Torrent at this key is invalid.", ex);
      }

      // Wait until downloading finishes
      waitHandle.WaitOne();

      return downloadPath;
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
      byte[] torrentDhtKey = Util.GetDhtKeyBytes(nameSpace, name);
      var savePath = _manager.GetPathOfItemInDownloads(nameSpace, name);
      _manager.PublishData(torrentDhtKey, nameSpace, savePath);
    }

    /// <summary>
    /// Publishes a file or directory.
    /// </summary>
    /// <param name="path">The path to the file/directory to be published.</param>
    /// <remarks>Used when you have a specific path for the
    /// file/directory.</remarks>
    public void Publish(string path) {
      var isInCacheDir = _manager.IsInCacheDir(path);
      if (isInCacheDir) {
        string nameSpace;
        string name;
        _manager.GetNsAndNameOfItemInDownloads(path, out nameSpace,
          out name);
        Publish(nameSpace, name);
      } else {
        var nameSpace = _manager.SelfNameSpace;
        var torrentDhtKey = Util.GetDhtKeyBytes(nameSpace, 
          Util.GetFileOrDirectoryName(path));
        _manager.PublishData(torrentDhtKey, nameSpace, path);
      }
    }

    #endregion
  }
}
