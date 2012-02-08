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
    using System.Reflection;
    using MonoTorrent.Common;

    public class DeduplicationServiceTest {
        static readonly ILog logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);

        static DeduplicationServiceTest() {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("NHibernate.Debug.config.xml"));
        }

        public static void TestAddFileToDownload(string chunkDb, string datafile,
            string chunkMap, string torrentFile) {
            logger.DebugFormat("DB file path: {0}", chunkDb);
            var dbs = new ChunkDbService(chunkDb, false);
            var ds = new DeduplicationService(dbs);
            var torrentBytes = File.ReadAllBytes(torrentFile);
            var torrent = Torrent.Load(torrentBytes);
            ds.AddFileToDownload(datafile, File.ReadAllBytes(chunkMap),
                torrentBytes, torrent.Files[0].Length);
        }

        public static void ReverseChunkMap(string chunkMap, string newChunkMap) {
            logger.DebugFormat("ChunkMap: {0}", chunkMap);
            var dto = ChunkMapSerializer.Deserialize(chunkMap);
            var cm = new ChunkMap(dto);
            var fileIndices = dto.FileIndices.Reverse();
            var l = cm.HashesAsList;
            l.Reverse();
            cm.HashesAsList = l;
            cm.ChunkMapDto.FileIndices = fileIndices.ToArray();
            cm.ChunkMapDto.EofChunkIndex = 0;
            logger.DebugFormat("New ChunkMap: {0}", cm);
            using (var stream = File.OpenWrite(newChunkMap)) {
                ChunkMapSerializer.Serialize(stream, cm.ChunkMapDto);
            }
        }

        public static void TestRegisterFilePart(string db, string file, string offset, string count) {
            logger.DebugFormat("DB file path: {0}", db);
            var dbs = new ChunkDbService(db, false);
            var dedup = new DeduplicationService(dbs);
            dedup.RegisterFilePart(file, long.Parse(offset), int.Parse(count));
        }

        public static void TestGetDedupFileParts1(string db, string path, string offset, string count) {
            logger.DebugFormat("DB file path: {0}", db);
            var dbs = new ChunkDbService(db, false);
            var ds = new DeduplicationService(dbs);
            IList<Tuple<string, long, int>> result = 
                ds.GetDedupFileParts(path, long.Parse(offset), int.Parse(count));
            result.ToList().ForEach(x => logger.Debug(x));
        }
    }
}
