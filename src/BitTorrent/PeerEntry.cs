using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using MonoTorrent.Tracker;
using MonoTorrent.Common;
using System.Collections;
using System.IO;
using Brunet;

namespace Fushare.BitTorrent {
  /**
   * A data object that represents the entry to be put to/get from Dht
   */
  class PeerEntry {

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

    #region Constructors
    /**
     * This initializes the PeerEntry to be stored in Dht
     * @param pars
     */
    public PeerEntry(AnnounceParameters pars) {
      /* 
       * The ClientAddress here could be ["ip"] in the args of GET request
       * or, if it is null, the remote endpoint IP of the request
       */
      this._peer_endpoint = pars.ClientAddress;
      this._peer_id = pars.PeerId;
      this._event = pars.Event;
    }

    public PeerEntry(byte[] binaryEntry) {
      IList list = (IList)AdrConverter.Deserialize(binaryEntry);
      this._peer_id = list[0] as string;
      IPAddress ip = IPAddress.Parse(list[1] as string);
      this._peer_endpoint = new IPEndPoint(ip, (int)list[2]);
      this._event = (TorrentEvent)list[3];
    }
    #endregion

    public string Serialize() {
      //Use list instead of Dictionary to save storage space and network bandwidth
      IList list = new ArrayList();
      list.Add(_peer_id);
      list.Add(_peer_endpoint.Address.ToString());
      list.Add(_peer_endpoint.Port);
      list.Add((int)_event);
      byte[] content = null;
      using (MemoryStream ms = new System.IO.MemoryStream()) {
        AdrConverter.Serialize(list, ms);
        content = ms.ToArray();
      }
      return Encoding.UTF8.GetString(content);
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine("Peer ID: " + _peer_id);
      sb.AppendLine("Peer Endpint: " + _peer_endpoint.ToString());
      sb.AppendLine("Peer Event: " + _event.ToString());
      return sb.ToString();
    }
  }
}
