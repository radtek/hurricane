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
    /// This class maps the DataChunk to a database table.
    /// </summary>
    public class DataChunkMap : ClassMap<DataChunk> {
        public DataChunkMap() {
            Id(x => x.Id).GeneratedBy.HiLo("1000");
            Map(x => x.Hash)
                .Length(20);
                //.Unique();
            References<ManagedFile>(x => x.File).Cascade.All()
                .UniqueKey("PathFileIndex_Index");
            Map(x => x.FileIndex).UniqueKey("PathFileIndex_Index");
            Map(x => x.Count);
            BatchSize(500);
        }
    }
}
