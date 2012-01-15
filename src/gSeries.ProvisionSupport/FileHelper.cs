// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Security.Cryptography;
    using System.Data;
    using log4net;

    /// <summary>
    /// This class contains chunk related methods that deal with files.
    /// </summary>
    public class FileHelper {
        #region Fields
        static readonly ILog logger = LogManager.GetLogger(typeof(FileHelper));
        readonly ChunkDbService _chunkDb; 
        #endregion

        public FileHelper(ChunkDbService chunkDb) {
            _chunkDb = chunkDb;
        }

        #region Member Methods
        /// <summary>
        /// Reads the chunk from local file system by querying the chunk db.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns></returns>
        public byte[] GetChunk(byte[] hash) {
            DataChunk entry = _chunkDb.GetChunkEntry(hash);
            if (entry == null) {
                throw new DataChunkException(
                    "Cannot find the data chunk by the hash.");
            }
            int readLength;
            return ReadChunk(entry.File.Path, entry.FileIndex * DataChunk.ChunkSize, out readLength);
        }

        public void AddChunk(string filePath, int fileIndex) {  
            SHA1 sha = new SHA1CryptoServiceProvider();
            int readLength;
            byte[] chunk = ReadChunk(filePath, fileIndex, out readLength);
            byte[] hash = sha.ComputeHash(chunk);
            try {
                _chunkDb.AddChunk(hash, filePath, fileIndex);
            } catch (DuplicateNameException ex) {
                throw new DataChunkException(
                    "Cannot add chunk to ChunkDB because duplicate exists.", ex);
            }
        }
        #endregion

        #region Static Methods
        public static byte[] ReadChunk(string filePath, long offset,
            out int readLength) {
            using (var stream = File.OpenRead(filePath)) {
                var chunk = new byte[DataChunk.ChunkSize];
                stream.Seek(offset, SeekOrigin.Begin);
                readLength = stream.Read(chunk, 0, chunk.Length);
                return chunk;
            }
        }

        public static byte[] GetChunkHash(string filePath, long offset) {
            SHA1 sha = new SHA1CryptoServiceProvider();
            int readLength;
            byte[] chunk = ReadChunk(filePath, offset, out readLength);
            if (readLength == 0) {
                throw new DataChunkException("Cannot read this chunk from file.");
            }
            return sha.ComputeHash(chunk);
        }

        public static byte[] GetFileHash(string filePath) {
            SHA1 sha = new SHA1CryptoServiceProvider();
            return sha.ComputeHash(File.OpenRead(filePath));
        }
        #endregion
    }
}
