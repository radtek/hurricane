﻿// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using FluentNHibernate.Mapping;

    /// <summary>
    /// Maps <see cref=""/> created in Python.
    /// </summary>
    public class AccessProfileMap : ClassMap<AccessProfile> {
        public AccessProfileMap() {
            Table("profile");
            Id(x => x.Id).Column("id");
            Map(x => x.ChunkIndex).Column("chunk_index");
            Map(x => x.ReadCount).Column("read_count");
            Map(x => x.EarliestRead).Column("earliest_read");
        }
    }
}
