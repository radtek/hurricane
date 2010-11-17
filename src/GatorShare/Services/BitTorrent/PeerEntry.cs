using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using MonoTorrent.Tracker;
using MonoTorrent.Common;
using System.Collections;
using System.IO;
using Brunet;

namespace GatorShare.Services.BitTorrent {
  /**
   * A data object that represents the entry to be put to/get from Dht
   */
  public class PeerEntry : DictionaryData {

    #region Fields
    private string _peer_id;
    private IPEndPoint _peer_endpoint;
    private TorrentEvent _event; 
    #endregion

    #region Properties
    public TorrentEvent PeerState {
      get {
        return _event;
      }
    }

    /**
     * Lower case string
     */
    public string PeerEventAsRequestKey {
      get {
        return _event.ToString().ToLower();
      }
    }

    public string PeerIP {
      get {
        return _peer_endpoint.Address.ToString();
      }
    }

    public int PeerPort {
      get {
        return _peer_endpoint.Port;
      }
    }

    public string PeerID {
      get {
        return _peer_id;
      }
    }
    #endregion

    /// <summary>
    /// Added so that Activator.CreateInstance doesn't complain.
    /// </summary>
    public PeerEntry() {
    }

    /**
     * This initializes the PeerEntry to be stored in Dht
     * @param pars
     */
    public PeerEntry(AnnounceParameters pars) {
      /* 
       * The ClientAddress here could be ["ip"] in the args of GET request
       * or, if it is null, the remote endpoint IP of the request.
       * Don't use pars.RemoteAddress
       */
      this._peer_endpoint = pars.ClientAddress;
      this._peer_id = pars.PeerId;
      this._event = pars.Event;
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      sb.Append("{");
      sb.Append("Peer ID = " + _peer_id + ",");
      sb.Append("Peer Endpint = " + _peer_endpoint.ToString() + ",");
      sb.Append("Peer Event = " + _event.ToString());
      sb.Append("}");
      return sb.ToString();
    }

    public override IDictionary ToDictionary() {
      IDictionary dict = InitIDictionaryFromType();
      dict.Add("id", _peer_id);
      dict.Add("addr", _peer_endpoint.Address.ToString());
      dict.Add("port", _peer_endpoint.Port);
      dict.Add("event", (int)_event);
      return dict;
    }

    public override void FromDictionary(IDictionary dict) {
      _peer_id = dict["id"] as string;
      _peer_endpoint = new IPEndPoint(IPAddress.Parse(dict["addr"] as string),
        (int)dict["port"]);
      _event = (TorrentEvent)dict["event"];
    }
  }
}
