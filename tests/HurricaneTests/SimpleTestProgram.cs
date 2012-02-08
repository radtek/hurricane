// Copyright (c) 2011 Jiangyan Xu <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;
using System.Threading;
using System.Diagnostics;
using GSeries.BitTorrent;

namespace MonoTorrentExperiments {
  class SimpleTestProgram {
    const string TorrentFileName = "Zenith.Part.1.2011.Xvid-VODO.torrent";
   
    static void Main(string[] args) {
      var traceListener = new ConsoleTraceListener();
      
      Debug.Listeners.Add(traceListener);

      var torrentDefaults = new TorrentSettings(4, 150, 0, 0);
      torrentDefaults.UseDht = false;
      var engineSettings = new EngineSettings();
      engineSettings.PreferEncryption = false;
      engineSettings.AllowedEncryption = EncryptionTypes.All;
      var clientEngine = new ClientEngine(engineSettings);
      StartDownload(clientEngine, torrentDefaults);
    }

    static void StartDownload(ClientEngine clientEngine, TorrentSettings torrentDefaultSettings) {
      string baseDir = Path.Combine(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Personal), "var"), 
        "MonoTorrent");
      Debug.WriteLine(string.Format("Base Dir is {0}", baseDir));

      var torrent = Torrent.Load(Path.Combine(baseDir, TorrentFileName));
      (torrent.Files[0] as TorrentFile).Priority = Priority.Highest;
      (torrent.Files[1] as TorrentFile).Priority = Priority.DoNotDownload;
      (torrent.Files[2] as TorrentFile).Priority = Priority.DoNotDownload;
      long targetDownloadSize = (torrent.Files[0] as TorrentFile).Length;
      long totalSize = torrent.Size;
      double targetPercentage = (double)targetDownloadSize / totalSize;
      Debug.WriteLine(string.Format("Intend to download {0}/{1} = {2}", 
        targetDownloadSize, totalSize, targetPercentage));

      var torrentManager = new TorrentManager(torrent, Path.Combine(baseDir, "Downloads"), torrentDefaultSettings);
      var progressMonitor = new PartialDownloadProgressMonitor(torrentManager);
      progressMonitor.FileDownloaded += new EventHandler<FileDownloadedEventArgs>(progressMonitor_FileDownloaded);
      
      clientEngine.Register(torrentManager);

      torrentManager.TorrentStateChanged += new EventHandler<TorrentStateChangedEventArgs>(torrentManager_TorrentStateChanged);
      // Start downloading
      torrentManager.Start();

      // Keep running while the torrent isn't stopped or paused.
      while (torrentManager.State != TorrentState.Stopped && 
        torrentManager.State != TorrentState.Paused) {
        Debug.WriteLine(string.Format("Progress: {0}", torrentManager.Progress));
        System.Threading.Thread.Sleep(2000);
      }

      Console.Read();
    }

    static void progressMonitor_FileDownloaded(object sender, 
      FileDownloadedEventArgs e) {
      Debug.WriteLine(string.Format("File \"{0}\" download finished.", 
        e.TorrentFile.Path));
      Torrent torrent = e.TorrentManager.Torrent;
      // Add a file to the torrent.
      (torrent.Files[1] as TorrentFile).Priority = Priority.Normal;
      (torrent.Files[2] as TorrentFile).Priority = Priority.Normal;
      Debug.WriteLine(string.Format("Added two file in the downloading list."));
    }

    static void torrentManager_TorrentStateChanged(object sender, 
      TorrentStateChangedEventArgs e) {
      Debug.WriteLine(string.Format("Torrent state changed {0} => {1}", 
        e.OldState, e.NewState));
      
      if (e.NewState == TorrentState.Seeding) {
        if (e.OldState == TorrentState.Downloading) {
          Debug.WriteLine("Entire torrent download completed");
        }
      }
    }
  }
}
