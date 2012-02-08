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

    public class VirtualDiskProfileServiceTest {
        public void TestGenerateChunkMap(string filePath) {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("NHibernate.Debug.config.xml"));
            var logger = LogManager.GetLogger("CreateChunkDb");
            var dbFilePath = Path.Combine(Path.GetTempPath(), "newChunkDB.db");
            logger.DebugFormat("DB file path: {0}", dbFilePath);
            var dbs = new ChunkDbService(dbFilePath, true);
            dbs.AddFileAllChunks(filePath);
            logger.Debug("Data file processing finished.");
        }
    }
}
