// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DataDistributionService {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Runtime.Serialization;

    /// <summary>
    /// This class is used to communicate file information.
    /// </summary>
    [DataContract]
    public class PathStatusDto {
        public enum PathTypeEnum {
            File,
            Directory
        }

        [DataMember(Order=1)]
        public PathTypeEnum PathType;
        [DataMember(Order=2)]
        public long FileSize;
    }
}
