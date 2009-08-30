using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml.Serialization;

using Fushare.Services;

namespace Fushare.Configuration {
  /// <summary>
  /// Main config file of fushare application.
  /// </summary>
  /// <remarks>
  /// All directory names should have DirName suffix and don't have 
  /// DirectorySeparator.
  /// </remarks>
  [XmlType("fushareConfig")]
  public class FushareConfig {
    public ServiceConfigSection serviceConfig = new ServiceConfigSection();
    public BitTorrentConfigSection bitTorrentConfig = new BitTorrentConfigSection();
    public FilesystemConfigSection filesysConfig = new FilesystemConfigSection();
  }

  /// <summary>
  /// The configuration of BitTorrent module of Fushare.
  /// </summary>
  public class BitTorrentConfigSection {
    public bool enableBitTorrent = true;
    /// <summary>
    /// The prefix that DhtTracker passes to start System.Net.HttpListener
    /// </summary>
    public int dhtTrackerPort = 20123;
    /// <summary>
    /// The directory inside fushareSysDir that stores tmp bt data
    /// </summary>
    public string btTmpDirName = ".bt";
    /// <summary>
    /// The port that the MonoTorrent client uses.
    /// </summary>
    public int clientListenPort = 28399;
  }

  public class FilesystemConfigSection {
    /// <summary>
    /// The directory for tmp and config data of Fushare.
    /// </summary>
    public string fushareSysDirName = ".fushare";
    /// <summary>
    /// Whether real (shadow full) path can be translated as the combination 
    /// of shadow root and fuse path.
    /// </summary>
    public bool shadowPathEqualsFusePath = true;
  }
}
