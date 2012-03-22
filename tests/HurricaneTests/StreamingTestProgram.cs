// Copyright (c) 2011 Jiangyan Xu <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using System.IO;
using MonoTorrent.Common;
using GSeries.BitTorrent;
using log4net.Config;
using log4net.Appender;
using log4net.Layout;

namespace MonoTorrentExperiments {
  /// <summary>
  /// Downloads the content using a sliding window.
  /// </summary>
  public class StreamingTestProgram {
    const string TorrentFileName = "ubuntu-11.04-server-i386.iso.torrent";

    public static void Main(string[] args) {
      BasicConfigurator.Configure(new ConsoleAppender() {
        Layout = new PatternLayout(
          "%timestamp [%thread] %-5level %logger{1} - %message%newline")
      });

      var torrentDefaults = new TorrentSettings(4, 150, 0, 0);
      torrentDefaults.UseDht = false;
      var engineSettings = new EngineSettings();
      engineSettings.PreferEncryption = false;
      engineSettings.AllowedEncryption = EncryptionTypes.All;
      var clientEngine = new ClientEngine(engineSettings);
      StartDownload(clientEngine, torrentDefaults);
    }

    private static void StartDownload(ClientEngine clientEngine, TorrentSettings
      torrentDefaultSettings) {
      string baseDir = Path.Combine(Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.Personal),
          "var"), "MonoTorrent");
      Debug.WriteLine(string.Format("Base Dir is {0}", baseDir));
      var torrent = Torrent.Load(Path.Combine(baseDir, TorrentFileName));
      var torrentManager = new TorrentManager(torrent, Path.Combine(baseDir, 
        "Downloads"), torrentDefaultSettings, "", -1);

      clientEngine.Register(torrentManager);
      torrentManager.Start();
      Console.Read();
    }
  }
}
