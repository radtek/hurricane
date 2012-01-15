// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
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
    /// The Chunk Access is created in Python.
    /// </summary>
    public class ChunkAccessStatsMap : ClassMap<ChunkAccessStats> {
        public ChunkAccessStatsMap() {
            Table("block_access");
            Id(x => x.Id).Column("id");
            Map(x => x.ChunkNumber).Column("blocknumber");
            Map(x => x.ReadCount).Column("reads");
            Map(x => x.EarliestRead).Column("earliest_read");
        }
    }
}
