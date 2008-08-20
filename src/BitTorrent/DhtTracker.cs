using System;
using System.Collections;
using System.Text;

using MonoTorrent.Tracker;
using MonoTorrent.Tracker.Listeners;
using Fushare.Services;

namespace Fushare.BitTorrent {
  /// <summary>
  /// Represents a tracker that listens to HTTP requests and grabs peers from 
  /// DHT.
  /// </summary>
  public class DhtTracker {
    #region Fields
    Tracker _tracker;
    DhtListener _dht_listener;
    HttpListener _http_listener;
    private static IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(DhtTracker));
    #endregion

    /// <summary>
    /// Gets The original tracker that MonoTorrent provides. 
    /// </summary>
    public Tracker Tracker {
      get {
        return _tracker;
      }
    }

    /// <summary>
    /// Constructs DhtTracker using the given DhtSerivceProxy instance.
    /// </summary>
    /// <param name="dhtProxy"></param>
    public DhtTracker(DhtServiceProxy dhtProxy, string listeningPrefix) {
      _dht_listener = new DhtListener(dhtProxy);
      string listening_prefix = listeningPrefix;
      _http_listener = new MonoTorrent.Tracker.Listeners.HttpListener(
        listening_prefix);
      Logger.WriteLineIf(LogLevel.Info, _log_props, string.Format(
        "DhtTracker starting at: {0}", listening_prefix));
      // Subscribe the HttpListener events to do our nifty stuff.
      _http_listener.AnnounceReceived += this.OnAnnounceReceived;
      _http_listener.ScrapeReceived += this.OnScrapeReceived;

      _tracker = new Tracker();

      // This also subscribes the same above 2 events but does this AFTER them, 
      // so HttpListener invokes DHT operations first.
      _tracker.RegisterListener(_http_listener);
      // And... the events from DHT
      _tracker.RegisterListener(_dht_listener);
    }

    /// <summary>
    /// Starts all the listeners in this tracker.
    /// </summary>
    public void Start() {
      _dht_listener.Start();
      _http_listener.Start();
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("DhtTracker started."));
    }

    /// <summary>
    /// Listens to event fired by HttpListener and delegates the handling 
    /// process to DhtListener where a list of peers are retrieved.
    /// </summary>
    private void OnAnnounceReceived(object sender, AnnounceParameters e) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Annoucement received from {0}", e.RemoteAddress));
      _dht_listener.HandleAnnounceRequest(e);
    }

    private void OnScrapeReceived(object sender, ScrapeParameters e) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Scrape received from {0}", e.RemoteAddress));
      // Do nothing.
    }
  }
}
