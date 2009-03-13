using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Web;
using System.Net;

using MonoTorrent;
using MonoTorrent.Tracker.Listeners;
using MonoTorrent.Tracker;
using MonoTorrent.BEncoding;

namespace Fushare.Services.BitTorrent {
  /// <summary>
  /// Listens to nothing but function calls from DhtTracker.
  /// </summary>
  public class DhtListener : ManualListener {
    #region Fields
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(DhtListener));
    private DhtProxy _proxy;
    #endregion Fields

    #region Constructors

    public DhtListener(DhtProxy proxy) {
      _proxy = proxy;
    }

    #endregion Constructors

    #region Methods

    /// <summary>
    /// Gets the peers from DHT and generates AnnounceParameters of a list of 
    /// peers and puts the peer info of this announcing peer to DHT
    /// </summary>
    /// <param name="parameters">
    /// AnnounceParameters from the requesting client. It's modified in method.
    /// </param>
    internal void HandleAnnounceRequest(AnnounceParameters parameters) {
      ICollection<PeerEntry> entries = _proxy.GetPeers(parameters.InfoHash);
      foreach (PeerEntry entry in entries) {
        AnnounceParameters par = GenerateAnnounceParameters(
          parameters.InfoHash, entry);
        if (par.IsValid) {
          // Tracker will write to the par.Response but we don't use it
          // ListenerBase.Handle use par.ClientAddress to generate 
          // AnnounceParameters again.
          Handle(par.Parameters, par.ClientAddress.Address, false);
        } else {
          Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
            "Parameters invalid!"));
          continue;
        }

        //Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        //    string.Format("Tracker's response for this peer {0} from DHT: {1}",
        //    par.ClientAddress, par.Response.ToString()));
      }

      // Got all I need, now announce myself to DHT.
      try {
        _proxy.AnnouncePeer(parameters.InfoHash, parameters);
      } catch (DhtException ex) {
        // It is OK to temporarily unable to announce to Dht. We can try the next time.
        Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
          "Unable to reach Dht. We can try the next time.\n{0}", ex));
      }

      Logger.WriteLineIf(LogLevel.Info, _log_props,
        string.Format("DhtListener finished handling annoucement from {0}", 
        parameters.RemoteAddress));
    }

    /// <summary>
    /// Generates the AnnounceParameters from PeerEntry
    /// </summary>
    private AnnounceParameters GenerateAnnounceParameters(byte[] infoHash, PeerEntry entry) {
      NameValueCollection c = new NameValueCollection();
      // "info_hash", "peer_id", "port", "uploaded(bytes)", "downloaded", "left", "compact"
      // InfoHash here should be just like what's in HttpRequests
      c.Add("info_hash", System.Web.HttpUtility.UrlEncode(infoHash));
      c.Add("peer_id", entry.PeerID);
      c.Add("port", entry.PeerPort.ToString());
      // Fake the mandatory fields, these are solely used to compute the upload/download speed
      c.Add("uploaded", "0");
      c.Add("downloaded", "0");
      c.Add("left", "1000");
      c.Add("compact", "1");
      // Optional but needs to set for monotorrent tracker
      if (entry.PeerState != MonoTorrent.Common.TorrentEvent.None) {
        c.Add("event", entry.PeerEventAsRequestKey);
      }
      // NOTE: "ip" is optional and we don't supply it.
      // AnnounceParameters will set entry.PeerIP as its ClientAddress.
      AnnounceParameters par = new AnnounceParameters(c, IPAddress.Parse(entry.PeerIP));
      return par;
    }

    #endregion Methods
  }
}
