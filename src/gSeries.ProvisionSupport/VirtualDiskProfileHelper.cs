// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using log4net;

    /// <summary>
    /// Helper class for <see cref="VirtualDiskProfileHelper"/> that handles DB
    /// operations.
    /// </summary>
    public class VirtualDiskProfileHelper {
        #region Readonly Fields
        readonly VirtualDiskProfileService _profileDb;
        readonly ChunkDbService _chunkDb;
        static readonly ILog logger = LogManager.GetLogger(typeof(FileHelper)); 
        #endregion

        public VirtualDiskProfileHelper(VirtualDiskProfileService profileDb, 
            ChunkDbService chunkDb) {
            _profileDb = profileDb;
            _chunkDb = chunkDb;
        }
    }
}
