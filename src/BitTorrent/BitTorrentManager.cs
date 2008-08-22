using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using Brunet;
using Fushare.Filesystem;
using Fushare.Services;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Client.Tracker;
using MonoTorrent.Common;
using MonoTorrent.Tracker;

namespace Fushare.BitTorrent {
  /// <summary>
  /// Manages activities pertaining to BitTorrent protocol
  /// </summary>
  public class BitTorrentManager {
    #region Fields
    private DhtServiceProxy _proxy;
    private DhtTracker _dht_tracker;
    ClientEngine _client_engine;
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(BitTorrentManager));

    string _bt_downloads_dir;
    string _bt_torrents_dir;
    string _bt_fastresume_file;
    string _bt_base_dir;

    string _tracker_listening_prefix;

    BEncodedDictionary _fast_resume;
    EngineSettings _bt_client_settings;
    TorrentSettings _torrent_defaults;
    /// <summary>
    /// A dictionary of (TorrentManager, Downloaded Path)
    /// </summary>
    /// <remarks>
    /// This is to let the client be aware of the cached/downloaded files in 
    /// the system so that if the user wants to download a file already in
    /// local machine, no network traffic will be needed.
    /// </remarks>
    Dictionary<TorrentManager, string> _torrents_table = new Dictionary<TorrentManager,string>();

    /// <summary>
    /// Default TTL for torrent file in DHT.
    /// </summary>
    public const int TorrentTtl = 60 * 60 * 24;
    public const string DownloadsDirName = "Downloads";
    public const string TorrentsDirName = "Torrents";
    public const string FastResumeFileName = "fastresume.data";
    #endregion

    public string BTBaseDir {
      get { return _bt_base_dir; }
    }

    public string BTDownloadsDir {
      get {
        return _bt_downloads_dir;
      }
    }

    public string BTTorrentsDir {
      get {
        return _bt_torrents_dir;
      }
    }

    /// <summary>
    /// Creates 
    /// </summary>
    /// <param name="btBaseDir"></param>
    /// <param name="clientPort"></param>
    /// <param name="trackerListeningPrefix"></param>
    public BitTorrentManager(string btBaseDir, int clientPort, string trackerListeningPrefix) {
      if (!Path.IsPathRooted(btBaseDir)) {
        throw new ArgumentException("btBaseDir should be rooted");
      }

      // init tracker
      BrunetDht dht = (BrunetDht)DictionaryServiceFactory.GetServiceInstance(
        typeof(BrunetDht));
      _proxy = new DhtServiceProxy(dht);
      _tracker_listening_prefix = trackerListeningPrefix;
      _dht_tracker = new DhtTracker(_proxy, trackerListeningPrefix);

      // init client engine
      _bt_base_dir = btBaseDir;
      _bt_downloads_dir = Path.Combine(_bt_base_dir, DownloadsDirName);
      _bt_torrents_dir = Path.Combine(_bt_base_dir, TorrentsDirName);
      _bt_fastresume_file = Path.Combine(_bt_base_dir, FastResumeFileName);
      _bt_client_settings = new EngineSettings(_bt_downloads_dir, clientPort);
      // Create the default settings which a torrent will have.
      // 4 Upload slots - a good ratio is one slot per 5kB of upload speed
      // 50 open connections - should never really need to be changed
      // Unlimited download speed - valid range from 0 -> int.Max
      // Unlimited upload speed - valid range from 0 -> int.Max
      _torrent_defaults = new TorrentSettings(4, 150, 0, 0);
      _client_engine = new ClientEngine(_bt_client_settings);

      // prepare directories
      if (!Directory.Exists(_client_engine.Settings.SavePath))
        Directory.CreateDirectory(_client_engine.Settings.SavePath);
      if (!Directory.Exists(_bt_torrents_dir))
        Directory.CreateDirectory(_bt_torrents_dir);

      try {
        _fast_resume = BEncodedValue.Decode<BEncodedDictionary>(
          File.ReadAllBytes(_bt_fastresume_file));
      } catch {
        _fast_resume = new BEncodedDictionary();
      }
    }

    /// <summary>
    /// Starts the manager and all BitTorrent services.
    /// </summary>
    public void Start() {
      _dht_tracker.Start();
      FilesystemWatcher.SetupTorrentWatcher(this, _bt_torrents_dir);
      // Client is started when StartDownload is called.
    }

    /// <summary>
    /// Adds torrent to tracker thread-safely
    /// </summary>
    public void AddTorrentToTracker(Torrent torrent) {
      ITrackable trackable = new InfoHashTrackable(torrent);
      lock (_dht_tracker.Tracker) {
        _dht_tracker.Tracker.Add(trackable);
      }
    }

    /// <summary>
    /// Serves file sharing over BitTorrent.
    /// </summary>
    /// <param name="filePath">
    /// The path of the data file to be shared over BitTorrent</param>
    /// <remarks>
    /// Should be used in general cases where DHT key is decided by infoHash of
    /// the file.
    /// </remarks>
    public byte[] ServeFile(string filePath) {
      byte[] dht_key = null;
      ServeFile(ref dht_key, filePath);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("dhtKey: {0}", dht_key == null ? "null" : Base32.Encode(dht_key)));
      return dht_key;
    }

    /// <summary>
    /// Serves the file over BitTorrent.
    /// </summary>
    /// <param name="dhtKey">The DHT key to be used to put the torrent info
    /// </param>
    public void ServeFile(ref byte[] dhtKey, string filePath) {
      BEncodedDictionary bdict = CreateTorrent(filePath, 
        _tracker_listening_prefix);
      byte[] torrent_bytes = bdict.Encode();

      Torrent torrent = Torrent.Load(bdict);
      byte[] info_hash = torrent.InfoHash;
      if (dhtKey == null) {
        byte[] suffix = Encoding.UTF8.GetBytes(".torrent");
        dhtKey = new byte[info_hash.Length + suffix.Length];
        info_hash.CopyTo(dhtKey, 0);
        suffix.CopyTo(dhtKey, info_hash.Length);

        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("DHT Key for this torrent file ({1}) generated: {0} (in Base32)", 
          Base32.Encode(dhtKey), torrent.Name));
      }

      // Dump it to the torrent folder so that tracker could load it.
      using (FileStream stream = new FileStream(Path.Combine(_bt_torrents_dir, 
        Base32.Encode(info_hash) + ".torrent"), FileMode.Create)) {
        stream.Write(torrent_bytes, 0, torrent_bytes.Length);
      }

      bool succ = _proxy.PutTorrent(dhtKey, torrent_bytes, TorrentTtl);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Torrent file succesfully put into DHT"));
      // Start downloading.
      string file_dir = new FileInfo(filePath).Directory.FullName;
      StartDownload(torrent, file_dir, null);
    }

    /// <summary>
    /// Gets file from the given dht key.
    /// </summary>
    /// <param name="torrentDhtKey">DHT key of the torrent</param>
    /// <param name="saveToPath">the shadow full path to save the file</param>
    public Torrent GetFile(byte[] torrentDhtKey, string saveToDir, EventWaitHandle waitHandle) {
      byte[] torrent_bytes = _proxy.GetTorrent(torrentDhtKey);

      Torrent torrent = Torrent.Load(torrent_bytes);

      // Dump it to the torrent folder so that the tracker could load it.
      using (FileStream stream = new FileStream(Path.Combine(_bt_torrents_dir,
        Base32.Encode(torrent.InfoHash) + ".torrent"), FileMode.Create)) {
        stream.Write(torrent_bytes, 0, torrent_bytes.Length);
      }
      
      StartDownload(torrent, saveToDir, waitHandle);
      return torrent;
    }

    /// <summary>
    /// Creates torrent from the file specified by the given path.
    /// </summary>
    private static BEncodedDictionary CreateTorrent(string filePath, string trackerUrl) {
      TorrentCreator creator = new TorrentCreator();
      creator.Comment = "Comment";
      creator.CreatedBy = "Fushare";
      creator.Path = filePath;
      creator.Announces.Add(new List<string>());
      creator.Announces[0].Add(trackerUrl);
      return creator.Create();
    }

    /// <summary>
    /// Downloads or seeds a file.
    /// </summary>
    /// <param name="torrentValue">Value of a torrent file</param>
    /// <param name="saveToSfp"></param>
    public void StartDownload(byte[] torrentValue, string saveDir, EventWaitHandle waitHandle) {
      Torrent torrent = Torrent.Load(torrentValue);
      StartDownload(torrent, saveDir, waitHandle);
    }

    public void StartDownload(string torrentFilePath, string saveDir, EventWaitHandle waitHandle) {
      Torrent torrent = Torrent.Load(torrentFilePath);
      StartDownload(torrent, saveDir, waitHandle);
    }

    /// <summary>
    /// Downloads or seeds a file.
    /// </summary>
    /// <param name="saveToSfp">
    /// The full path of the directory to save the downloaded file.</param>
    /// <param name="waitHandle">Handle to wait on for the downloading to be 
    /// finished. Pass null if no need to set the handle.</param>
    public void StartDownload(Torrent torrent, string saveDir, EventWaitHandle waitHandle) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Start downloading torrent {0} to directory {1}", 
        torrent.Name, saveDir));

      // When any preprocessing has been completed, you create a TorrentManager
      // which you then register with the engine.
      TorrentManager manager;
      if (_fast_resume.ContainsKey(torrent.InfoHash)) {
        manager = new TorrentManager(torrent, saveDir, _torrent_defaults,
          new FastResume((BEncodedDictionary)_fast_resume[torrent.InfoHash]));
      } else {
        manager = new TorrentManager(torrent, saveDir, _torrent_defaults);
      }

      _client_engine.Register(manager);

      // Every time a piece is hashed, this is fired.
      //manager.PieceHashed += delegate(object o, PieceHashedEventArgs e) {
      //  Logger.WriteLineIf(LogLevel.Verbose, _log_props,
      //    string.Format("{2}: Piece Hashed: {0} - {1}", e.PieceIndex, 
      //    e.HashPassed ? "Pass" : "Fail", manager.Torrent.Name));
      //};

      // Every time the state changes (Stopped -> Seeding -> Downloading -> Hashing) this is fired
      manager.TorrentStateChanged += delegate(object o, TorrentStateChangedEventArgs e) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("{0}: State changed from {1} to {2}", manager.Torrent.Name, 
          e.OldState.ToString(), e.NewState.ToString()));

        switch (e.NewState) {
          case TorrentState.Downloading:
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("OpenConnections: {0}"), manager.OpenConnections);
            break;
          case TorrentState.Seeding:
            if (e.OldState == TorrentState.Downloading) {
              TimeSpan download_time = DateTime.Now - manager.StartTime;
              double total_secs = download_time.TotalSeconds;
              double kb_data_downloaded = manager.Monitor.DataBytesDownloaded / 1024;
              double kb_data_uploaded = manager.Monitor.DataBytesUploaded / 1024;
              double kb_proto_downloaded = manager.Monitor.ProtocolBytesDownloaded / 1024;
              double kb_proto_uploaded = manager.Monitor.ProtocolBytesUploaded / 1024;
              Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                string.Format("Data Download {0:0.00} MB and Uploaded {1:0.00},"
                + " MB. Protocol Download {5:0.00} MB and Protocol Upload {6:0.00} in"
                + " {2:0.00} seconds. Avg Data Download rate: {3:0.00} kb/s, Avg"
                + " Data Upload rate: {4:0.00} kb/s, Avg Protocol Download rate:"
                + " {7:0.00} kb/s, Avg Protocol Upload rate: {8:0.00} kb/s",
                kb_data_downloaded / 1024, 
                kb_data_uploaded / 1024, 
                total_secs,
                kb_data_downloaded / total_secs,
                kb_data_uploaded / total_secs,
                kb_proto_downloaded / 1024,
                kb_proto_uploaded / 1024,
                kb_proto_downloaded / total_secs,
                kb_proto_uploaded / total_secs));
              if (waitHandle != null) {
                waitHandle.Set();
              }
            }
            break;
          default:
            break;
        }
      };

      // Log the first time when a peer connects.
      manager.PeerConnected += delegate(object sender, 
        PeerConnectionEventArgs e) {
        if (e.TorrentManager.OpenConnections == 1) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Peer ({0}) Connected. Currently 1 open connection.",
            e.PeerID.Location));
        }
      };

      // Log when the no connection left after a disconnection. 
      manager.PeerDisconnected += delegate(object sender, 
        PeerConnectionEventArgs e) {
        if (e.TorrentManager.OpenConnections == 0) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("{1}: Peer ({0}) disconnected. Currently no open "
            + "connection.", e.PeerID.Location, 
            e.TorrentManager.Torrent.Name));
        }
      };

      // Every time the tracker's state changes, this is fired
      foreach (TrackerTier tier in manager.TrackerManager.TrackerTiers) {
        foreach (MonoTorrent.Client.Tracker.Tracker t in tier.Trackers) {
          t.AnnounceComplete += delegate(object sender, AnnounceResponseEventArgs e) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("{2}: {0} to Tracker", e.Successful ? 
              "Succesfully announced" : "Failed to announce", 
              manager.Torrent.Name));
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("{0} seeders and {1} leechers returned by Tracker",
              e.Tracker.Complete, e.Tracker.Incomplete));
          };

          t.StateChanged += delegate(object sender, TrackerStateChangedEventArgs e) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("Tracker state changed from {0} to {1}", e.OldState, e.NewState));
          };
        }
      }

      // Start downloading
      manager.Start();

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("TorrentManager {0} started", manager.Torrent.Name));
      // Store the torrent manager in our list so we can access it later
      _torrents_table.Add(manager, saveDir);
    }
  }
}
