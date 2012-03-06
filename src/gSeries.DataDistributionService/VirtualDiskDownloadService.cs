// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DataDistributionService {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using MonoTorrent.Client;
    using MonoTorrent.Common;
    using System.Reflection;
    using log4net;
    using MonoTorrent.Client.Tracker;

    /// <summary>
    /// The service in charge of downloading virtual disks.
    /// </summary>
    public class VirtualDiskDownloadService {
        static readonly ILog logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);
        ClientEngine _clientEngine;
        TorrentSettings torrentDefaults = new TorrentSettings(4, 150, 0, 0);
        FileInfoTable<TorrentManager> _torrentManagerTable;

        public VirtualDiskDownloadService(ClientEngine torrentClientEngine, 
            FileInfoTable<TorrentManager> torrentManagerTable) {
            _clientEngine = torrentClientEngine;
            _torrentManagerTable = torrentManagerTable;
        }

        public void Start() {
            _clientEngine.StartAll();
        }

        /// <summary>
        /// Adds the file to download. This method returns when the torrent is 
        /// added and doesn't wait for the download to complete.
        /// </summary>
        /// <param name="torrent">The torrent.</param>
        /// <param name="savePath">The save path.</param>
        public void StartDownloadingFile(Torrent torrent, string savePath) {
            TorrentSettings torrentDefaults = new TorrentSettings(4, 150, 0, 0);
            var tm = new TorrentManager(torrent, savePath, torrentDefaults, "");
            _clientEngine.Register(tm);

            tm.TrackerManager.CurrentTracker.AnnounceComplete += 
                new EventHandler<AnnounceResponseEventArgs>(
                    TorrentEventHandlers.HandleAnnounceComplete);

            tm.TrackerManager.CurrentTracker.ScrapeComplete += delegate(object o, ScrapeResponseEventArgs e) {
                logger.DebugFormat("Scrape completed. Successful={0}, Tracker={1}", 
                    e.Successful, e.Tracker.Uri);
            };

            tm.TorrentStateChanged += 
                new EventHandler<TorrentStateChangedEventArgs>(
                    TorrentEventHandlers.HandleTorrentStateChanged);

            tm.PieceHashed +=
                new EventHandler<PieceHashedEventArgs>(
                    TorrentEventHandlers.HandlePieceHashed);

            tm.PeerConnected += delegate(object o, PeerConnectionEventArgs e) {
                // Only log three connections.
                if (e.TorrentManager.OpenConnections < 4) {
                    logger.DebugFormat(
                        "Peer ({0}) Connected. Currently {1} open connection.", 
                        e.PeerID.Uri, e.TorrentManager.OpenConnections);
                }
            };

            foreach (var file in tm.Torrent.Files) {
                _torrentManagerTable[file.FullPath] = tm;
            }

            tm.Start();
            logger.DebugFormat("Torrent: {0}. Torrent manager started.", tm.Torrent.Name);
        }
    }
}
