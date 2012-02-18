using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Client.Tracker;
using MonoTorrent.Common;
using MonoTorrent.Tracker;
using System.Text;
using GSeries.External.DictionaryService;
using GSeries.External;

namespace GSeries.Services.BitTorrent {
  /// <summary>
  /// Manages activities pertaining to BitTorrent protocol
  /// </summary>
  public class BitTorrentManager {
    #region Fields

    DictionaryServiceProxy _dictProxy;
    DictionaryServiceTracker _dictTracker;
    /// <summary>
    /// A dictionary of (TorrentManager, Downloaded Path)
    /// </summary>
    /// <remarks>
    /// This is to let the client be aware of the cached/downloaded files in 
    /// the system so that if the user wants to download a file already in
    /// local machine, no network traffic will be needed.
    /// </remarks>
    Dictionary<TorrentManager, string> _torrents_table = new Dictionary<TorrentManager, string>();
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(BitTorrentManager));
    TorrentHelper _torrentHelper;
    bool _startSeedingAtStartup;
    #endregion
 
    #region MonoTorrent
    BEncodedDictionary _fastResumeData;
    TorrentSettings _torrentDefaults;
    ClientEngine _clientEngine;
    BitTorrentCache _bittorrentCache;
    #endregion

    #region Constants
    /// <summary>
    /// Default TTL for torrent file in DHT.
    /// </summary>
    public const int TorrentTtl = 60 * 60 * 24;
    #endregion

    #region Properties
    public CacheRegistry CacheRegistry { get; private set; }

    /// <summary>
    /// Gets the listening prefix.
    /// </summary>
    public string ListeningPrefix {
      get {
        return _dictTracker.ListeningPrefix;
      }
    }

    public string SelfNameSpace { get; private set; }

    internal ClientEngine ClientEngine { get { return _clientEngine; }}
    #endregion

    #region Constructors

    public BitTorrentManager(BitTorrentCache bittorrentCache, string selfNameSpace,
      DictionaryServiceProxy dhtProxy, DictionaryServiceTracker dhtTracker, ClientEngine clientEngine,
      TorrentSettings torrentDefaults, TorrentHelper torrentHelper, 
      bool startSeedingAtStartup) {
      _bittorrentCache = bittorrentCache;
      SelfNameSpace = selfNameSpace;
      _dictProxy = dhtProxy;
      _dictTracker = dhtTracker;
      _torrentDefaults = torrentDefaults;
      _startSeedingAtStartup = startSeedingAtStartup;

      RegisterClientEngineEventHandlers(clientEngine);
      _clientEngine = clientEngine;

      _torrentHelper = torrentHelper;

      try {
        _fastResumeData = BEncodedValue.Decode<BEncodedDictionary>(
          File.ReadAllBytes(_bittorrentCache.FastResumeFilePath));
      } catch {
        _fastResumeData = new BEncodedDictionary();
      }

      // CacheRegistry is created here because the default cache registry file path is 
      // defined here.
      CacheRegistry = new CacheRegistry(_bittorrentCache.CacheRegistryFilePath, selfNameSpace);
      CacheRegistry.LoadCacheDir(_bittorrentCache.DownloadsDirPath);
    }

    #endregion
    
    #region Public Methods

    /// <summary>
    /// Starts the torrentManager and all BitTorrent services.
    /// </summary>
    public void Start() {
      _dictTracker.Start();
      TorrentFolderWatcherHelper.SetupTorrentWatcher(this, _bittorrentCache.TorrentsDirPath);
      // Client is started when StartDownload is called.
      if (_startSeedingAtStartup) {
        StartSeedingLocalFiles();
      }
    }

    private void StartSeedingLocalFiles() {
      foreach (var entry in CacheRegistry.Registry) {
        string nameSpace, name;
        ServiceUtil.ParseDictKeyString(entry.Key, out nameSpace, out name);
        string downloadPath;
        // @TODO: The case in which the file is outside the cache dir should be handled 
        // when BitTorrentService.Get(nameSpace, name, saveDirPath) is implemented.
        GetData(nameSpace, name, out downloadPath);
      }
    }

    /// <summary>
    /// Adds torrent to tracker thread-safely
    /// </summary>
    public void AddTorrentToTracker(Torrent torrent) {
      ITrackable trackable = new InfoHashTrackable(torrent);
      lock (_dictTracker.Tracker) {
        _dictTracker.Tracker.Add(trackable);
      }
    }

    /// <summary>
    /// Serves file sharing over BitTorrent.
    /// </summary>
    /// <param name="path">
    /// The path of the data file to be shared over BitTorrent</param>
    /// <remarks>
    /// Should be used in general cases where DHT name is decided by infoHash of
    /// the file.
    /// </remarks>
    [Obsolete]
    public byte[] ServeFile(string filePath) {
      byte[] dht_key = null;
      PublishData("", "");
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("dhtKey: {0}", dht_key == null ? "null" : Base32.Encode(dht_key)));
      return dht_key;
    }

    public void PublishData(string nameSpace, string name) {
      PublishDataInternal(nameSpace, name, true);
    }

    public void UpdateData(string nameSpace, string name) {
      PublishDataInternal(nameSpace, name, false);
    }

    /// <summary>
    /// Gets file from the given dht name.
    /// </summary>
    /// <param name="torrentDhtKey">DHT name of the torrent</param>
    /// <param name="torrent">The torrent.</param>
    /// <param name="saveToPath">The path to save. Not the containing dir.</param>
    /// <param name="waitHandle">The EventWaitHandle to wait on.</param>
    /// <remarks>The torrent of the data could be either already in place or not.
    /// </remarks>
    void GetDataInternal(byte[] torrentDhtKey, Torrent torrent,
      string saveToPath, EventWaitHandle waitHandle) {

      // The file/dir hasn't been downloaded yet. So, don't check path.
      var saveDir = IOUtil.GetParent(saveToPath, false).FullName;
      StartDownload(torrent, saveDir, waitHandle);
    }

    public byte[] GetData(string nameSpace, string name, out string downloadPath) {
      return GetData(nameSpace, name, out downloadPath, false);
    }

    public byte[] PeekData(string nameSpace, string name, out string downloadPath) {
      return GetData(nameSpace, name, out downloadPath, true);
    }

    /// <summary>
    /// Gets the data from the given dht name.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="waitHandle">The wait handle.</param>
    /// <param name="downloadPath">The download path.</param>
    /// <returns>Torrent bytes</returns>
    /// <exception cref="DictionaryKeyNotFoundException">Torrent at the given key is 
    /// invalid.</exception>
    /// <remarks>MonoTorrent library provides easy conversion from bytes to 
    /// Torrent object but not vise versa so we return bytes.</remarks>
    byte[] GetData(string nameSpace, string name, out string downloadPath, bool peek) {
      ManualResetEvent waitHandle = new ManualResetEvent(false);
      byte[] torrentDhtKey = ServiceUtil.GetDictKeyBytes(nameSpace, name);
      downloadPath =
        _bittorrentCache.GetPathOfItemInDownloads(nameSpace, name);
      // @TODO Check integrity of the data -- How do we know the download is complete?
      // A possbile solution. Use the name <name.part> and change it to <name> after
      // download completes.
      byte[] torrentBytes;
      if (!CacheRegistry.IsInCacheRegistry(nameSpace, name)) {
        try {
          var torrentSavePath = _bittorrentCache.GetTorrentFilePath(nameSpace, name);
          torrentBytes = _torrentHelper.ReadOrDownloadTorrent(nameSpace, 
            name, _dictProxy);
          var torrent = Torrent.Load(torrentBytes);
          if (!peek) {
            GetDataInternal(torrentDhtKey, torrent, downloadPath,
            waitHandle);

            // Wait until downloading finishes
            waitHandle.WaitOne();
            // Download completed.
            CacheRegistry.AddPathToRegistry(downloadPath, true);
          } else {
            // If we are only peeking, we don't add it to the registry.
          }
        } catch (TorrentException ex) {
          throw new DictionaryKeyNotFoundException(string.Format(
            "Torrent at key {0} (UrlBase64) is invalid.",
            UrlBase64.Encode(torrentDhtKey)), ex);
        }
      } else {
        torrentBytes = _torrentHelper.ReadOrDownloadTorrent(nameSpace, 
          name, _dictProxy);
        var torrent = Torrent.Load(torrentBytes);
        if (!_clientEngine.Contains(torrent)) {
          // This is the case where the manager start seeding data when it boots up.
          GetDataInternal(torrentDhtKey, torrent, downloadPath, null);
        } else {
          // If the data is already there and the client engine is busily downloading or
          // seeding, we don't need to do it again.
          return torrentBytes;
        }
      }
      return torrentBytes;
    }

    #endregion

    #region Private Methods
    private static void RegisterClientEngineEventHandlers(ClientEngine clientEngine) {
      clientEngine.CriticalException += delegate(object sender, CriticalExceptionEventArgs args) {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
          string.Format("ClientEngine Critical Exception: {0}", args.Exception));
      };

      //cliengEngine.StatsUpdate += delegate(object sender, StatsUpdateEventArgs args) {
      //  Logger.WriteLineIf(LogLevel.Verbose, _log_props,
      //    string.Format("ClientEngine Stats Update"));
      //};

      clientEngine.TorrentRegistered += delegate(object sender, TorrentEventArgs args) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("ClientEngine Torrent Registered: {0}.", args.TorrentManager.Torrent.Name));
      };

      //cliengEngine.ConnectionManager.PeerMessageTransferred += delegate(object sender, PeerMessageEventArgs args) {
      //  Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
      //    "ClientEngine Peer Message Transferred. Direction:{0}, Message: {1}.",
      //    args.Direction, args.Message));
      //};
    }
    
    /// <summary>
    /// Serves the file over BitTorrent.
    /// </summary>
    /// <param name="dhtKey">The DHT name to be used to put the torrent info</param>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="path">The file or directory path.</param>
    /// <param name="unique">if set to <c>true</c>, the manager uses Create 
    /// instead of Put to insert torrent to Dht.</param>
    void PublishDataInternal(string nameSpace, string name, bool unique) {
      byte[] dhtKey = ServiceUtil.GetDictKeyBytes(nameSpace, name);
      var dataPath = _bittorrentCache.GetPathOfItemInDownloads(nameSpace, name);
      var torrentSavePath = _bittorrentCache.GetTorrentFilePath(nameSpace, name);

      // Create torrent
      BEncodedDictionary bdict = _torrentHelper.CreateTorrent(dataPath);
      Torrent torrent = Torrent.Load(bdict);
      // Dump torrent to the torrent folder so that tracker could load it.
      byte[] torrentBytes = bdict.Encode();
      TorrentHelper.WriteTorrent(torrentBytes, torrentSavePath);

      string torrentUrl = _torrentHelper.GetTorrentFileUrlToPublish(nameSpace, name);
      // Put the Url bytes to the dictionary.
      var torrentUrlBytes = Encoding.UTF8.GetBytes(torrentUrl);

      if (unique) {
        _dictProxy.CreateTorrent(dhtKey, torrentUrlBytes, TorrentTtl);
        CacheRegistry.AddPathToRegistry(dataPath, true);
      } else {
        _dictProxy.PutTorrent(dhtKey, torrentUrlBytes, TorrentTtl);
        CacheRegistry.UpdatePathInRegistry(dataPath, true);
      }

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("{0}: Succesfully registered torrent url to dictionary.", 
        torrent.Name));

      var saveDir = IOUtil.GetParent(dataPath, true).FullName;
      // Download without blocking.
      StartDownload(torrent, saveDir, null);
    }

    /// <summary>
    /// Downloads or seeds a file.
    /// </summary>
    /// <param name="torrent">The torrent.</param>
    /// <param name="saveDir">The full path of the directory to save the 
    /// downloaded file.</param>
    /// <param name="waitHandle">Handle to wait on for the downloading to be
    /// finished. Pass null if no need to set the handle.</param>
    void StartDownload(Torrent torrent, string saveDir, EventWaitHandle waitHandle) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Starting to download torrent {0} to directory {1}",
        torrent.Name, saveDir));

      // When any preprocessing has been completed, you create a TorrentManager
      // which you then register with the engine.
      TorrentManager torrentManager;
      if (_fastResumeData.ContainsKey(torrent.InfoHash.ToArray())) {
        torrentManager = new TorrentManager(torrent, saveDir, _torrentDefaults);
        torrentManager.LoadFastResume(new FastResume((BEncodedDictionary)_fastResumeData[torrent.InfoHash.ToArray()]));
      } else {
        torrentManager = new TorrentManager(torrent, saveDir, _torrentDefaults);
      }

      _clientEngine.Register(torrentManager);

      // Every time a piece is hashed, this is fired.
      //torrentManager.PieceHashed += delegate(object o, PieceHashedEventArgs e) {
      //  Logger.WriteLineIf(LogLevel.Verbose, _log_props,
      //    string.Format("{2}: Piece Hashed: {0} - {1}", e.PieceIndex, 
      //    e.HashPassed ? "Pass" : "Fail", torrentManager.Torrent.Name));
      //};

      // Every time the state changes (Stopped -> Seeding -> Downloading -> Hashing) this is fired
      torrentManager.TorrentStateChanged += delegate(object o, TorrentStateChangedEventArgs e) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("{0}: State changed from {1} to {2}", torrentManager.Torrent.Name,
          e.OldState.ToString(), e.NewState.ToString()));

        switch (e.NewState) {
          case TorrentState.Downloading:
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("OpenConnections: {0}", torrentManager.OpenConnections));
            break;
          case TorrentState.Seeding:
            if (e.OldState == TorrentState.Downloading) {
              Logger.WriteLineIf(LogLevel.Info, _log_props, string.Format(
                "{0}: Download completed.", torrentManager.Torrent.Name));
              // Torrent statistics.
              LogTorrentStatistics(torrentManager);
              // Flush so that the file readers can get a hold of the file.
              _clientEngine.DiskManager.Flush(e.TorrentManager);
              
              if (waitHandle != null) {
                // Now that we have downloaded the file, we release the waitHandle.
                waitHandle.Set();
              }
            }
            break;
          default:
            break;
        }
      };

      // Log the first time when a peer connects.
      torrentManager.PeerConnected += delegate(object sender,
        PeerConnectionEventArgs e) {
        if (e.TorrentManager.OpenConnections == 1) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Peer ({0}) Connected. Currently 1 open connection.",
            e.PeerID.Uri));
        }
      };

      // Log when the no connection left after a disconnection. 
      torrentManager.PeerDisconnected += delegate(object sender,
        PeerConnectionEventArgs e) {
        if (e.TorrentManager.OpenConnections == 0) {
          Logger.WriteLineIf(LogLevel.Info, _log_props, string.Format(
            "{1}: Peer ({0}) disconnected. Message: {2}. No open connection now.",
            e.PeerID.Uri,
            e.TorrentManager.Torrent.Name,
            e.Message));
        }
        LogTorrentStatistics(e.TorrentManager);
      };

      torrentManager.PeersFound += delegate(object o, PeersAddedEventArgs e) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
          "{2}: PeersFound: {0} New Peers. {1} Existing Peers.",
          e.NewPeers,
          e.ExistingPeers,
          e.TorrentManager.Torrent.Name));
      };

      foreach (TrackerTier tier in torrentManager.TrackerManager.TrackerTiers) {
        foreach (MonoTorrent.Client.Tracker.Tracker t in tier) {
          t.AnnounceComplete += delegate(object sender,
            AnnounceResponseEventArgs e) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("{0}: AnnounceComplete. Tracker={1}, Successful={2}",
              torrentManager.Torrent.Name,
              e.Tracker.Uri,
              e.Successful));
            Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
              "Tracker: Peers={2}, Complete={0}, Incomplete={1}",
              e.Tracker.Complete,
              e.Tracker.Incomplete,
              e.Peers.Count));
          };
        }
      }

      // Start downloading
      torrentManager.Start();

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("{0}: TorrentManager started", torrentManager.Torrent.Name));
    }

    private static void LogTorrentStatistics(TorrentManager torrentManager) {
      // The StartTime was is first set using DateTime.Now;
      TimeSpan download_time = DateTime.Now - torrentManager.StartTime;
      double total_secs = download_time.TotalSeconds;
      double kb_data_downloaded = torrentManager.Monitor.DataBytesDownloaded / 1024.0;
      double kb_data_uploaded = torrentManager.Monitor.DataBytesUploaded / 1024.0;
      double kb_proto_downloaded = torrentManager.Monitor.ProtocolBytesDownloaded / 1024.0;
      double kb_proto_uploaded = torrentManager.Monitor.ProtocolBytesUploaded / 1024.0;
      Logger.WriteLineIf(LogLevel.Info, _log_props,
        string.Format(
        "Torrent: {9} \n"
        + "Torrent Lifetime: {2:0.00} seconds. \n" 
        + "Data Downloaded: {0:0.00} MB. \n"
        + "Data Uploaded: {1:0.00} MB. \n"
        + "Protocol Downloaded {5:0.00} MB \n"
        + "Protocol Uploaded {6:0.00} \n"
        + "Average Data Download Rate: {3:0.00} kB/s \n"
        + "Average Data Upload Rate: {4:0.00} kB/s \n"
        + "Average Protocol Download Rate: {7:0.00} kB/s \n"
        + "Average Protocol Upload Rate: {8:0.00} kB/s ",
        kb_data_downloaded / 1024,
        kb_data_uploaded / 1024,
        total_secs,
        kb_data_downloaded / total_secs,
        kb_data_uploaded / total_secs,
        kb_proto_downloaded / 1024,
        kb_proto_uploaded / 1024,
        kb_proto_downloaded / total_secs,
        kb_proto_uploaded / total_secs,
        torrentManager.Torrent.Name));
    } 
    #endregion
  }
}
