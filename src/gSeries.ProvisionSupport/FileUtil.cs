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
    public class FileUtil {
        #region Fields
        static readonly ILog logger = LogManager.GetLogger(typeof(FileUtil));
        readonly ChunkDbService _chunkDbService; 
        #endregion

        public FileUtil(ChunkDbService chunkDbService) {
            _chunkDbService = chunkDbService;
        }

        #region Member Methods
        /// <summary>
        /// Gets the hash from chunk db or file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="chunkNumber">The chunk number.</param>
        /// <param name="readFile">Is set to <c>true</c> if the chunk hash is read 
        /// from file instead Db.</param>
        /// <returns></returns>
        public byte[] GetHashFromChunkDbOrFile(string filePath, int chunkNumber, 
            out bool readFile) {
            DataChunk entry = _chunkDbService.GetChunkEntry(filePath,
                chunkNumber);
            byte[] hash;
            if (entry == null) {
                // This chunk is not in ChunkDB maybe because it's a duplicate.
                // Read directly from file.
                hash = FileUtil.GetChunkHash(filePath,
                    chunkNumber * DataChunk.ChunkSize);
                readFile = true;
            } else {
                hash = entry.Hash;
                readFile = false;
            }
            return hash;
        }

        /// <summary>
        /// Reads the chunk from local file system by querying the chunk db.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns></returns>
        public byte[] GetChunk(byte[] hash) {
            DataChunk entry = _chunkDbService.GetChunkEntry(hash);
            if (entry == null) {
                throw new ChunkDbException(
                    "Cannot find the data chunk by the hash.");
            }
            int readLength;
            return ReadChunk(entry.File.Path, 
                entry.ChunkIndex * DataChunk.ChunkSize, out readLength);
        }

        public void AddChunk(string filePath, int fileIndex) {  
            SHA1 sha = new SHA1CryptoServiceProvider();
            int readLength;
            byte[] chunk = ReadChunk(filePath, fileIndex, out readLength);
            byte[] hash = sha.ComputeHash(chunk);
            try {
                _chunkDbService.AddChunk(hash, filePath, fileIndex);
            } catch (DuplicateNameException ex) {
                throw new ChunkDbException(
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
                throw new ChunkDbException("Cannot read this chunk from file.");
            }
            return sha.ComputeHash(chunk);
        }

        public static byte[] GetFileHash(string filePath) {
            if (!File.Exists(filePath)) {
                throw new ArgumentException(string.Format(
                    "File path {0} doesn't exist.", filePath));
            }
            SHA1 sha = new SHA1CryptoServiceProvider();
            using (File.OpenRead(filePath)) {
                return sha.ComputeHash(File.OpenRead(filePath));
            }
        }

        public static int SizeOfLastChunk(long fileSize) {
            long numChunks;
            long remainder = Math.DivRem(fileSize, (long)DataChunk.ChunkSize, out numChunks);
            return remainder == 0 ? DataChunk.ChunkSize : (int)remainder;
        }

        /// <summary>
        /// Pads the file with zeros.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>True if padded the file and false if no need to.</returns>
        public static bool PadFileWithZeros(string path) {
            if (!File.Exists(path)) {
                throw new ArgumentException(
                    string.Format("File {0} doesn't exist.", path));
            }
            var payloadSize = new FileInfo(path).Length;
            int chunks = (int)Math.Ceiling((double)payloadSize / 
                DataChunk.ChunkSize);
            long fileFullSize = chunks * DataChunk.ChunkSize;
            int paddingSize = (int)(fileFullSize - payloadSize);
            if (paddingSize > 0) {
                using (var file = File.Open(path, FileMode.Append, 
                    FileAccess.Write, FileShare.ReadWrite)) {
                    var padding = new byte[paddingSize];
                    file.Write(padding, 0, padding.Length);
                }
                logger.DebugFormat(
                    "Original file size is {0}. Padded with {1} zeros to size {2}.", 
                    payloadSize, paddingSize, fileFullSize);
                return true;
            } else {
                return false;
            }
        }

        public static bool IsFilePadded(string file) {
            if (!File.Exists(file)) {
                throw new ArgumentException(
                    string.Format("File {0} doesn't exist.", file));
            }
            var fileSize = new FileInfo(file).Length;
            long remainder;
            Math.DivRem(fileSize, DataChunk.ChunkSize, out remainder);
            return remainder == 0 ? true : false;
        }

        #endregion
    }
}
