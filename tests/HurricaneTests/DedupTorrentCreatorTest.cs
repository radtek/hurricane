// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using log4net.Config;
    using log4net;
    using System.IO;
    using System.Reflection;
    using GSeries.BitTorrent;
    using GSeries.ProvisionSupport;
    using MonoTorrent;
    using MonoTorrent.Common;
    using NUnit.Framework;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DedupTorrentCreatorTest {
        static readonly ILog logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);

        static DedupTorrentCreatorTest() {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("NHibernate.Debug.config.xml"));
        }

        public static void TestCreateTorrent(string db, string dataFile, string savePath, string savePath1) {
            var dedupWriter = new DedupDiskWriter(new DeduplicationService(new ChunkDbService(db, false)));
            var creator = new DedupTorrentCreator(dedupWriter);
            var tier = new RawTrackerTier { 
                "udp://tracker.publicbt.com:80/announce",
                "udp://tracker.openbittorrent.com:80/announce"
            };
            creator.Announces.Add(tier);
            var binaryTorrent = creator.Create(new TorrentFileSource(dataFile));
            var torrent = Torrent.Load(binaryTorrent);
            string infoHash = torrent.InfoHash.ToHex();
            File.WriteAllBytes(savePath, binaryTorrent.Encode());

            var creator1 = new TorrentCreator();
            creator1.Announces.Add(tier);
            var binary1 = creator1.Create(new TorrentFileSource(dataFile));
            string infoHash1 = Torrent.Load(binary1).InfoHash.ToHex();
            File.WriteAllBytes(savePath1, binary1.Encode());

            Assert.AreEqual(infoHash, infoHash1);
            logger.DebugFormat("InfoHash: {0}", infoHash);
        }
    }
}
