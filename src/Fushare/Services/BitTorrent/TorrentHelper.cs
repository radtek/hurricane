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
    public string TorrentsDirPath { get; private set; }

    /// <summary>
    /// The URL prefix that clients should send requests to.
    /// </summary>
    /// <value>The tracker URL prefix.</value>
    public string TrackerUrlPrefix { get; private set; }

    public TorrentHelper(string torrentsDirPath, string trackrUrlPrefix) {
      TorrentsDirPath = torrentsDirPath;
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
      var pathToWrite = GetTorrentFilePath(nameSpace, torrentName);
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
      var torrentPath = GetTorrentFilePath(nameSpace, name);
      if (File.Exists(torrentPath)) {
        return File.ReadAllBytes(torrentPath);
      } else {
        var torrentKey = ServiceUtil.GetDhtKeyBytes(nameSpace, name);
        byte[] torrentBytes = proxy.GetTorrent(torrentKey);
        File.WriteAllBytes(torrentPath, torrentBytes);
        return torrentBytes;
      }
    }

  }
}
