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
    using System.Collections.Specialized;

    /// <summary>
    /// A chunk map maps disk chunks to their hashes. 
    /// </summary>
    /// <seealso cref="ChunkMapDto"/>
    [Serializable]
    public class ChunkMap {
        static readonly ILog logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);
        #region Data Fields
        
        /// <summary>
        /// File index is the index of this chunk in the original file.
        /// Chunk Index => File Index.
        /// </summary>
        public int[] FileIndices;
        /// <summary>
        /// Hashes concatenated according to the chunk indices.
        /// </summary>
        public byte[] Hashes;

        /// <summary>
        /// The size of the End-of-file chunk could be smaller than regular chunk size.
        /// </summary>
        public int EofChunkSize;

        /// <summary>
        /// The index of the EoF file chunk in the FileIndices array.
        /// </summary>
        public int EofChunkIndex;

        /// <summary>
        /// Mapping from file indices to chunk indices. It's the reverse index
        /// of FileIndices and is created to speed up queries.
        /// File Index => Chunk Index.
        /// </summary>
        public int[] ChunkIndices;

        /// <summary>
        /// If it equals 0. Then the file doesn't have a profile and should be 
        /// downloaded through randomly through BitTorrent optimization. If -1,
        /// the the entire file needs to be downloaded through a sliding window.
        /// </summary>
        public int LastPieceInProfile;

        #endregion


        /// <summary>
        /// For XmlSerializer
        /// </summary>
        public ChunkMap() { }

        public ChunkMap(ChunkMapDto dto) {
            if (dto.FileIndices.Length != dto.Hashes.Length / DataChunk.HashSize) {
                throw new ArgumentException(
                    "Hashes and FileIndices don't match.", "dto");
            }

            FileIndices = dto.FileIndices;
            EofChunkIndex = dto.EofChunkIndex;
            EofChunkSize = dto.EofChunkSize;
            Hashes = dto.Hashes;
            LastPieceInProfile = dto.LastPieceInProfile;

            GenerateChunkIndices();
        }

        internal void GenerateChunkIndices() {
            ChunkIndices = new int[FileIndices.Length];
            for (int chunkIndex = 0; chunkIndex < FileIndices.Length; chunkIndex++) {
                ChunkIndices[FileIndices[chunkIndex]] = chunkIndex;
            }
        }

        public static ChunkMap Create(byte[] serializedDto) {
            return new ChunkMap(ChunkMapSerializer.Deserialize(
                    new MemoryStream(serializedDto)));
        }

        public ChunkMapDto ConvertToDto() {
            return new ChunkMapDto {
                Hashes = this.Hashes,
                FileIndices = this.FileIndices,
                EofChunkIndex = EofChunkIndex,
                EofChunkSize = EofChunkSize,
                LastPieceInProfile = LastPieceInProfile
            };
        }

        /// <summary>
        /// An entry in the ChunkMap.
        /// </summary>
        public class ChunkMapEntry {
            public int FileIndex;
            public int ChunkIndex;
            public byte[] Hash;
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
                    Hash = HashAt(index),
                    ChunkIndex = index,
                    ChunkSize = index == EofChunkIndex ? EofChunkSize 
                        : DataChunk.ChunkSize
                };
            }
        }

        public ChunkMapEntry GetByFileIndex(int fileIndex) {
            int chunkIndex = ChunkIndices[fileIndex];
            return this[chunkIndex];
        }

        public List<byte[]> CopyHashesAsList() {
            var ret = new List<byte[]>();
            for (int i = 0; i < FileIndices.Length; i++) {
                var hash = new byte[DataChunk.HashSize];
                Buffer.BlockCopy(Hashes, i * DataChunk.HashSize,
                    hash, 0, hash.Length);
                ret.Add(hash);
            }
            return ret;
        }

        internal void SetHashesAsList(List<byte[]> value) {
            // This is used by tests to manually modify the chunk map.
            using (var hashes = new MemoryStream()) {
                foreach (var hash in value) {
                    hashes.Write(hash, 0, hash.Length);
                }
                Hashes = hashes.ToArray();
            }
        }

        public ChunkMapEntry EofChunk {
            get {
                return this[EofChunkIndex];
            }
        }

        /// <summary>
        /// Returns the file index of the chunk at the given index in the ChunkMap.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public int FileIndexAt(int index) {
            return FileIndices[index];
        }

        /// <summary>
        /// A convenience method to return the (copy of the) hash at the 
        /// specified index.
        /// </summary>
        public byte[] HashAt(int index) {
            var hash = new byte[DataChunk.HashSize];
            Buffer.BlockCopy(Hashes, index * DataChunk.HashSize, hash, 0,
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
                return DataChunk.ChunkSize * (FileIndices.Length - 1) 
                    + EofChunkSize;
            }
        }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append(string.Format("{{File Indices: [{0}], ", string.Join(",", 
                FileIndices)));
            var hexStrList = CopyHashesAsList().ConvertAll<string>(x => BitConverter.ToString(x));
            sb.Append(string.Format("Hashes: [{0}],", string.Join(",", hexStrList)));
            sb.Append(string.Format("Eof Chunk Index: {0}, ", EofChunkIndex));
            sb.Append(string.Format("Eof Chunk Size: {0}}}", EofChunkSize));
            return sb.ToString();
        }
    }
}
