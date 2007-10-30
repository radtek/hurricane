using System;
using System.Collections.Generic;
using System.Text;

namespace FuseSolution.Tracker {
  class DhtTorrentManager : MonoTorrent.Tracker.ITorrentManager {

    #region ITorrentManager Members

    public void Add(MonoTorrent.Tracker.AnnounceParameters par) {
      throw new Exception("The method or operation is not implemented.");
    }

    /**
     * counts the peers downloading/uploading this torrent.
     */
    public int Count {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    /**
     * counts all seeders.
     */
    public int CountComplete {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    /**
     * return how often the torrent was fully downloaded.
     */
    public int Downloaded {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    /**
     * this method is used for computing the list of peers which share this torrent
     * @return if par.compact is true then the value is a BEncodedString otherwise it's a BEncodedDictionary 
     */
    public MonoTorrent.BEncoding.BEncodedValue GetPeersList(MonoTorrent.Tracker.AnnounceParameters par) {
      throw new Exception("The method or operation is not implemented.");
    }

    /**
     * this method returns the scrape entry for this torrent
     */
    public MonoTorrent.BEncoding.BEncodedDictionary GetScrapeEntry() {
      throw new Exception("The method or operation is not implemented.");
    }

    /**
     * removes the peer with par.ip and par.port
     */
    public void Remove(MonoTorrent.Tracker.AnnounceParameters par) {
      throw new Exception("The method or operation is not implemented.");
    }

    /**
     * identifies the torrent which we are managing
     */
    public MonoTorrent.Common.Torrent Torrent {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    /**
     * update the internale torrent datas and the peer with par.ip and par.port
     */
    public void Update(MonoTorrent.Tracker.AnnounceParameters par) {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion
  }
}
