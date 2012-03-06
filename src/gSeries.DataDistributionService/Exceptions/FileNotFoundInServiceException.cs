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
    /// Exception that indicates the requested file cannot be found in the 
    /// data distribution service.
    /// </summary>
    public class FileNotFoundInServiceException : DataDistributionServiceException {
        public FileNotFoundInServiceException() : base() { }
        public FileNotFoundInServiceException(string msg) : base(msg) { }
        public FileNotFoundInServiceException(string msg, Exception innerException) : 
            base(msg, innerException) { }
        public FileNotFoundInServiceException(Exception innerException) : 
            base("Error occurred in Data Distribution Service.", innerException) { }
    }
}
