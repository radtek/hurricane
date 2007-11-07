using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using MonoTorrent.Common;
using MonoTorrent.BEncoding;
using MonoTorrent.Tracker;
using Ipop;

namespace FuseSolution.Tracker {
  class DhtTracker {
    #region Fields
    /**
     * We might need this to cache the torrents information intead of going to DHT every time
     */
    private Dictionary<string, ITorrentManager> torrents;

    /**
     * Singleton instance
     */
    private static DhtTracker _instance = null;
    private bool allow_non_compact = true;
    private IIntervalAlgorithm interval_algorithm = new StaticIntervalAlgorithm();

    #endregion
    /**
     * Private ctor used by Instance
     */
    private DhtTracker() {
      torrents = new Dictionary<string, ITorrentManager>();
    }

    #region Properties

    /**
     * Singleton returning an instance of the Tracker class
     */
    public static DhtTracker Instance {
      get {
        if (_instance == null) {
          _instance = new DhtTracker();
        }
        return _instance;
      }
    }

    /**
     * AllowNonCompact allows or denie requests in compact or 
     * non compact format default is true
     */
    public bool AllowNonCompact {
      get {
        return allow_non_compact;
      }
      set {
        allow_non_compact = value;
      }
    }

    /**
     * Get and set the IntervalAlgorithm used by this Tracker
     */
    public IIntervalAlgorithm IntervalAlgorithm {
      get {
        return interval_algorithm;
      }
      set {
        interval_algorithm = value;
      }
    }
    #endregion

    /**
     * @param: The torrent which should be added. If it is already in the List the Method returns immidiatly.
     */
    public void AddTorrent(Torrent torrent) {
      //Same as Tracker.AddTorrent except that we use DhtTorrentManager instead of SimpleTorrentManager
      Console.WriteLine("Adding torrent " + Toolbox.GetHex(torrent.InfoHash) + " to the tracker");
      if (torrents.ContainsKey(Toolbox.GetHex(torrent.InfoHash))) {
        Console.WriteLine("torrent already added");//TODO remove
        return;
      }
      torrents.Add(Toolbox.GetHex(torrent.InfoHash), new DhtTorrentManager(torrent));
    }

    /**
     * This Method is used to Disable Torrents.
     * @param: The Torrent to be removed from the Tracker
     */
    public void RemoveTorrent(Torrent torrent) {
      //We might not need to do this in DHT
    }

    /**
     * Not used in here
     */
    public void RemoveTorrent(string path) {
    }

    /**
     * This Method is called by the Frontend if a Peer called the announc URL
     * In DhtTracker, the peer would only be the local client
     */
    public void Announce(AnnounceParameters par, Stream stream) {
      //some pre checks
      if (!torrents.ContainsKey(Toolbox.GetHex(par.infoHash))) {
        throw new TrackerException("Torrent not Registered at this Tracker");
      }

      if (!AllowNonCompact && par.compact) {
        throw new TrackerException("Tracker does not allow Non Compact Format");
      }

      //DhtTorrentManager
      ITorrentManager torrent = torrents[Toolbox.GetHex(par.infoHash)];

      switch (par.@event) {
        case TorrentEvent.Completed:
          //In a DHT sense, currently we do nothing different from None
          torrent.Update(par);
          break;
        case TorrentEvent.Stopped:
          //The local client stopped, it should has no impact on DHT
          torrent.Remove(par);
          IntervalAlgorithm.PeerRemoved();
          //Alan said do nothing, me agrees
          //Debug.WriteLine("removed peer and do nothing");
          //return;
          break;
        case TorrentEvent.Started:
          //The local client is a new guy, we add torrent to our management. 
          //This serves as a cache function of the local tracker.
          torrent.Add(par);
          IntervalAlgorithm.PeerAdded();
          break;
        case TorrentEvent.None:
          //Just retrieve the data from DHT and also add the client to DHT
          torrent.Update(par);
          break;
        default:
          throw new TorrentException("unknown announce event");
      }

      //write response
      byte[] encData = GetAnnounceResponse(par).Encode();
      stream.Write(encData, 0, encData.Length);

      WriteResult("announce", encData);
    }

    #region Private Methods

    [Conditional("DEBUG")]
    private void WriteResult(string prefix, byte[] encData) {
      string tmpPath = Path.GetTempFileName();
      using (FileStream tmpFile = new FileStream(tmpPath, FileMode.Open)) {
        tmpFile.Write(encData, 0, encData.Length);
        Debug.WriteLine(prefix + " return written to: " + tmpPath);
      }
    }

    /**
     * Precondition: here we should already have the peer information in place
     */
    private BEncodedDictionary GetAnnounceResponse(AnnounceParameters par) {
      ITorrentManager torrentManager = torrents[Toolbox.GetHex(par.infoHash)];
      BEncodedDictionary dict = new BEncodedDictionary();

      Debug.WriteLine(torrentManager.Count);

      dict.Add("complete", new BEncodedNumber(torrentManager.CountComplete));
      dict.Add("incomplete", new BEncodedNumber(torrentManager.Count - torrentManager.CountComplete));
      dict.Add("interval", new BEncodedNumber((int)IntervalAlgorithm.Interval));

      dict.Add("peers", torrentManager.GetPeersList(par));

      dict.Add("min interval", new BEncodedNumber((int)IntervalAlgorithm.MinInterval));

      if (par.trackerId == null)//FIXME is this the right behaivour 
        par.trackerId = "monotorrent-tracker";
      dict.Add("tracker id", new BEncodedString(par.trackerId));

      return dict;
    }
    #endregion

  }

  public class TrackerException : Exception {
    internal TrackerException(string message)
      : base(message) { }
  }
}
