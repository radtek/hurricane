using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Ipop;
using Brunet.Dht;
using System.Diagnostics;
using FuseSolution.Common;
using MonoTorrent.Tracker;
using Brunet;

namespace FuseSolution.Tracker {
  class DhtServiceLocator {
    private static IDictionary<DhtType, DhtServiceProxy> _dhts = new Dictionary<DhtType, DhtServiceProxy>();

    /**
     * @param port: port on localhost that dht service is running at.
     */
    public static DhtServiceProxy GetDhtServiceProxy(DhtType dhtType, int port) {
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
            IDht dht = Ipop.DhtServiceClient.GetXmlRpcDhtClient(port);
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
      Debug.WriteLineIf(Logger.TrackerLog.TraceVerbose, 
          string.Format("Getting peers for infoHash:\t{0} (Base32)", Base32.Encode(infoHash)));
      ICollection<PeerEntry> peers = new List<PeerEntry>();
      //Firing DHT Get
      DhtGetResult[] results = _dht.Get(Encoding.UTF8.GetString(infoHash));
      Debug.WriteLineIf(Logger.TrackerLog.TraceInfo, 
          string.Format("{0} peer(s) retrieved from DHT", results.Length));
      int index = 0;
      foreach (DhtGetResult r in results) {
        try {
          PeerEntry entry = new PeerEntry(r.value);
          Debug.WriteLineIf(Logger.TrackerLog.TraceVerbose, string.Format("Peer entry {0} built:\n{1}", index++,entry.ToString()));
          peers.Add(entry);
        } catch (Exception e) {
          //Deserliazation error
          Debug.WriteLineIf(Logger.TrackerLog.TraceError, "Error when Deserializing result from DHT");
          Debug.WriteLineIf(Logger.TrackerLog.TraceError, e);
          continue;
        }
      }
      return peers;
    }

    public bool AnnouncePeer(byte[] infoHash, AnnounceParameters pars) {
      PeerEntry entry = new PeerEntry(pars);
      Debug.WriteLineIf(Logger.TrackerLog.TraceVerbose, string.Format("Peer to be announced:\n{0}", entry.ToString()));
      return AnnouncePeer(infoHash, entry);
    }

    public bool AnnouncePeer(byte[] infoHash, PeerEntry peer) {
      //Firing DHT Put
      bool succ = _dht.Put(Encoding.UTF8.GetString(infoHash), peer.Serialize(), (int)_interval_alg.Interval);
      Console.WriteLine(string.Format("{0} peer to DHT", succ ? "Successfully announced" : "Failed to announce"));
      return succ;
    }
  }
}
