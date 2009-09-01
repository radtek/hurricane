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

namespace Fushare.Services.BitTorrent {
  /// <summary>
  /// Manages activities pertaining to BitTorrent protocol
  /// </summary>
  public class BitTorrentManager {
    #region Fields

    DhtProxy _dhtProxy;
    DhtTracker _dhtTracker;
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
    #endregion
 
    #region MonoTorrent
    BEncodedDictionary _fastResumeData;
    [Obsolete]
    EngineSettings _btEngineSettings;
    TorrentSettings _torrentDefaults;
    ClientEngine _clientEngine;
    #endregion

    #region Constants
    /// <summary>
    /// Default TTL for torrent file in DHT.
    /// </summary>
    public const int TorrentTtl = 60 * 60 * 24;
    public const string DownloadsDirName = "Downloads";
    public const string TorrentsDirName = "Torrents";
    public const string FastResumeFileName = "fastresume.data";
    public const string CacheRegistryFileName = "cacheRegistry.xml";
    #endregion

    #region Properties
    public CacheRegistry CacheRegistry { get; private set; }
    /// <summary>
    /// The full path of the base directory assigned to this manager.
    /// </summary>
    /// <value></value>
    public string BaseDirPath { get; set; }

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

    /// <summary>
    /// Gets the listening prefix.
    /// </summary>
    public string ListeningPrefix {
      get {
        return _dhtTracker.ListeningPrefix;
      }
    }

    public string SelfNameSpace { get; private set; }
    #endregion

    #region Constructors

    public BitTorrentManager(string baseDirPath, string selfNameSpace,
      DhtProxy dhtProxy, DhtTracker dhtTracker, ClientEngine clientEngine,
      TorrentSettings torrentDefaults, TorrentHelper torrentHelper) {
      IOUtil.CheckPathRooted(baseDirPath, "baseDirPath");

      BaseDirPath = baseDirPath;
      SelfNameSpace = selfNameSpace;
      _dhtProxy = dhtProxy;
      _dhtTracker = dhtTracker;
      _torrentDefaults = torrentDefaults;

      RegisterClientEngineEventHandlers(clientEngine);
      _clientEngine = clientEngine;

      // Prepare directories
      if (!Directory.Exists(DownloadsDirPath))
        Directory.CreateDirectory(DownloadsDirPath);
      if (!Directory.Exists(TorrentsDirPath))
        Directory.CreateDirectory(TorrentsDirPath);

      try {
        _fastResumeData = BEncodedValue.Decode<BEncodedDictionary>(
          File.ReadAllBytes(FastResumeFilePath));
      } catch {
        _fastResumeData = new BEncodedDictionary();
      }

      _torrentHelper = torrentHelper;

      // CacheRegistry is created here because the default cache registry file path is 
      // defined here.
      CacheRegistry = new CacheRegistry(CacheRegistryFilePath, selfNameSpace);
      CacheRegistry.LoadCacheDir(DownloadsDirPath);
    }

    [Obsolete("Bad design.")]
    public BitTorrentManager(string btBaseDir, int clientPort, string trackerListeningPrefix) {
      IOUtil.CheckPathRooted(btBaseDir, "btBaseDir");

      // init tracker
      BrunetDht dht = (BrunetDht)DictionaryServiceFactory.GetServiceInstance(
        typeof(BrunetDht));
      _dhtProxy = new DhtProxy(dht, 0);
      _dhtTracker = new DhtTracker(_dhtProxy, trackerListeningPrefix);

      // init client engine
      BaseDirPath = btBaseDir;
      _btEngineSettings = new EngineSettings(DownloadsDirPath, clientPort);
      // Create the default settings which a torrent will have.
      // 4 Upload slots - a good ratio is one slot per 5kB of upload speed
      // 50 open connections - should never really need to be changed
      // Unlimited download speed - valid range from 0 -> int.Max
      // Unlimited upload speed - valid range from 0 -> int.Max
      _torrentDefaults = new TorrentSettings(4, 150, 0, 0);
      _clientEngine = new ClientEngine(_btEngineSettings);

      // prepare directories
      if (!Directory.Exists(_clientEngine.Settings.SavePath))
        Directory.CreateDirectory(_clientEngine.Settings.SavePath);
      if (!Directory.Exists(TorrentsDirPath))
        Directory.CreateDirectory(TorrentsDirPath);

      try {
        _fastResumeData = BEncodedValue.Decode<BEncodedDictionary>(
          File.ReadAllBytes(FastResumeFilePath));
      } catch {
        _fastResumeData = new BEncodedDictionary();
      }
    } 

    #endregion
    
    #region Public Methods

    /// <summary>
    /// Starts the torrentManager and all BitTorrent services.
    /// </summary>
    public void Start() {
      _dhtTracker.Start();
      TorrentFolderWatcherHelper.SetupTorrentWatcher(this, TorrentsDirPath);
      // Client is started when StartDownload is called.
    }

    /// <summary>
    /// Adds torrent to tracker thread-safely
    /// </summary>
    public void AddTorrentToTracker(Torrent torrent) {
      ITrackable trackable = new InfoHashTrackable(torrent);
      lock (_dhtTracker.Tracker) {
        _dhtTracker.Tracker.Add(trackable);
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

    /// <summary>
    /// Gets the data from the given dht name.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="waitHandle">The wait handle.</param>
    /// <param name="downloadPath">The download path.</param>
    /// <returns>Torrent bytes</returns>
    /// <exception cref="ResourceNotFoundException">Torrent at the given key is 
    /// invalid.</exception>
    /// <remarks>MonoTorrent library provides easy conversion from bytes to 
    /// Torrent object but not vise versa so we return bytes.</remarks>
    public byte[] GetData(string nameSpace, string name, out string downloadPath) {
      ManualResetEvent waitHandle = new ManualResetEvent(false);
      byte[] torrentDhtKey = ServiceUtil.GetDhtKeyBytes(nameSpace, name);
      downloadPath =
        GetPathOfItemInDownloads(nameSpace, name);
      // @TODO Check integrity of the data -- How do we know the download is complete?
      // A possbile solution. Use the name <name.part> and change it to <name> after
      // download completes.
      byte[] torrentBytes;
      if (!CacheRegistry.IsInCacheRegistry(nameSpace, name)) {
        try {
          var torrentSavePath = _torrentHelper.GetTorrentFilePath(nameSpace, name);
          torrentBytes = _torrentHelper.ReadOrDownloadTorrent(nameSpace, 
            name, _dhtProxy);
          var torrent = Torrent.Load(torrentBytes);
          GetDataInternal(torrentDhtKey, torrent, downloadPath,
              waitHandle);

          // Wait until downloading finishes
          waitHandle.WaitOne();
          // Download completed.
          CacheRegistry.AddPathToRegistry(downloadPath, true);
        } catch (TorrentException ex) {
          throw new ResourceNotFoundException(string.Format(
            "Torrent at key {0} (UrlBase64) is invalid.",
            UrlBase64.Encode(torrentDhtKey)), ex);
        }
      } else {
        // If the data is already there, we don't need to download it again.
        torrentBytes = _torrentHelper.ReadOrDownloadTorrent(nameSpace, 
          name, _dhtProxy);
      }
      return torrentBytes;
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

    internal static string GetTorrentsDirPath(string baseDirPath) {
      return Path.Combine(baseDirPath, TorrentsDirName);
    }
    #endregion

    #region Private Methods
    private static void RegisterClientEngineEventHandlers(ClientEngine cliengEngine) {
      cliengEngine.CriticalException += delegate(object sender, CriticalExceptionEventArgs args) {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
          string.Format("ClientEngine Critical Exception: {0}", args.Exception));
      };

      //cliengEngine.StatsUpdate += delegate(object sender, StatsUpdateEventArgs args) {
      //  Logger.WriteLineIf(LogLevel.Verbose, _log_props,
      //    string.Format("ClientEngine Stats Update"));
      //};

      cliengEngine.TorrentRegistered += delegate(object sender, TorrentEventArgs args) {
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
      byte[] dhtKey = ServiceUtil.GetDhtKeyBytes(nameSpace, name);
      var dataPath = GetPathOfItemInDownloads(nameSpace, name);
      var torrentSavePath = _torrentHelper.GetTorrentFilePath(nameSpace, name);

      // Create torrent
      BEncodedDictionary bdict = _torrentHelper.CreateTorrent(dataPath);
      Torrent torrent = Torrent.Load(bdict);
      // Dump torrent to the torrent folder so that tracker could load it.
      byte[] torrentBytes = bdict.Encode();
      TorrentHelper.WriteTorrent(torrentBytes, torrentSavePath);

      if (unique) {
        _dhtProxy.CreateTorrent(dhtKey, torrentBytes, TorrentTtl);
        CacheRegistry.AddPathToRegistry(dataPath, true);
      } else {
        _dhtProxy.PutTorrent(dhtKey, torrentBytes, TorrentTtl);
        CacheRegistry.UpdatePathInRegistry(dataPath, true);
      }

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("{0}: Torrent file succesfully put into DHT.", torrent.Name));

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
      if (_fastResumeData.ContainsKey(torrent.InfoHash)) {
        torrentManager = new TorrentManager(torrent, saveDir, _torrentDefaults,
          new FastResume((BEncodedDictionary)_fastResumeData[torrent.InfoHash]));
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
              // Torrent statistics.
              TimeSpan download_time = DateTime.Now - torrentManager.StartTime;
              double total_secs = download_time.TotalSeconds;
              double kb_data_downloaded = torrentManager.Monitor.DataBytesDownloaded / 1024.0;
              double kb_data_uploaded = torrentManager.Monitor.DataBytesUploaded / 1024.0;
              double kb_proto_downloaded = torrentManager.Monitor.ProtocolBytesDownloaded / 1024.0;
              double kb_proto_uploaded = torrentManager.Monitor.ProtocolBytesUploaded / 1024.0;
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
          Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "{1}: Peer ({0}) disconnected. Message: {2}. No open connection now.",
            e.PeerID.Uri,
            e.TorrentManager.Torrent.Name,
            e.Message));
        }
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
    #endregion
  }
}
