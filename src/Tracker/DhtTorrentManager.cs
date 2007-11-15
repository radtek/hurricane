using System;
using System.Collections.Generic;
using System.Text;
using MonoTorrent.Common;
using Ipop;

namespace FuseSolution.Tracker {
  class DhtTorrentManager : ITorrentManager {
    IDht _dht;

    public DhtTorrentManager(Torrent torrent, IDht dht) {
      _dht = dht;
    }



    #region ITorrentManager Members

    public MonoTorrent.Common.Torrent Torrent {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public int CountComplete {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public int Downloaded {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public int Count {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public MonoTorrent.BEncoding.BEncodedValue GetPeersList(AnnounceParameters par) {
      throw new Exception("The method or operation is not implemented.");
    }

    public MonoTorrent.BEncoding.BEncodedDictionary GetScrapeEntry() {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Add(AnnounceParameters par) {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Remove(AnnounceParameters par) {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Update(AnnounceParameters par) {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion
  }
}
