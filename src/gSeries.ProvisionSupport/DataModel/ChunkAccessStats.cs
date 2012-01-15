// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Stats object about the access pattern on a chunk.
    /// </summary>
    public class ChunkAccessStats {
        public virtual int Id { get; private set; }
        public virtual int ChunkNumber { get; private set; }
        public virtual int ReadCount { get; private set; }
        /// <summary>
        /// Gets the the time (milliseconds since the start) of the earliest 
        /// read on this chunk.
        /// </summary>
        public virtual double EarliestRead { get; private set; }
    }
}
