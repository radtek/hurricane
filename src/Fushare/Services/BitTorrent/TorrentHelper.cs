using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTorrent.Common;
using MonoTorrent.BEncoding;
using System.IO;

namespace Fushare.Services.BitTorrent {
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
    /// <param name="dataPath">The path to the file/directory.</param>
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

    private string GetTorrentPath(string nameSpace, string torrentName) {
      return Path.Combine(TorrentsDirPath,  
        Path.Combine(nameSpace, torrentName + ".torrent"));
    }

    public string WriteTorrent(byte[] torrentBytes, string nameSpace, 
      string torrentName) {
      var pathToWrite = GetTorrentPath(nameSpace, torrentName);
      Directory.CreateDirectory(Util.GetParent(pathToWrite, false).FullName);
      using (FileStream stream = new FileStream(pathToWrite, 
        FileMode.Create)) {
        stream.Write(torrentBytes, 0, torrentBytes.Length);
      }
      return pathToWrite;
    }

  }
}
