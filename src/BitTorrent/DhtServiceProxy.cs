using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Brunet.Rpc;
using Brunet.DistributedServices;
using System.Diagnostics;
using MonoTorrent.Tracker;
using Brunet;
using Fushare.Services;

namespace Fushare.BitTorrent {
  /// <summary>
  /// A proxy of the DHT service that handles the BitTorrent related work.
  /// </summary>
  public class DhtServiceProxy {
    #region Fields
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(DhtServiceProxy));
    BrunetDht _dht;
    IIntervalAlgorithm _interval_alg; 
    #endregion

    public DhtServiceProxy(BrunetDht dht) {
      _dht = dht;
      _interval_alg = new StaticIntervalAlgorithm();
    }

    /// <summary>
    /// Gets peers from DHT.
    /// </summary>
    /// <param name="infoHash">The infoHash of the torrent, used as the key in Dht</param>
    /// <returns>
    /// A List of PeerEntries which could have duplicated peers with different 
    /// states. Empty List if no peers for this infoHash
    /// </returns>
    public ICollection<PeerEntry> GetPeers(byte[] infoHash) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Getting peers for infoHash: {0} (Base32)", Base32.Encode(infoHash)));
      ICollection<PeerEntry> peers = new List<PeerEntry>();
      //Firing DHT Get
      DhtGetResult[] results = (DhtGetResult[])_dht.Get(infoHash);
      Logger.WriteLineIf(LogLevel.Info, _log_props,
          string.Format("{0} peer(s) retrieved from DHT", results.Length));
      int index = 0;
      foreach (DhtGetResult r in results) {
        try {
          PeerEntry entry = (PeerEntry)DictionaryData.CreateDictionaryData(r.value);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("Peer entry #{0} built:\n{1}", index++, entry.ToString()));
          peers.Add(entry);
        } catch (Exception e) {
          Logger.WriteLineIf(LogLevel.Error, _log_props,
              "Error when Deserializing result from DHT", e);
          // Ignore this entry and continue to parse others.
          continue;
        }
      }
      return peers;
    }

    /// <summary>
    /// Announces Peer to DHT with AnnounceParameters infomation.
    /// </summary>
    /// <returns>True if successful</returns>
    public bool AnnouncePeer(byte[] infoHash, AnnounceParameters pars) {
      PeerEntry entry = new PeerEntry(pars);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Peer to be announced to DHT:\n{0}", entry.ToString()));
      return AnnouncePeer(infoHash, entry);
    }

    /// <summary>
    /// Annouces Peer to DHT.
    /// </summary>
    public bool AnnouncePeer(byte[] infoHash, PeerEntry peer) {
      // Firing DHT Put
      byte[] peer_bytes = peer.SerializeTo();
      bool succ = _dht.Put(infoHash, peer_bytes, _interval_alg.Interval);
      Logger.WriteLineIf(LogLevel.Info, _log_props,
          string.Format("{0} peer to DHT", succ ? "Successfully announced" : "Failed to be announced"));
      return succ;
    }
  }
}
