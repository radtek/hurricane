// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php
using System;
using MonoTorrent.Tracker;
using MonoTorrent.Tracker.Listeners;
using System.Diagnostics;

namespace GSeries.TrackerApp {
    class TrackerApp {
        Tracker _tracker;
        ListenerBase _listener;
       
        public TrackerApp() {
            ListenerBase listener = new HttpListener("http://+:25456/announce/");
            var tracker = new Tracker();
            tracker.AllowUnregisteredTorrents = true;
            tracker.RegisterListener(listener);
            tracker.AllowScrape = false;
            _listener = listener;
            _tracker = tracker;
        }

        public void Start() {
            _listener.Start();
        }

        public static void Main(string[] args) {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var t = new TrackerApp();
            t.Start();
            Console.WriteLine("Tracker is running until a keyboard key is hit.");
            Console.Read();
        }
    }
}
