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
    /// A file that is managed in the system (DB).
    /// </summary>
    public class ManagedFile {
        public virtual int Id { get; private set; }
        /// <summary>
        /// Gets or sets the file hash (SHA1).
        /// </summary>
        /// <value>
        /// The file hash.
        /// </value>
        public virtual byte[] FileHash { get; set; }
        public virtual string Path { get; set; }
        public virtual long Size { get; set; }
    }
}
