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

        public byte[] ReadFile(string path, List<Tuple<long, int>> readList) {
            var readResults = new byte[readList.Count][];
            var failedItems = new List<int>();
            TorrentManager tm = _torrentManagerTable[path];

            for (int i = 0; i < readList.Count(); i++) {
                var tuple = readList[i];
                AutoResetEvent waitHandle = new AutoResetEvent(false);
                long offset = tuple.Item1;
                int count = tuple.Item2;
                byte[] buffer = new byte[count];
                bool successful = false;
                _diskManager.QueueRead(tm, offset, buffer, count, delegate(bool succ) {
                    // DiskManager with DedupDiskWriter reads data according to 
                    // mappings for deduplicated data.
                    successful = succ;
                    if (succ) {
                        logger.DebugFormat("Read (offset, count) = ({0}, {1}) successful.", offset, count);
                        readResults[i] = buffer;
                    } else {
                        failedItems.Add(i);
                    }
                    waitHandle.Set();
                });
                waitHandle.WaitOne();
            }

            // Now request the rest.
            if (failedItems.Count != 0)
                throw new Exception("Not all chunks are on the host.");

            using (var stream = new MemoryStream()) {
                foreach (var bytes in readResults) {
                    stream.Write(bytes, 0, bytes.Length);
                }
                return stream.ToArray();
            }
        }
    }
}
