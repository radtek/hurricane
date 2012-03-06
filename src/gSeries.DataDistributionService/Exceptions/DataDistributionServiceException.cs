// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DataDistributionService {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Base exception for errors in this data distribution service.
    /// </summary>
    public class DataDistributionServiceException : Exception {
        public string File { set; get; }
        public DataDistributionServiceException() : base() { }
        public DataDistributionServiceException(string msg) : base(msg) { }
        public DataDistributionServiceException(string msg, Exception innerException) : 
            base(msg, innerException) { }
        public DataDistributionServiceException(Exception innerException) : 
            base("Error occurred in Data Distribution Service.", innerException) { }
    }
}
