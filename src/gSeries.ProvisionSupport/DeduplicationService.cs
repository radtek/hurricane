// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Reflection;
    using log4net;

    public class DeduplicationService {
        ChunkDbService _chunkDbService;
        static readonly ILog logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);

        public DeduplicationService(ChunkDbService chunkDbService) {
            _chunkDbService = chunkDbService;
        }

        /// <summary>
        /// Translate information from the original file to where the data 
        /// segments are actually stored.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <remarks>All chunks overlap with (offset, count) are returned with 
        /// offset and count adjusted.</remarks>
        /// <returns>List of tuples (path, offset, count)</returns>
        public IList<Tuple<string, long, int>> GetDedupFileParts(string path, long offset, int count) {
            long bytesToSkipInStartChunk;
            int firstChunk = (int)Math.DivRem(offset,
                (long)DataChunk.ChunkSize, out bytesToSkipInStartChunk);

            // The chunk that is after the last chunk needed.
            //(int)Math.Ceiling((double)(offset + count) / DataChunk.ChunkSize);
            long countInLastChunk;
            int lastChunk = (int)Math.DivRem(offset + count,
                (long)DataChunk.ChunkSize, out countInLastChunk);
            if (countInLastChunk == 0) {
                lastChunk--;
                countInLastChunk = DataChunk.ChunkSize;
            }

            IList<Tuple<string, int, int>> chunkLocationList;
            try {
                chunkLocationList = _chunkDbService
                        .GetChunkLocations(path, Enumerable.Range((int)firstChunk,
                            (int)(lastChunk - firstChunk + 1)).ToArray<int>());
            } catch (ChunkNotInDbException ex) {
                throw new FileSegmentIncompleteException(ex) {
                    FirstMissingChunk = ex.ChunkIndex
                };
            }

            // Usually not a long list.
            var ret = new List<Tuple<string, long, int>>();
            foreach (var tuple in chunkLocationList) {
                ret.Add(Tuple.Create<string, long, int>(tuple.Item1, (long)(tuple.Item2 * DataChunk.ChunkSize),
                    tuple.Item3));
            }

            // Adjust the first and last chunks.
            if (ret.Count == 1) {
                // Thus start == end.
                ret[0] = Tuple.Create<string, long, int>(ret[0].Item1,
                    ret[0].Item2 + bytesToSkipInStartChunk,
                    (int)(countInLastChunk - bytesToSkipInStartChunk));
            } else {
                ret[0] = Tuple.Create<string, long, int>(ret[0].Item1,
                    ret[0].Item2 + bytesToSkipInStartChunk,
                    DataChunk.ChunkSize - (int)bytesToSkipInStartChunk);
                ret[ret.Count - 1] = Tuple.Create<string, long, int>(
                    ret[ret.Count - 1].Item1,
                    ret[ret.Count - 1].Item2,
                    (int)countInLastChunk);
            }
            return ret;
        }

        /// <summary>
        /// Registers the file part so that other downloads can find it.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <remarks>The registration process does not involve reading the file.
        /// </remarks>
        public void RegisterFilePart(string filePath, long offset, int count) {
            int firstChunk = (int)Math.Ceiling((double)offset / DataChunk.ChunkSize);
            int lastChunk = (int)Math.Floor((double)(offset + count) / DataChunk.ChunkSize) - 1;

            _chunkDbService.AddChunks(filePath, 
                Enumerable.Range(firstChunk, lastChunk - firstChunk + 1).ToArray());
        }

        /// <summary>
        /// Adds the chunk map.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="chunkMap">The chunk map.</param>
        public void AddFileToDownload(string path, byte[] chunkMap, byte[] torrent, long fileSize) {
            _chunkDbService.AddFileToDownload(path, chunkMap, torrent, fileSize);
        }

        public bool FileExistsInDb(string path) {
            try {
                _chunkDbService.GetManagedFile(path);
            } catch (ChunkDbException ex) {
                return false;
            }
            return true;
        }
    }
}
