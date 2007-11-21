using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Ipop;
using Brunet.Dht;
using FuseSolution.Common;
using MonoTorrent.Tracker;

namespace FuseSolution.Tracker {
  class DhtServiceLocator {
    private static IDictionary<DhtType, DhtServiceProxy> _dhts = new Dictionary<DhtType, DhtServiceProxy>();

    public static DhtServiceProxy GetDhtServiceProxy(DhtType dhtType) {
      switch (dhtType) {
        case DhtType.Local:
          if (!_dhts.ContainsKey(DhtType.Local)) {
            IDht dht = new LocalHT();
            _dhts[DhtType.Local] = new DhtServiceProxy(dht);
          }
          return _dhts[DhtType.Local];
        case DhtType.BrunetDht:
        default:
          if (!_dhts.ContainsKey(DhtType.BrunetDht)) {
            IDht dht = Ipop.DhtServiceClient.GetSoapDhtClient();
            _dhts[DhtType.BrunetDht] = new DhtServiceProxy(dht);
          }
          return _dhts[DhtType.BrunetDht];
      }
    }
  }
  
  class DhtServiceProxy {
    IDht _dht;
    IIntervalAlgorithm _interval_alg;

    public DhtServiceProxy(IDht dht) {
      _dht = dht;
      _interval_alg = new StaticIntervalAlgorithm();
    }

    /**
     * @param infoHash The infoHash of the torrent, used as the key in Dht
     * @return A List of PeerEntries which could have duplicated peers w/ different states. Empty List if no peers for this infoHash
     */
    public ICollection<PeerEntry> GetPeers(byte[] infoHash) {
      ICollection<PeerEntry> peers = new List<PeerEntry>();
      DhtGetResult[] results = _dht.Get(Encoding.UTF8.GetString(infoHash));
      foreach (DhtGetResult r in results) {
        PeerEntry entry = new PeerEntry(r.value);
        peers.Add(entry);
      }
      return peers;
    }

    public bool AnnouncePeer(byte[] infoHash, AnnounceParameters pars) {
      return AnnouncePeer(infoHash, new PeerEntry(pars));
    }

    public bool AnnouncePeer(byte[] infoHash, PeerEntry peer) {
      return _dht.Put(Encoding.UTF8.GetString(infoHash), peer.Serialize(), (int)_interval_alg.Interval);
    }
  }
}
