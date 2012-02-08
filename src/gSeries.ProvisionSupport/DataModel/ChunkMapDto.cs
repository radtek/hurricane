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
    /// The ith chunk in the map has the value of fileIndices[i] & Hashes[20 * i].
    /// <seealso cref="GSeries.BitTorrent.ChunkMap"/>
    /// </remarks>
    [ProtoContract]
    public class ChunkMapDto {
        /// <summary>
        /// File index is the index of this chunk in the original file.
        /// </summary>
        [ProtoMember(1)]
        public int[] FileIndices;
        /// <summary>
        /// Hashes concatenated according to the chunk indices.
        /// </summary>
        [ProtoMember(2)]
        public byte[] Hashes;

        /// <summary>
        /// The size of the End-of-file chunk could be smaller than regular chunk size.
        /// </summary>
        [ProtoMember(3)]
        public int EofChunkSize;

        /// <summary>
        /// The index of the EoF file chunk in the FileIndices array.
        /// </summary>
        [ProtoMember(4)]
        public int EofChunkIndex;
    }
}
