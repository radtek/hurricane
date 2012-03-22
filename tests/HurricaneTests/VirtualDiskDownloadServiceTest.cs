// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using log4net;
    using System.Reflection;
    using log4net.Config;
    using System.IO;
    using GSeries.ProvisionSupport;
    using GSeries.BitTorrent;
    using MonoTorrent.Client;
    using GSeries.DataDistributionService;
    using MonoTorrent.Client.Encryption;
    using MonoTorrent.Common;
    using System.Net;
    using GSeries;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class VirtualDiskDownloadServiceTest {
        static readonly ILog logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);

        static VirtualDiskDownloadServiceTest() {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("NHibernate.Debug.config.xml"));
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            logger.FatalFormat("Fatal exception caught: {0}", e.ExceptionObject);
        }
        public static void TestDownloadFile(string db, string torrentPath, string savePath) {
            //System.Diagnostics.Debugger.Launch();
            var dbs = new ChunkDbService(db, false);
            var ds = new DeduplicationService(dbs);
            var writer = new DedupDiskWriter(ds);
            var engineSettings = new EngineSettings();
            engineSettings.PreferEncryption = false;
            engineSettings.AllowedEncryption = EncryptionTypes.All;
            int port = 33123;
            var ip = NetUtil.GetLocalIPByInterface("Local Area Connection");
            engineSettings.ReportedAddress = new IPEndPoint(ip, port);
            var engine = new ClientEngine(engineSettings, new DedupDiskWriter(ds));
            var vd = new VirtualDiskDownloadService(engine, new FileInfoTable<TorrentManager>());
            var torrent = Torrent.Load(torrentPath);
            logger.DebugFormat("Loaded torrent file: {0}, piece length: {1}.", 
                torrent.Name, torrent.PieceLength);
            var filePath = Path.Combine(savePath, torrent.Name);
            vd.StartDownloadingFile(torrent, savePath, dbs.GetManagedFile(filePath).ChunkMap.LastPieceInProfile);
            Console.Read();
        }
    }
}
