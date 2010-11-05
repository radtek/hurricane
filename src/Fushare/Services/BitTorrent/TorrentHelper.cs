using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTorrent.Common;
using MonoTorrent.BEncoding;
using System.IO;

namespace Fushare.Services.BitTorrent {
  /// <summary>
  /// Helper class that provides torrent related operations.
  /// </summary>
  public class TorrentHelper {
    readonly BitTorrentCache _bittorrentCache;

    /// <summary>
    /// The URL prefix that clients should send requests to.
    /// </summary>
    /// <value>The tracker URL prefix.</value>
    public string TrackerUrlPrefix { get; private set; }

    public TorrentHelper(BitTorrentCache bittorrentCache, string trackrUrlPrefix) {
      _bittorrentCache = bittorrentCache;
      TrackerUrlPrefix = trackrUrlPrefix;
    }

    /// <summary>
    /// Creates torrent from the file specified by the given path.
    /// </summary>
    /// <param name="path">The path to the file/directory.</param>
    /// <param name="trackerUrl">The tracker URL.</param>
    /// <returns>The torrent</returns>
    public BEncodedDictionary CreateTorrent(string dataPath) {
      TorrentCreator creator = new TorrentCreator();
      creator.Comment = "Comment";
      creator.CreatedBy = "Fushare";
      creator.Path = dataPath;
      creator.Announces.Add(new List<string>());
      creator.Announces[0].Add(TrackerUrlPrefix);
      return creator.Create();
    }

    /// <summary>
    /// Writes the torrent file.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="torrentName">Name of the torrent.</param>
    /// <param name="torrentBytes">The torrent bytes.</param>
    /// <returns>The full path written.</returns>
    public string WriteTorrentFile(string nameSpace, string torrentName, 
      byte[] torrentBytes) {
      var pathToWrite = _bittorrentCache.GetTorrentFilePath(nameSpace, torrentName);
      WriteTorrent(torrentBytes, pathToWrite);
      return pathToWrite;
    }

    /// <summary>
    /// Writes the torrent and more importantly, creates preceding directories first.
    /// </summary>
    /// <param name="torrentBytes">The torrent bytes.</param>
    /// <param name="pathToWrite">The path to write.</param>
    /// <returns></returns>
    public static void WriteTorrent(byte[] torrentBytes, string pathToWrite) {
      IOUtil.WriteAllBytes(pathToWrite, torrentBytes);
    }

    /// <summary>
    /// Reads torrent from file system if exists or download it from DHT and writes it 
    /// to file system.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="proxy">The proxy.</param>
    /// <returns>Torrent bytes.</returns>
    public byte[] ReadOrDownloadTorrent(string nameSpace, string name, 
      DhtProxy proxy) {
      var torrentPath = _bittorrentCache.GetTorrentFilePath(nameSpace, name);
      if (File.Exists(torrentPath)) {
        return File.ReadAllBytes(torrentPath);
      } else {
        var torrentKey = ServiceUtil.GetDhtKeyBytes(nameSpace, name);
        byte[] torrentBytes = proxy.GetTorrent(torrentKey);
        if (torrentBytes == null) {
          throw new ResourceException();
        }
        IOUtil.PrepareParentDirForPath(torrentPath);
        File.WriteAllBytes(torrentPath, torrentBytes);
        return torrentBytes;
      }
    }

  }
}
