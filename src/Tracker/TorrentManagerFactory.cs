using System;
using System.Collections.Generic;
using System.Text;
using MonoTorrent.Tracker;
using MonoTorrent.Common;
using Ipop;
using FuseSolution.Common;

namespace FuseSolution.Tracker {
  class TorrentManagerFactory {
    
    public enum TrackerType {
      Local, Dht, Simple
    }

    public static ITorrentManager GetTorrentManager(TrackerType t, Torrent torrent) {
      switch (t) {
        case TrackerType.Local:
          IDht dht = new LocalHT();
          return new DhtTorrentManager(torrent, dht);
        case TrackerType.Dht:
          //TODO
          return null;
        case TrackerType.Simple:
          return new SimpleTorrentManager(torrent);
        default:
          throw new ArgumentException("Invalid TrackerType");
      }
    }
  }
}
