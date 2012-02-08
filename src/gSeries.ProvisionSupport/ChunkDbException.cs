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
    /// The exception generated from the data chunk management
    /// component.
    /// </summary>
    public class ChunkDbException : ProvisionSupportException {
        public ChunkDbException() : base() { }
        public ChunkDbException(string msg) : base(msg) { }
        public ChunkDbException(string msg, Exception innerException) : 
            base(msg, innerException) { }
        public ChunkDbException(Exception innerException) : base(innerException) { }
    }
}
