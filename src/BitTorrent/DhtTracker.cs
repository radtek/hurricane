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

    public Tracker Tracker {
      get {
        return _tracker;
      }
    }

    public DhtTracker() {
      string listening_prefix = FushareConfigHandler.ConfigObject.
        dhtTrackerListeningPrefix;
      _http_listener = new MonoTorrent.Tracker.Listeners.HttpListener(
        listening_prefix);
      Logger.WriteLineIf(LogLevel.Info, _log_props, string.Format(
        "DhtTracker starting at: {0}", listening_prefix));
      // Subscribe the HttpListener events to do our nifty stuff.
      _http_listener.AnnounceReceived += this.OnAnnounceReceived;
      _http_listener.ScrapeReceived += this.OnScrapeReceived;
      BrunetDht dht = (BrunetDht)DictionaryServiceFactory.GetServiceInstance(
        typeof(BrunetDht));
      _dht_listener = new DhtListener(new DhtServiceProxy(dht));
      _tracker = new Tracker();

      // This also subscribes the same above 2 events but does this AFTER them, 
      // so HttpListener invokes DHT operations first.
      _tracker.RegisterListener(_http_listener);
      // And... the events from DHT
      _tracker.RegisterListener(_dht_listener);
    }

    /// <summary>
    /// Start all the listeners in this tracker.
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
      _dht_listener.HandleAnnounceRequest(e);
    }

    private void OnScrapeReceived(object sender, ScrapeParameters e) {
      // Do thing
    }
  }
}
