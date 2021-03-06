﻿// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
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
    using CuttingEdge.Conditions;

    public class DeduplicationService {
        ChunkDbService _chunkDbService;
        static readonly ILog logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);

        public DeduplicationService(ChunkDbService chunkDbService) {
            _chunkDbService = chunkDbService;
        }

        public List<Tuple<long, int>> MapFileIndicesToChunkIndices(
            string path, long offset, int count) {

            var fileInfo = GetManagedFileInfo(path);
            Condition.Requires<long>(offset).IsLessOrEqual(fileInfo.Size);

            int realCount = Math.Min((int)(fileInfo.Size - offset), count);

            if (realCount < count) {
                logger.DebugFormat(
                    "Can only read less ({0}) than requested count {1}.", 
                    realCount, count);
            }

            List<Tuple<string, long, int>> chunkList =
                MapChunks(path, offset, realCount, 
                delegate(string filePath, int[] fileIndices) {
                    List<Tuple<string, int, int>> ret =
                        _chunkDbService.GetChunkIndices(filePath, fileIndices);
                    return ret;
            });

            return chunkList.ConvertAll<Tuple<long, int>>(x => 
                Tuple.Create<long, int>(x.Item2, x.Item3));
        }

        public List<Tuple<string, long, int>> GetDedupFileSourceLocations(string path, 
            long offset, int count) {
            return MapChunks(path, offset, count, this.MapChunksToSourceLocations);
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
        /// Adds a file with chunk map and torrent information to the DB.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="chunkMap">The chunk map.</param>
        /// <remarks>This usually happens after the destination node downloads 
        /// the torrent and chunk map of the file.</remarks>
        public void AddFileToDownload(string path, byte[] chunkMap, byte[] torrent, long fileSize) {
            _chunkDbService.AddFileToDownload(path, chunkMap, torrent, fileSize);
        }

        /// <summary>
        /// Adds the file with chunk map to the DB.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="chunkMapDto">The chunk map.</param>
        /// <remarks>This usually happens on the source node to add/update 
        /// chunk map for a file.</remarks>
        public void AddFile(string path, byte[] chunkMapDto) {
            _chunkDbService.AddFile(path, chunkMapDto);
        }

        public bool CheckFileExists(string path) {
            if (File.Exists(path)) {
                return true;
            }
            try {
                _chunkDbService.GetManagedFile(path);
            } catch (ChunkDbException ex) {
                return false;
            }
            return true;
        }

        public ManagedFile GetManagedFileInfo(string path) {
            return _chunkDbService.GetManagedFile(path);
        }

        #region Private Methods
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
        List<Tuple<string, long, int>> MapChunks(string path, long offset, int count,
            Func<string, int[], List<Tuple<string, int, int>>> chunkMapper) {
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

            List<Tuple<string, int, int>> chunkLocationList = chunkMapper(path,
                Enumerable.Range((int)firstChunk, (int)(lastChunk - firstChunk + 1)).ToArray<int>());

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

        private List<Tuple<string, int, int>> MapChunksToSourceLocations(
            string path, int[] chunkIndices) {
            List<Tuple<string, int, int>> chunkLocationList;
            try {
                chunkLocationList = _chunkDbService
                        .GetChunkLocations(path, chunkIndices);
            } catch (ChunkNotInDbException ex) {
                throw new FileSegmentIncompleteException(ex) {
                    FirstMissingChunk = ex.ChunkIndex
                };
            }
            return chunkLocationList;
        }
        #endregion
    }
}
