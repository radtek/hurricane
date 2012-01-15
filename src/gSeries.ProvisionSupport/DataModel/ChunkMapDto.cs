// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ProtoBuf;

    /// <summary>
    /// Data transfer object for the chunk map implemented using ProtoBuf.
    /// </summary>
    /// <remarks>
    /// It is transferred from the source node to destination nodes to convey
    /// the hashes and order of chunks.
    /// <seealso cref="GSeries.BitTorrent.ChunkMap"/>
    /// </remarks>
    [ProtoContract]
    public class ChunkMapDto {
        [ProtoMember(1)]
        public int[] FileIndices;
        [ProtoMember(2)]
        public byte[] Hashes;
    }
}
