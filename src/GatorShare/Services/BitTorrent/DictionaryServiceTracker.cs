using System;
using System.Collections;
using System.Text;

using MonoTorrent.Tracker;
using GatorShare.Services;
using System.Net;
using HttpListener = MonoTorrent.Tracker.Listeners.HttpListener;

namespace GatorShare.Services.BitTorrent {
  /// <summary>
  /// Represents a tracker that listens to HTTP requests and grabs peers from 
  /// DHT.
  /// </summary>
  public class DictionaryServiceTracker {
    #region Fields
    Tracker _tracker;
    DictionaryServiceTrackerListener _dictListener;
    HttpListener _httpListener;
    private static IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(DictionaryServiceTracker));
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
    /// The URL prefix that this tracker listens to.
    /// </summary>
    public string ListeningPrefix { get; private set; }

    /// <summary>
    /// Constructs DhtTracker using the given DhtSerivceProxy instance.
    /// </summary>
    /// <param name="dhtProxy"></param>
    public DictionaryServiceTracker(DictionaryServiceProxy dhtProxy, string listeningPrefix) {
      _dictListener = new DictionaryServiceTrackerListener(dhtProxy);
      ListeningPrefix = listeningPrefix;
      _httpListener = new HttpListener(ListeningPrefix);
      Logger.WriteLineIf(LogLevel.Info, _log_props, string.Format(
        "DictionaryServiceTracker starting at: {0}", ListeningPrefix));
      // Subscribe the HttpListener events to do our nifty stuff.
      _httpListener.AnnounceReceived += this.OnAnnounceReceived;
      _httpListener.ScrapeReceived += this.OnScrapeReceived;

      _tracker = new Tracker();

      // This also subscribes the same above 2 events but does this AFTER them, 
      // so HttpListener invokes DHT operations first.
      _tracker.RegisterListener(_httpListener);
      // And... the events from DHT
      _tracker.RegisterListener(_dictListener);
    }

    /// <summary>
    /// Starts all the listeners in this tracker.
    /// </summary>
    public void Start() {
      _dictListener.Start();
      _httpListener.Start();
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
      try {
        _dictListener.HandleAnnounceRequest(e);
      } catch (Exception ex) {
        Logger.WriteLineIf(LogLevel.Error, _log_props, 
          string.Format("Exception caught while processing announce request. {0}", ex));
        throw;
      }
    }

    private void OnScrapeReceived(object sender, ScrapeParameters e) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Scrape received from {0}. Ignoring it.", e.RemoteAddress));
      // Do nothing.
    }
  }
}
