// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The base class for exceptions in the ProvisionSupport component.
    /// </summary>
    public class ProvisionSupportException : Exception {
        public ProvisionSupportException() : base() { }
        public ProvisionSupportException(string msg) : base(msg) { }
        public ProvisionSupportException(string msg, Exception innerException) : 
            base(msg, innerException) { }
        public ProvisionSupportException(Exception innerException) : 
            base("Failure in ProvisionSupport.", innerException) { }
    }
}
