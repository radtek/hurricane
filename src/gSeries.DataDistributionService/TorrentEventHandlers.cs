// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DataDistributionService {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using log4net;
    using System.Reflection;
    using MonoTorrent.Client;
    using MonoTorrent.Common;
    using MonoTorrent.Client.Tracker;

    /// <summary>
    /// This class implements the logic handling various torrent events so that 
    /// it can be reused.
    /// </summary>
    public class TorrentEventHandlers {
        static readonly ILog logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);

        public static void HandleTorrentStateChanged(object sender, 
            TorrentStateChangedEventArgs e) {
            var torrentManager = e.TorrentManager;
            var clientEngine = torrentManager.Engine;
            logger.DebugFormat("Torrent {0}: State changed from {1} to {2}", e.TorrentManager.Torrent.Name,
              e.OldState.ToString(), e.NewState.ToString());

            switch (e.NewState) {
                case TorrentState.Downloading:
                    logger.DebugFormat("Open connections: {0}", torrentManager.OpenConnections);
                    if (e.OldState == TorrentState.Hashing) {
                        logger.Debug("Hashing Mode completed.");
                        logger.DebugFormat("{0} pieces ({2} percent) already available out of {1} in total.", 
                            torrentManager.Bitfield.TrueCount, 
                            torrentManager.Torrent.Pieces.Count, torrentManager.Bitfield.PercentComplete);
                    }
                    break;
                case TorrentState.Seeding:
                    if (e.OldState == TorrentState.Downloading) {
                        logger.DebugFormat("Download completed.", torrentManager.Torrent.Name);

                        logger.Debug(GetStatsLog(torrentManager));
                        // Flush so that whoever is waiting for the download can 
                        // read it immediately.
                        clientEngine.DiskManager.Flush(e.TorrentManager);
                    }
                    break;
                default:
                    break;
            }
        }

        public static void HandlePieceHashed(object sender, PieceHashedEventArgs e) {
            var torrentManager = e.TorrentManager;
            if (torrentManager.State == TorrentState.Downloading && e.HashPassed) {
                logger.DebugFormat("Torrent {0}: downloaded piece {1}.", 
                    torrentManager.Torrent.Name, e.PieceIndex);
            }
        }

        public static void HandleAnnounceComplete(object sender, AnnounceResponseEventArgs e) {
            logger.DebugFormat("AnnounceComplete. Tracker={0}, Successful={1}",
              e.Tracker.Uri,
              e.Successful);
            if (e.Successful) {
                logger.DebugFormat("Tracker: Peers={2}, Complete={0}, Incomplete={1}",
                    e.Tracker.Complete,
                    e.Tracker.Incomplete,
                    e.Peers.Count);
                e.Peers.ForEach(x => logger.DebugFormat("Peer: {0}", x.ToString()));
            } else {
                logger.DebugFormat("Announce failed: {0}", e.Tracker.FailureMessage);
            }
        }

        private static void AppendSeperator(StringBuilder sb) {
            AppendFormat(sb, "", null);
            AppendFormat(sb, "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - -", null);
            AppendFormat(sb, "", null);
        }

        public static string GetStatsLog(TorrentManager manager) {
            StringBuilder sb = new StringBuilder();
            AppendSeperator(sb);
            AppendFormat(sb, "State:           {0}", manager.State);
            AppendFormat(sb, "Name:            {0}", manager.Torrent == null ? "MetaDataMode" : manager.Torrent.Name);
            AppendFormat(sb, "Progress:           {0:0.00}", manager.Progress);
            AppendFormat(sb, "Download Speed:     {0:0.00} kB/s", manager.Monitor.DownloadSpeed / 1024.0);
            AppendFormat(sb, "Upload Speed:       {0:0.00} kB/s", manager.Monitor.UploadSpeed / 1024.0);
            AppendFormat(sb, "Total Downloaded:   {0:0.00} MB", manager.Monitor.DataBytesDownloaded / (1024.0 * 1024.0));
            AppendFormat(sb, "Total Uploaded:     {0:0.00} MB", manager.Monitor.DataBytesUploaded / (1024.0 * 1024.0));
            MonoTorrent.Client.Tracker.Tracker tracker = manager.TrackerManager.CurrentTracker;
            //AppendFormat(sb, "Tracker Status:     {0}", tracker == null ? "<no tracker>" : tracker.State.ToString());
            AppendFormat(sb, "Warning Message:    {0}", tracker == null ? "<no tracker>" : tracker.WarningMessage);
            AppendFormat(sb, "Failure Message:    {0}", tracker == null ? "<no tracker>" : tracker.FailureMessage);
            //if (manager.PieceManager != null)
            //    AppendFormat(sb, "Current Requests:   {0}", manager.PieceManager.CurrentRequestCount());

            //foreach (PeerId p in manager.GetPeers())
            //    AppendFormat(sb, "\t{2} - {1:0.00}/{3:0.00}kB/sec - {0}", p.Peer.ConnectionUri,
            //                                                              p.Monitor.DownloadSpeed / 1024.0,
            //                                                              p.AmRequestingPiecesCount,
            //                                                              p.Monitor.UploadSpeed / 1024.0);

            AppendFormat(sb, "", null);
            if (manager.Torrent != null)
                foreach (TorrentFile file in manager.Torrent.Files)
                    AppendFormat(sb, "{1:0.00}% - {0}", file.Path, file.BitField.PercentComplete);
            return sb.ToString();
        }

        private static void AppendFormat(StringBuilder sb, string str, params object[] formatting) {
            if (formatting != null)
                sb.AppendFormat(str, formatting);
            else
                sb.Append(str);
            sb.AppendLine();
        }
    }
}
