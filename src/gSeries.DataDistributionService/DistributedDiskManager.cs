// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DataDistributionService {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using MonoTorrent.Client;
    using System.Threading;
    using log4net;
    using System.Reflection;
    using System.IO;
    using GSeries.BitTorrent;

    /// <summary>
    /// A disk manager that transparently locate data on local disk or download
    /// data chunk on demand.
    /// </summary>
    public class DistributedDiskManager {
        static readonly ILog logger = LogManager.GetLogger(
                        MethodBase.GetCurrentMethod().DeclaringType);
        DiskManager _diskManager;
        FileInfoTable<TorrentManager> _torrentManagerTable;

        public DistributedDiskManager(DiskManager diskManager,
            FileInfoTable<TorrentManager> tmTable) {
            _diskManager = diskManager;
            _torrentManagerTable = tmTable;
        }

        public byte[] ReadReorderedFile(string path, List<Tuple<long, int>> readList) {
            var readResults = new byte[readList.Count][];
            TorrentManager tm;
            try {
                tm = _torrentManagerTable[path];
            } catch (KeyNotFoundException ex) {
                throw new InvalidOperationException(
                    "TorrentManagerTable should have the torrent manager " +
                    "registered.", ex);
            }

            int i = 0;
            var expandedReadList = readList.ConvertAll<Tuple<int, long, int>>(x => Tuple.Create<int, long, int>(i++, x.Item1, x.Item2));
            var failedItems = Read(expandedReadList, readResults, tm);
            logger.DebugFormat("{0} items failed to be read from disk and " +
                "thus will be requested on-demand.", failedItems.Count);

            var wh = new AutoResetEvent(false);
            var pieces2Request = new HashSet<int>();
            // Wait for results.
            // TODO: What if the wanted pieces are downloaded between the above
            // and the following actions?
            EventHandler<PieceHashedEventArgs> dl = delegate(object sender, PieceHashedEventArgs e) {
                if (e.HashPassed) {
                    pieces2Request.Remove(e.PieceIndex);
                }
                if (pieces2Request.Count == 0)
                    wh.Set();
            };

            lock (tm.OnDemandPicker.SyncRoot) {
                if (failedItems.Count > 0) {
                    // Now request the rest.
                    foreach (int item in failedItems) {
                        var tuple = readList[item];
                        long offset = tuple.Item1;
                        int count = tuple.Item2;

                        // Assume count <= 16KB and doesn't cross piece boundary.
                        int pieceIndex = (int)(offset / tm.Torrent.PieceLength);
                        pieces2Request.Add(pieceIndex);
                        tm.OnDemandPicker.AddOnDemand(pieceIndex);
                    }
                    logger.DebugFormat("There are {0} pieces in total that we need " +
                        "to request on-demand.", pieces2Request.Count);


                    tm.PieceHashed += dl;
                }
            }
            if (wh.WaitOne(40000)) {
                logger.DebugFormat("All pending pieces have been downloaded for this read.");
            } else {
                logger.ErrorFormat("Pieces cannot be downloaded in time.");
            }
            
            tm.PieceHashed -= dl;

            // Read them.
            var newReadList = failedItems.ConvertAll<Tuple<int, long, int>>(x => expandedReadList[x]);
            var newFailedList = Read(newReadList, readResults, tm);

            if (newFailedList.Count != 0) {
                throw new DataDistributionServiceException("On-demand pieces still cannot be read.");
            }

            using (var stream = new MemoryStream()) {
                foreach (var bytes in readResults) {
                    stream.Write(bytes, 0, bytes.Length);
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Reads the specified read list.
        /// </summary>
        /// <param name="readList">The read list. The first tuple item specifies 
        /// where in the result array to put the read result.</param>
        /// <param name="readResults">The read results.</param>
        /// <param name="tm">The tm.</param>
        /// <returns>The list of indices of failed items in the readlist.</returns>
        private List<int> Read(List<Tuple<int, long, int>> readList, byte[][] readResults, TorrentManager tm) {
            var failedItems = new List<int>();
            for (int i = 0; i < readList.Count(); i++) {
                var tuple = readList[i];
                AutoResetEvent waitHandle = new AutoResetEvent(false);
                int resultIndex = tuple.Item1;
                long offset = tuple.Item2;
                int count = tuple.Item3;
                byte[] buffer = new byte[count];
                bool successful = false;
                _diskManager.QueueRead(tm, offset, buffer, count, delegate(bool succ) {
                    // DiskManager with DedupDiskWriter reads data according to 
                    // mappings for deduplicated data.
                    successful = succ;
                    if (succ) {
                        logger.DebugFormat("Read (offset, count) = ({0}, {1}) successful.", offset, count);
                        readResults[resultIndex] = buffer;
                    } else {
                        failedItems.Add(i);
                    }
                    waitHandle.Set();
                });
                waitHandle.WaitOne();
                waitHandle.Close();
            }
            return failedItems;
        }

    }
}
