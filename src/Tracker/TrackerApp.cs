//
// Main.cs
//
// Authors:
//   Gregor Burger burger.gregor@gmail.com
//
// Copyright (C) 2006 Gregor Burger
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Collections;
using System.Web;
using System.Net;

using MonoTorrent.Tracker;
using MonoTorrent.Common;
using FuseSolution.Common;

#if TRACKER_NUNIT
using NUnit.Framework;
#endif


namespace FuseSolution.Tracker {
  using Tracker = MonoTorrent.Tracker.Tracker;

  /// <summary>
  /// This is a sample implementation of how you could create a custom ITrackable
  /// </summary>
  public class CustomITrackable : ITrackable {
    // I just want to keep the TorrentFiles in memory when i'm tracking the torrent, so i store
    // a reference to them in the ITrackable. This allows me to display information about the
    // files in a GUI without having to keep the entire (really really large) Torrent instance in memory.
    private TorrentFile[] files;

    // We require the infohash and the name of the torrent so the tracker can work correctly
    private byte[] infoHash;
    private string name;

    public CustomITrackable(Torrent t) {
      // Note: I'm just storing the files, infohash and name. A typical Torrent instance
      // is ~100kB in memory. A typical CustomITrackable will be ~100 bytes.
      files = t.Files;
      infoHash = t.InfoHash;
      name = t.Name;
    }

    /// <summary>
    /// The files in the torrent
    /// </summary>
    public TorrentFile[] Files {
      get { return files; }
    }

    /// <summary>
    /// The infohash of the torrent
    /// </summary>
    public byte[] InfoHash {
      get { return infoHash; }
    }

    /// <summary>
    /// The name of the torrent
    /// </summary>
    public string Name {
      get { return name; }
    }
  }

  class MySimpleTracker {
    #region Fields
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(MySimpleTracker));

    MonoTorrent.Tracker.Tracker tracker;
    TorrentFolderWatcher watcher;
    const string TORRENT_DIR = "Torrents";
    ListenerBase _listener;
    int _interval = 20; //in seconds 
    #endregion

    #region Constructors
    ///<summary>Start the Tracker. Start Watching the TORRENT_DIR Directory for new Torrents.</summary>
    public MySimpleTracker(IDictionary options)
    {
      /* The following code not necessary when found a way to set DhtListener listen to all the prefixes
      System.Net.IPEndPoint listenpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 10000);
      Console.WriteLine("Listening at: {0}", listenpoint);
      IPAddress[] addrs = null;
      try {
        addrs = Dns.GetHostAddresses(Dns.GetHostName());
        if (addrs == null || addrs.Length == 0) {
          throw new Exception();
        }
      } catch (Exception e) {
        Debug.WriteLineIf(Logger.TrackerLog.TraceError, e);
        addrs = new IPAddress[1];
        addrs[0] = IPAddress.Loopback;
      }
      IList<string> prefixes = new List<string>();
      foreach (IPAddress addr in addrs) {
        string prefix = string.Format("http://{0}:{1}/", addr.ToString(), 10000);
        prefixes.Add(prefix);
        Debug.WriteLineIf(Logger.TrackerLog.TraceInfo, string.Format("Listening to {0}", prefix));
      }
      ListenerBase listener = new DhtListener(prefixes, t);
      */

      _listener = new DhtListener((int)options["tracker_port"], (DhtType)options["dht_type"],
          (int)options["dht_port"]);
      _interval = (int)options["interval"];
      tracker = new Tracker();
      tracker.RegisterListener(_listener);
      SetupTorrentWatcher();
      StartTracker();
    } 
    #endregion

    public void StartTracker() {
      _listener.Start();
      watcher.StartWatching();
      watcher.ForceScan();
      while (true) {
        lock (tracker)
          foreach (SimpleTorrentManager m in tracker) {
            Console.WriteLine("Name: {0}", m.Trackable.Name);
            Console.WriteLine("Complete: {1}   Incomplete: {2}   Downloaded: {0}", m.Downloaded, m.Complete, m.Count - m.Complete);
            Console.WriteLine();
            System.Threading.Thread.Sleep(_interval * 1000);
          }
      }
    }

    private void SetupTorrentWatcher() {
      watcher = new TorrentFolderWatcher(Path.GetFullPath(TORRENT_DIR), "*.torrent");
      watcher.TorrentFound += delegate(object sender, TorrentWatcherEventArgs e) {
        try {
          Torrent t = Torrent.Load(e.TorrentPath);
          // There is also a predefined 'InfoHashTrackable' MonoTorrent.Tracker which
          // just stores the infohash and name of the torrent. This is all that the tracker
          // needs to run. So if you want an ITrackable that "just works", then use InfoHashTrackable.
          ITrackable trackable = new CustomITrackable(t);
          lock (tracker)
            tracker.Add(trackable);
        } catch (Exception ex) {
          Debug.WriteLine("Error loading torrent from disk: {0}", ex.Message);
          Debug.WriteLine("Stacktrace: {0}", ex.ToString());
        }
      };
    }

    void watcher_TorrentLost(object sender, TorrentWatcherEventArgs e) {
      //try
      //{
      //    TrackerEngine.Instance.Tracker.Remove(e.TorrentPath);
      //}
      //catch(Exception ex)
      //{
      //    Console.WriteLine("Couldn't remove torrent: {0}", e.TorrentPath);
      //    Console.WriteLine("Reason: {0}", ex.Message);
      //}
    }

    void watcher_TorrentFound(object sender, TorrentWatcherEventArgs e) {
      //try
      //{
      //    Torrent t = Torrent.Load(e.TorrentPath);
      //    TrackerEngine.Instance.Tracker.Add(t);
      //}
      //catch (Exception ex)
      //{
      //    Console.WriteLine("Couldn't load {0}.", e.TorrentPath);
      //    Console.WriteLine("Reason: {0}", ex.Message);
      //}
    }

    public void OnProcessExit(object sender, EventArgs e) {
      //Console.Write("shutting down the Tracker...");
      //TrackerEngine.Instance.Stop();
      //Console.WriteLine("done");
    }

    public static void Main(string[] args) {
      //default values
      DhtType t = DhtType.BrunetDht;
      int tracker_port = 10000;
      int dht_port = 51515;
      int interval = 20;
      string l4n_config = null;

      for (int i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "-l4n":
            if (i == args.Length - 1) {
              //no next value
              Console.Error.WriteLine("No l4n config specified");
              return;
            }
            l4n_config = args[++i];
            break;
          case "-l":
            t = DhtType.Local;
            break;
          case "-tp":
            if (i == args.Length - 1) {
              //no next value
              Console.Error.WriteLine("No tracker port specified");
              return;
            }
            if (!Int32.TryParse(args[++i], out tracker_port)) {
              Console.Error.WriteLine("Invalid tracker port");
              return;
            }
            break;
          case "-dp":
            if (i == args.Length - 1) {
              //no next value
              Console.Error.WriteLine("No dht service specified");
              return;
            }
            if (!Int32.TryParse(args[++i], out dht_port)) {
              Console.Error.WriteLine("Invalid dht service port");
              return;
            }
            break;
          case "-i":
            if (i == args.Length - 1) {
              //no next value
              Console.Error.WriteLine("No interval specified");
              return;
            }
            if (!Int32.TryParse(args[++i], out interval)) {
              Console.Error.WriteLine("Invalid dht service port");
              return;
            }
            break;
          default:
            Console.Error.WriteLine("Invalid arguments");
            return;
        }
      }

      if (string.IsNullOrEmpty(l4n_config)) {
        Logger.LoadConfig();
      } else {
        Logger.LoadConfig(l4n_config);
      }
      //Debug.WriteLine(string.Format("Starting DhtTracker FrontendEngine at port: {0}", tracker_port));
      Logger.WriteLineIf(LogLevel.Info, _log_props, string.Format("Starting DhtTracker FrontendEngine at port: {0}", tracker_port));
      IDictionary c = new ListDictionary();
      c.Add("dht_type", t);
      c.Add("tracker_port", tracker_port);
      c.Add("dht_port", dht_port);
      c.Add("interval", interval);
      MySimpleTracker tracker = new MySimpleTracker(c);
      tracker.StartTracker();
    }
  }
}