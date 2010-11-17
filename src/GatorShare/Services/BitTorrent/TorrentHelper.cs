using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTorrent.Common;
using MonoTorrent.BEncoding;
using System.IO;
using System.Net;
using System.Collections;
using GatorShare.External;
using GatorShare.External.DictionaryService;

namespace GatorShare.Services.BitTorrent {
  /// <summary>
  /// Helper class that provides torrent related operations.
  /// </summary>
  public class TorrentHelper {
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(TorrentHelper));
    readonly BitTorrentCache _bittorrentCache;
    readonly IPAddress _hostIP;
    readonly int _trackerPort;
    readonly int _gsserverPort;

    /// <summary>
    /// The URL prefix that clients should send requests to.
    /// </summary>
    /// <value>The tracker URL prefix.</value>
    string TrackerUrlPrefix {
      get { 
        return string.Format("http://{0}:{1}/", _hostIP.ToString(), _trackerPort); 
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TorrentHelper"/> class.
    /// </summary>
    /// <param name="bittorrentCache">The bittorrent cache.</param>
    /// <param name="hostIP">The host IP.</param>
    /// <param name="trackerPort">The tracker port.</param>
    /// <param name="gsserverPort">The gsserver port. It is used to register the torrent web server.</param>
    public TorrentHelper(BitTorrentCache bittorrentCache, IPAddress hostIP, 
      int trackerPort, int gsserverPort) {
      _bittorrentCache = bittorrentCache;
      _hostIP = hostIP;
      _trackerPort = trackerPort;
      _gsserverPort = gsserverPort;
    }

    /// <summary>
    /// Creates torrent from the file specified by the given path.
    /// </summary>
    /// <param name="path">The path to the file/directory.</param>
    /// <param name="trackerUrl">The tracker URL.</param>
    /// <returns>The torrent</returns>
    public BEncodedDictionary CreateTorrent(string dataPath) {
      TorrentCreator creator = new TorrentCreator();
      creator.Comment = "This torrent file is automatically created.";
      creator.CreatedBy = "GatorShare";
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
    /// Gets the torrent file download Uri with which other peers can download 
    /// the torrent file from this server.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    public string GetTorrentFileUrlToPublish(string nameSpace, string name) {
      var template = new UriTemplate("TorrentData/{namespace}/{name}/TorrentFile");
      var baseAddr = string.Format("http://{0}:{1}", _hostIP, _gsserverPort);
      Uri retVal = template.BindByPosition(new Uri(baseAddr), nameSpace, name);
      return retVal.ToString();
    }

    public bool TryReadOrDownloadTorrent(string nameSpace, string name,
      DictionaryServiceProxy proxy, out byte[] torrent) {
        try {
          torrent = ReadOrDownloadTorrent(nameSpace, name, proxy);
          return true;
        } catch (DictionaryServiceException) {
          torrent = null;
          return false;
        }
    }

    /// <summary>
    /// Reads torrent from file system if exists or download it from DHT and writes it 
    /// to file system.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="proxy">The proxy.</param>
    /// <returns>Torrent bytes.</returns>
    /// <exception cref="DictionaryServiceException">When such a torrent isn't available.
    /// </exception>
    public byte[] ReadOrDownloadTorrent(string nameSpace, string name, 
      DictionaryServiceProxy proxy) {
      var torrentPath = _bittorrentCache.GetTorrentFilePath(nameSpace, name);
      if (File.Exists(torrentPath)) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props, 
          string.Format("Torrent file already exists. Reading it."));
        return File.ReadAllBytes(torrentPath);
      } else {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Torrent file doesn't exist. Downloading it."));
        byte[] torrentKey = ServiceUtil.GetDictKeyBytes(nameSpace, name);
        IList<byte[]> urls = proxy.GetUrlsToDownloadTorrent(torrentKey);
        int numServers = urls.Count;
        var rnd = new Random();
        var webClient = new WebClient();
        byte[] torrentBytes = null;
        do {
          int index = rnd.Next(numServers);
          byte[] urlBytes = urls[index];
          urls.RemoveAt(index);
          string urlToTry = Encoding.UTF8.GetString(urlBytes);
          try {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
              "Trying to download torrent from the #{0} peer in the {1}-item list.",
              index, numServers));
            torrentBytes = webClient.DownloadData(urlToTry);
            break;
          } catch (WebException ex) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
              "Failed to download torrent from this peer: {0}. Exception: {1}", 
              urlToTry, ex));
          }
        } while (--numServers > 0);

        if (torrentBytes == null) {
          throw new DictionaryServiceException(string.Format(
            "No such torrent file: {0} available.", name));
        }
        IOUtil.PrepareParentDirForPath(torrentPath);
        File.WriteAllBytes(torrentPath, torrentBytes);
        return torrentBytes;
      }
    }

  }
}
