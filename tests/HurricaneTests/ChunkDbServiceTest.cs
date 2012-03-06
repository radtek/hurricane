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
    using GSeries.ProvisionSupport;
    using NUnit.Framework;
    using System.Threading;

    public class ChunkDbServiceTest {
        public static readonly ILog logger = LogManager.GetLogger("ChunkDbServiceTest");

        static ChunkDbServiceTest() {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("NHibernate.Debug.config.xml"));
        }

        public static void TestReadFileIntoChunkDb(string filePath) {
            var dbFilePath = Path.Combine(Path.GetTempPath(), "newChunkDB.db");
            logger.DebugFormat("DB file path: {0}", dbFilePath);
            var dbs = new ChunkDbService(dbFilePath, true);
            dbs.AddFileAllChunks(filePath);
            logger.Debug("Data file processing finished.");
        }


        public static void TestAddFileWithBasicChunkMap(string db, string filePath) {
            logger.DebugFormat("DB file path: {0}", db);
            var dbs = new ChunkDbService(db, true);
            dbs.AddFileWithBasicChunkMap(filePath);
            logger.Debug("Data file processing finished.");
        }

        public static void TestGetChunkLocations(string db, string file, string chunksStr) {
            var dbs = new ChunkDbService(db, false);
            var chunks = chunksStr.Split(',');
            int[] chunkArray = Array.ConvertAll<string, int>(chunks, x => int.Parse(x));
            var locations = dbs.GetChunkLocations(file, chunkArray);
            foreach (var loc in locations) {
                logger.Debug(loc);
            }
        }

        public static void TestAddFile(string db, string filePath, string chunkMapDto) {
            var dbs = new ChunkDbService(db, false);
            dbs.AddFile(filePath, File.ReadAllBytes(chunkMapDto));
        }

        public static void TestAddTorrent(string db, string filePath, string torrent) {
            var dbs = new ChunkDbService(db, false);
            dbs.AddTorrent(filePath, File.ReadAllBytes(torrent));
        }
    }
}
