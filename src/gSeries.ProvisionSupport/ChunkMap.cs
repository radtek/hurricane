// Copyright (c) 2011 Xu, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using GSeries.ProvisionSupport;
    using System.Collections.ObjectModel;
    using System.IO;
using log4net;
    using System.Reflection;

    /// <summary>
    /// A chunk map maps disk chunks to their hashes. 
    /// </summary>
    /// <seealso cref="ChunkMapDto"/>
    public class ChunkMap {
        static readonly ILog logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);

        readonly ChunkMapDto _chunkMapDto;
        public ChunkMapDto ChunkMapDto {
            get { return _chunkMapDto; }
        }

        public ChunkMap(ChunkMapDto dto) {
            if (dto.FileIndices.Length != dto.Hashes.Length / DataChunk.HashSize) {
                throw new ArgumentException(
                    "Hashes and FileIndices don't match.", "dto");
            }
            _chunkMapDto = dto;
        }

        public static ChunkMap Create(byte[] serializedDto) {
            return new ChunkMap(ChunkMapSerializer.Deserialize(
                    new MemoryStream(serializedDto)));
        }

        /// <summary>
        /// An entry in the ChunkMap.
        /// </summary>
        public class ChunkMapEntry {
            public int FileIndex;
            public byte[] Hash;
        }

        public class EofChunkInfo {
            public int ChunkIndex;
            public int FileIndex;
            public byte[] ChunkHash;
            public int ChunkSize;
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

        public List<byte[]> HashesAsList {
            get {
                var ret = new List<byte[]>();
                for (int i = 0; i < _chunkMapDto.FileIndices.Length; i++) {
                    var hash = new byte[DataChunk.HashSize];
                    Buffer.BlockCopy(_chunkMapDto.Hashes, i * DataChunk.HashSize, 
                        hash, 0, hash.Length);
                    ret.Add(hash);
                }
                return ret;
            }
            set {
                using (var hashes = new MemoryStream()) {
                    foreach (var hash in value) {
                        hashes.Write(hash, 0, hash.Length);
                    }
                    _chunkMapDto.Hashes = hashes.ToArray();
                }
            }
        }

        public EofChunkInfo EofChunk {
            get {
                return new EofChunkInfo {
                    ChunkIndex = _chunkMapDto.EofChunkIndex,
                    FileIndex = FileIndexAt(_chunkMapDto.EofChunkIndex),
                    ChunkHash = HashAt(_chunkMapDto.EofChunkIndex),
                    ChunkSize = _chunkMapDto.EofChunkSize
                };
            }
        }

        /// <summary>
        /// Returns the file index of the chunk at the given index in the ChunkMap.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public int FileIndexAt(int index) {
            return _chunkMapDto.FileIndices[index];
        }

        /// <summary>
        /// A convenience method to return the (copy of the) hash at the 
        /// specified index.
        /// </summary>
        public byte[] HashAt(int index) {
            var hash = new byte[DataChunk.HashSize];
            Buffer.BlockCopy(_chunkMapDto.Hashes, index * DataChunk.HashSize, hash, 0,
                DataChunk.HashSize);
            return hash;
        }

        /// <summary>
        /// Gets the size of the file.
        /// </summary>
        /// <value>
        /// The size of the file.
        /// </value>
        public long FileSize {
            get {
                return DataChunk.ChunkSize * (_chunkMapDto.FileIndices.Length - 1) 
                    + _chunkMapDto.EofChunkSize;
            }
        }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append(string.Format("{{File Indices: [{0}], ", string.Join(",", 
                _chunkMapDto.FileIndices)));
            var hexStrList = HashesAsList.ConvertAll<string>(x => BitConverter.ToString(x));
            sb.Append(string.Format("Hashes: [{0}],", string.Join(",", hexStrList)));
            sb.Append(string.Format("Eof Chunk Index: {0}, ", _chunkMapDto.EofChunkIndex));
            sb.Append(string.Format("Eof Chunk Size: {0}}}", _chunkMapDto.EofChunkSize));
            return sb.ToString();
        }
    }
}
