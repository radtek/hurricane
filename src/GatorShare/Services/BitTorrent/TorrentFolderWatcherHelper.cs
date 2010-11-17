using System;
using System.Collections;
using System.Text;
using System.Diagnostics;

using MonoTorrent.TorrentWatcher;
using MonoTorrent.Client;
using MonoTorrent.Common;

namespace GatorShare.Services.BitTorrent {
  /// <summary>
  /// Watches file system and read torrent files into system.
  /// </summary>
  class TorrentFolderWatcherHelper {
    private static IDictionary _log_props = 
      Logger.PrepareLoggerProperties(typeof(TorrentFolderWatcherHelper));

    public static void SetupTorrentWatcher(BitTorrentManager manager, 
      string torrentsDir) {
      TorrentFolderWatcher watcher = 
        new TorrentFolderWatcher(torrentsDir, "*.torrent");
      watcher.TorrentFound += delegate(object sender, TorrentWatcherEventArgs e) {
        try {
          // This is a hack to work around the issue where a file triggers the event
          // before it has finished copying. As the filesystem still has an exclusive lock
          // on the file, monotorrent can't access the file and throws an exception.
          // The best way to handle this depends on the actual application. 
          // Generally the solution is: Wait a few hundred milliseconds
          // then try load the file.
          System.Threading.Thread.Sleep(500);

          Torrent t = Torrent.Load(e.TorrentPath);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Torrent file {1} at {0} loaded", e.TorrentPath, t.Name));

          manager.AddTorrentToTracker(t);
        } catch (Exception ex) {
          Logger.WriteLineIf(LogLevel.Error, _log_props,
            string.Format("Error loading torrent from disk: {0}", ex.ToString()));
        }
      };

      watcher.Start();
      // We have two levels in the torrents directory.
      // The FileSystemWatcher is instantiated in Start().
      watcher.FileSystemWatcher.IncludeSubdirectories = true;
      watcher.ForceScan();
    }
  }
}
