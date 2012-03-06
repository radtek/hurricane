// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Exception thrown when file cannot be found in the Chunk DB.
    /// </summary>
    public class FileNotFoundInDbException : ChunkDbException {
        public string File { set; get; }
        public FileNotFoundInDbException() : base() { }
        public FileNotFoundInDbException(string msg) : base(msg) { }
        public FileNotFoundInDbException(string msg, Exception innerException) : 
            base(msg, innerException) { }
        public FileNotFoundInDbException(Exception innerException) : base(innerException) { }
    }
}
