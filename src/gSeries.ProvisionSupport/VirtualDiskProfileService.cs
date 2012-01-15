// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using FluentNHibernate.Cfg;
    using FluentNHibernate.Cfg.Db;
    using NHibernate;
    using NHibernate.Cfg;
    using NHibernate.Tool.hbm2ddl;
    using log4net;
    using NHibernate.Criterion;

    /// <summary>
    /// This service class deals with the VM disk profile.
    /// </summary>
    public class VirtualDiskProfileService {
        #region Fields
        readonly string _profileDbFile;
        readonly string _dataFile;
        readonly ChunkDbService _chunkDbService;
        static readonly ILog logger = LogManager.GetLogger(typeof(VirtualDiskProfileService));
        readonly ISessionFactory _sessionFactory;
        #endregion

        #region Properties
        public string DataFile {
            get { return _dataFile; }
        }
        public string ProfileDbFile { get { return _profileDbFile; } } 
        #endregion

        public VirtualDiskProfileService(string profileDbFile, string dataFile, ChunkDbService chunkDbService) {
            _profileDbFile = profileDbFile;
            _dataFile = dataFile;
            _sessionFactory = CreateSessionFactory();
            _chunkDbService = chunkDbService;
        }

        /// <summary>
        /// Creates the HN session factory.
        /// </summary>
        /// <returns></returns>
        private ISessionFactory CreateSessionFactory() {
            return Fluently.Configure()
                .Database(SQLiteConfiguration.Standard
                    .UsingFile(ProfileDbFile))
                .Mappings(m =>
                    m.FluentMappings.Add<ChunkAccessStatsMap>())
                .BuildSessionFactory();
        }

        public ChunkAccessStats[] GetChunksOrderedByEarliestRead() {
            using (var session = _sessionFactory.OpenSession()) {
                var stats = session.CreateCriteria(typeof(ChunkAccessStats))
                    .AddOrder(Order.Asc(Projections.Property<ChunkAccessStats>(x => x.EarliestRead)))
                    .List<ChunkAccessStats>();
                return stats.ToArray();
            }
        }

        public ChunkMapDto ToChunkMapDto() {
            ManagedFile file = _chunkDbService.GetManagedFile(_dataFile);
            int totalChunkNumber = (int)(file.Size / DataChunk.ChunkSize);
            logger.DebugFormat("Total number of chunk for this file: {0}.", 
                totalChunkNumber);

            ChunkAccessStats[] chunks = GetChunksOrderedByEarliestRead();
            logger.DebugFormat("Brought in totally {0} chunks from profile.", 
                chunks.Length);
            
            // The hashes have two parts: in the profile and out of the profile.
            int[] fileIndices = new int[totalChunkNumber];
            byte[] hashes = new byte[totalChunkNumber * DataChunk.HashSize];
            HashSet<int> indicesSet = new HashSet<int>();

            // Number of chunks read directly from file instead of pulled from DB.
            int numDirectRead = 0;
            int fileIndicesCur = 0;
            for (; fileIndicesCur < chunks.Length; fileIndicesCur++) {
                var chunk = chunks[fileIndicesCur];
                fileIndices[fileIndicesCur] = chunk.ChunkNumber;
                indicesSet.Add(chunk.ChunkNumber);
                bool readFile;
                byte[] hash = GetHashFromChunkDbOrFile(chunk.ChunkNumber, out readFile);
                if (readFile) numDirectRead++;
                Buffer.BlockCopy(hash, 0, hashes, fileIndicesCur * DataChunk.HashSize, hash.Length);
            }

            int inProfileChunkNum = fileIndicesCur;
            logger.DebugFormat("Finished adding {0} in the profile chunks.", inProfileChunkNum);

            // Begin handling out of the profile chunks.
            for (int i = 0; i < totalChunkNumber; i++) {
                if (!indicesSet.Contains(i)) {
                    // This chunk is not in the profile
                    fileIndices[fileIndicesCur] = i;
                    bool readFile;
                    byte[] hash = GetHashFromChunkDbOrFile(i, out readFile);
                    if (readFile) numDirectRead++;
                    Buffer.BlockCopy(hash, 0, hashes, fileIndicesCur * DataChunk.HashSize, hash.Length);
                    fileIndicesCur++;
                }
            }

            logger.DebugFormat("Finished adding {0} out of profile chunks.", 
                fileIndicesCur - inProfileChunkNum);
            logger.DebugFormat(
                "There are {0} chunks in total read directly from file.",
                numDirectRead);

            return new ChunkMapDto {
                Hashes = hashes,
                FileIndices = fileIndices
            };
        }

        private byte[] GetHashFromChunkDbOrFile(int chunkNumber, out bool readFile) {
            DataChunk entry = _chunkDbService.GetChunkEntry(DataFile,
                chunkNumber);
            byte[] hash;
            if (entry == null) {
                // This chunk is not in ChunkDB maybe because it's a duplicate.
                // Read directly from file.
                hash = FileHelper.GetChunkHash(DataFile,
                    chunkNumber * DataChunk.ChunkSize);
                readFile = true;
            } else {
                hash = entry.Hash;
                readFile = false;
            }
            return hash;
        }
    }
}
