// -----------------------------------------------------------------------
// <copyright file="DedupDiskWriterTest.cs" company="Jiangyan Xu">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace GSeries {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using MonoTorrent.Common;
    using System.IO;
    using log4net;
    using log4net.Config;
    using GSeries.BitTorrent;
    using GSeries.ProvisionSupport;
    using NUnit.Framework;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DedupDiskWriterTest {
        public static readonly ILog logger = LogManager.GetLogger(typeof(DedupDiskWriterTest));        

        static DedupDiskWriterTest() {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("NHibernate.Debug.config.xml"));
        }

        public static void TestRead(string db, string file, string length, string offset, string count, string realfile) {
            logger.DebugFormat("DB file path: {0}", db);
            var dbs = new ChunkDbService(db, false);
            var ds = new DeduplicationService(dbs);
            var writer = new DedupDiskWriter(ds);
            var c = int.Parse(count);
            var buffer = new byte[c];
            var f = new TorrentFile(file, long.Parse(length));
            var o = long.Parse(offset);
            int actualRead = writer.Read(f, o, buffer, 0, c);

            byte[] bufferToCompare = new byte[c];
            using (var rf = File.OpenRead(realfile)) {
                rf.Seek(o, SeekOrigin.Begin);
                rf.Read(bufferToCompare, 0, bufferToCompare.Length);
            }

            Assert.IsTrue(buffer.SequenceEqual(bufferToCompare));
        }
    }
}
