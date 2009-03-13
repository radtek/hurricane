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

namespace Fushare.Services.BitTorrent {
  /// <summary>
  /// A proxy of the DHT service that handles the BitTorrent related work.
  /// </summary>
  public class DhtProxy {
    #region Fields
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(DhtProxy));
    DhtBase _dht;
    public int PeerTtl { get; private set; }
    #endregion

    /// <summary>
    /// Instantiates DhtProxy with BrunetDht.
    /// </summary>
    /// <param name="dht"></param>
    public DhtProxy(DhtBase dht, int peerTtl) {
      _dht = dht;
      PeerTtl = peerTtl;
    }

    #region Tracker Operations
    /// <summary>
    /// Gets peers from DHT.
    /// </summary>
    /// <param name="infoHash">The infoHash of the torrent, used as the name in Dht</param>
    /// <returns>
    /// A List of PeerEntries which could have duplicated peers with different 
    /// states. Empty List if no peers for this infoHash or the network communication
    /// is temporarily down.
    /// </returns>
    public ICollection<PeerEntry> GetPeers(byte[] infoHash) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Getting peers for infoHash: {0} (Base32)", Base32.Encode(infoHash)));
      ICollection<PeerEntry> peers = new List<PeerEntry>();
      // Fire DHT Get
      DhtResults results;
      try {
        results = _dht.Get(infoHash);
      } catch (ResourceException ex) {
        Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
          "Exception caught when retrieving peers. \n{0}", ex));
        // No big deal. We simply wait until the next time to query again.
        // Return empty collection.
        return peers;
      }

      Logger.WriteLineIf(LogLevel.Info, _log_props,
          string.Format("{0} peer(s) retrieved from DHT", results.ResultEntries.Count));
      int index = 0;
      foreach (var r in results.ResultEntries) {
        try {
          PeerEntry entry = (PeerEntry)DictionaryData.CreateDictionaryData(r.Value);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("Peer entry #{0} built:\n{1}", index++, entry.ToString()));
          peers.Add(entry);
        } catch (Exception e) {
          Logger.WriteLineIf(LogLevel.Error, _log_props,
              "Error occurred when deserializing result from DHT", e);
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
    public void AnnouncePeer(byte[] infoHash, AnnounceParameters pars) {
      PeerEntry entry = new PeerEntry(pars);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Peer to be announced to DHT:\n{0}", entry.ToString()));
      AnnouncePeer(infoHash, entry);
    }

    /// <summary>
    /// Annouces Peer to DHT.
    /// </summary>
    public void AnnouncePeer(byte[] infoHash, PeerEntry peer) {
      // Firing DHT Put
      byte[] peer_bytes = peer.SerializeTo();
      try {
        _dht.Put(infoHash, peer_bytes, PeerTtl);
      } catch (DhtException ex) {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
          string.Format("Unable to announce peer to DHT. We can try next time. \n{0}", ex));
        return;
      }
      Logger.WriteLineIf(LogLevel.Info, _log_props,
          string.Format("Successfully announced peer to DHT"));
    }
    #endregion

    #region Torrent Operations
    /// <summary>
    /// Puts torrent file value to DHT.
    /// </summary>
    public void PutTorrent(byte[] dhtKey, byte[] torrent, int ttl) {
      _dht.Put(dhtKey, torrent, ttl);
    }

    public void CreateTorrent(byte[] dhtKey, byte[] torrent, int ttl) {
      _dht.Create(dhtKey, torrent, ttl);
    }

    /// <summary>
    /// Gets torrent file value from DHT.
    /// </summary>
    /// <remarks>The value could be null if the key doesn't exist.</remarks>
    public byte[] GetTorrent(byte[] dhtKey) {
      //DhtGetResult torrentDgr = _dht.GetOneDatum(
      //  dhtKey, true, BrunetDht.OneDatumMode.LastOne);
      //return torrentDgr.value;
      DhtResults results = _dht.Get(dhtKey);
      return results.Value;
    }
    #endregion
  }
}
