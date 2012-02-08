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

    public class ManagedFileMap : ClassMap<ManagedFile> {
        public ManagedFileMap() {
            Id(x => x.Id);
            Map(x => x.FileHash).Length(20);
            Map(x => x.Path).Unique();
            Map(x => x.Size).Not.Nullable();
            Map(x => x.ChunkMap);
            Map(x => x.TorrentFile);
            Map(x => x.InfoHash);

            Cache.ReadWrite().IncludeAll();
        }
    }
}
