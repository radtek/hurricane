// Copyright (c) 2011 Jiangyan Xu <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace gSeries {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using log4net.Appender;
  using log4net.Config;
  using log4net.Layout;
  using MonoTorrent.Client;
  using MonoTorrent.Client.Encryption;


  /// <summary>
  /// Downloads content using modified MonoTorrent.
  /// </summary>
  public class TestIncompletePieces {
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

    private static void StartDownload(ClientEngine clientEngine, 
      TorrentSettings torrentDefaults) {
      throw new NotImplementedException();
    }
  }
}
