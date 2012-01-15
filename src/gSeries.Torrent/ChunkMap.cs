// Copyright (c) 2011 Xu, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.BitTorrent {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using MonoTorrent.Common;
    using GSeries.ProvisionSupport;

    /// <summary>
    /// A chunk map maps disk chunks to their hashes. 
    /// </summary>
    public class ChunkMap {
        public class ChunkMapEntry {
            /// <summary>
            /// File index is the index of this chunk in the original file.
            /// </summary>
            public int FileIndex;
            public byte[] Hash;
        }

        ChunkMapDto _chunkMapDto;
        const int HashLength = 20;

        public ChunkMap(ChunkMapDto dto) {
            if (dto.FileIndices.Length != dto.Hashes.Length / HashLength) {
                throw new ArgumentException(
                    "Hashes and FileIndices don't match.", "dto");
            }
            _chunkMapDto = dto;
        }

        /// <summary>
        /// Gets the hashes in MonoTorrent's <see cref="Hashes"/> object.
        /// </summary>
        public Hashes Hashes {
            get {
                return new Hashes(_chunkMapDto.Hashes, 
                    _chunkMapDto.FileIndices.Length);
            }
        }

        /// <summary>
        /// Gets the <see cref="gSeries.BitTorrent.ChunkMap.ChunkMapEntry"/> at the 
        /// specified index in the torrent.
        /// </summary>
        /// <remarks>
        /// The index is different from the FileIndex in <see cref="ChunkMapEntry" />.
        /// </remarks>
        public ChunkMapEntry this[int index] {
            get {
                return new ChunkMapEntry() {
                    FileIndex = FileIndexAt(index),
                    Hash = HashAt(index)
                };
            }
        }

        public int FileIndexAt(int index) {
            return _chunkMapDto.FileIndices[index];
        }

        /// <summary>
        /// A convenience method to return the hash at the specified index.
        /// </summary>
        public byte[] HashAt(int index) {
            var hash = new byte[HashLength];
            Buffer.BlockCopy(_chunkMapDto.Hashes, index * HashLength, hash, 0, 
                HashLength);
            return hash;
        }
    }
}
