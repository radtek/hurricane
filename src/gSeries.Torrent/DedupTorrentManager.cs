// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.BitTorrent {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using MonoTorrent.Client;
    using System.Security.Cryptography;
    using MonoTorrent.Common;

    /// <summary>
    /// The central component that manages the file system chunks in the 
    /// BitTorrent processes.
    /// </summary>
    public class DedupTorrentManager {
        public const int ChunkSize = Piece.BlockSize;
        TorrentManager _torrentManager;
        ChunkMap _chunkMap;

        public DedupTorrentManager(TorrentManager torrentManager, 
            ChunkMap chunkMap) {
            _torrentManager = torrentManager;
            _chunkMap = chunkMap;
        }

        /// <summary>
        /// Checks the chunk data against the hash in <see cref="ChunkMap"/> to
        /// see if it is available on the host.
        /// </summary>
        /// <param name="pieceIndex">Index of the piece.</param>
        /// <param name="blockIndex">Index of the block in the piece.</param>
        /// <param name="data">The data.</param>
        public void CheckChunk(int pieceIndex, int blockIndex, 
            byte[] data) {
            SHA1 hasher = HashAlgoFactory.Create<SHA1>();
            byte[] hash = hasher.ComputeHash(data);
            int chunkIndex = pieceIndex * _torrentManager.Torrent.PieceLength + 
                blockIndex;
            bool isValid = _chunkMap.Hashes.IsValid(hash, chunkIndex);
            if (isValid) {
                //_torrentManager.
            }
        }

    }
}
