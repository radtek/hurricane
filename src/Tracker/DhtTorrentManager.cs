using System;
using System.Collections.Generic;
using System.Text;
using MonoTorrent.Common;
using MonoTorrent.Tracker;
using Ipop;
using System.Diagnostics;
using MonoTorrent.BEncoding;

namespace FuseSolution.Tracker {
  class DhtTorrentManager : MonoTorrent.Tracker.ITorrentManager {
    #region Fields
    private Torrent torrent;
    private IDht _dht;
    private PeerManager peers;
    private int complete = 0;
    private int downloaded;
    #endregion

    #region Properties
    /**
     * identifies the torrent which we are managing
     */
    public Torrent Torrent {
      get {
        return torrent;
      }
    }

    /**
     * Counts the peers downloading/uploading this torrent.
     */
    public int Count {
      get {
        return peers.Count;
      }
    }

    /**
     * Number of peers with the entire file
     */
    public int CountComplete {
      get {
        return complete;
      }
    }

    /**
     * Total number of times the tracker has registered a completion
     */
    public int Downloaded {
      get {
        return downloaded;
      }
    }

    #endregion

    #region Constructors
    public DhtTorrentManager(Torrent torrent) {
      this.torrent = torrent;
      peers = new PeerManager();
    }

    public DhtTorrentManager(Torrent torrent, IDht dht) {
      this.torrent = torrent;
      this._dht = dht;
    } 
    #endregion

    #region Public Methods

    public void Add(MonoTorrent.Tracker.AnnounceParameters par) {
      string key = Peer.GetKey(par);
      Debug.WriteLine("adding peer: " + par.ip + ":" + par.port);

      if (peers.Contains(key)) {
        Debug.WriteLine("peer already in there. maybe the client restarted?");
        peers.Remove(key);
      }

      Peer peer = new Peer(par, new System.Threading.TimerCallback(PeerTimeout));

      peers.Add(peer);

      if (peer.IsCompleted) {
        System.Threading.Interlocked.Increment(ref complete);
        System.Threading.Interlocked.Increment(ref downloaded);
      }
    }

    /**
     * removes the peer with par.ip and par.port
     */
    public void Remove(MonoTorrent.Tracker.AnnounceParameters par) {
      string key = Peer.GetKey(par);
      Debug.WriteLine("removing: |" + key + "|");
      peers.Remove(key);
    }

    /**
     * this method is used for computing the list of peers which share this torrent
     * @return if par.compact is true then the value is a BEncodedString otherwise it's a BEncodedDictionary 
     */
    public MonoTorrent.BEncoding.BEncodedValue GetPeersList(MonoTorrent.Tracker.AnnounceParameters par) {
      if (par.compact) {
        return GetCompactList(par);
      } else {
        return GetNonCompactList(par);
      }
    }

    /**
     * this method returns the scrape entry for this torrent
     */
    public MonoTorrent.BEncoding.BEncodedDictionary GetScrapeEntry() {
      BEncodedDictionary dict = new BEncodedDictionary();

      dict.Add("complete", new BEncodedNumber(CountComplete));
      dict.Add("downloaded", new BEncodedNumber(Downloaded));
      dict.Add("incomplete", new BEncodedNumber(Count - CountComplete));
      if (!torrent.Equals(String.Empty))
        dict.Add("name", new BEncodedString(torrent.Name));

      return dict;
    }

    /**
     * update the internale torrent datas and the peer with par.ip and par.port
     */
    public void Update(MonoTorrent.Tracker.AnnounceParameters par) {
      string key = Peer.GetKey(par);
      Debug.WriteLine("updating peer: " + par.ip + ":" + par.port);
      if (!peers.Contains(key)) {
        Add(par);
        Console.Error.WriteLine("warning: Peer not managed. If you restarted the Tracker ignore this message");
        return;
      }
      Peer peer = peers.Get(key);

      if (par.@event.Equals(TorrentEvent.Completed)) {
        System.Threading.Interlocked.Increment(ref complete);
        System.Threading.Interlocked.Increment(ref downloaded);
      }

      peer.Update(par);
    }

    #endregion

    #region Private Methods
    //TODO refactor - done not debuged
    private BEncodedValue GetCompactList(AnnounceParameters par) {
      Peer exclude = null;
      if (peers.Contains(Peer.GetKey(par))) {
        exclude = peers.Get(Peer.GetKey(par));
      }
      List<Peer> randomPeers = peers.GetRandomPeers(par.numberWanted, exclude);
      byte[] peersBuffer = new byte[randomPeers.Count * 6];
      int offset = 0;
      Debug.WriteLine("Number of peers returned: " + randomPeers.Count);
      foreach (Peer each in randomPeers) {
        byte[] entry = each.CompactPeersEntry;
        Array.Copy(entry, 0, peersBuffer, offset, entry.Length);
        offset += entry.Length;
      }
//    Debug.WriteLine("stream.length: "+stream.Length);
//    Debug.WriteLine("stream.buffer.length: "+stream.GetBuffer().Length);
//    Debug.Assert(stream.GetBuffer().Length == stream.Length);
      return new BEncodedString(peersBuffer);
    }

    //TODO refactor: done - not debuged
    private BEncodedValue GetNonCompactList(AnnounceParameters par) {
      Peer exclude = peers.Get(Peer.GetKey(par));
      List<Peer> randomPeers = peers.GetRandomPeers(par.numberWanted, exclude);
      BEncodedList announceList = new BEncodedList(randomPeers.Count);

      foreach (Peer each in randomPeers) {
        announceList.Add(each.PeersEntry);
      }

      return announceList;
    }

    //this is the handle from the peer timer. it is called when the peer is not responding anymore.
    //if this is the case we remove em to save memory. this is neccesary if peers shut down but do 
    //not send the finished update. 
    private void PeerTimeout(object peer) {
      Debug.WriteLine("peer is not updating anymore");
      Peer p = peer as Peer;

      if (p == null) {
        throw new ArgumentException("not a Peer instance", "peer");
      }

      Remove(p);
    }
    #endregion
  }
}
