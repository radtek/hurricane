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
    public class FullProfileChunkMapBuilder {
        #region Fields
        readonly string _profileDbFile;
        readonly ChunkDbService _chunkDbService;
        FileHelper _fileHelper;
        static readonly ILog logger = LogManager.GetLogger(typeof(FullProfileChunkMapBuilder));
        readonly ISessionFactory _sessionFactory;
        string _datafilePath;
        #endregion

        #region Properties
        public string ProfileDbFile { get { return _profileDbFile; } }
        public string DataFilePath { get { return _datafilePath; } } 
        #endregion

        public FullProfileChunkMapBuilder(string profileDbFile, string dataFilePath, 
            ChunkDbService chunkDbService) {
            _profileDbFile = profileDbFile;
            _sessionFactory = CreateSessionFactory();
            _chunkDbService = chunkDbService;
            _fileHelper = new FileHelper(_chunkDbService);
            _datafilePath = dataFilePath;
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

        ChunkAccessStats[] GetChunksOrderedByEarliestRead() {
            using (var session = _sessionFactory.OpenSession()) {
                var stats = session.CreateCriteria(typeof(ChunkAccessStats))
                    .AddOrder(Order.Asc(Projections.Property<ChunkAccessStats>(x => x.EarliestRead)))
                    .List<ChunkAccessStats>();
                return stats.ToArray();
            }
        }

        public ChunkMapDto BuildChunkMapDto() {
            ManagedFile file = _chunkDbService.GetManagedFile(DataFilePath);
            int totalChunkNumber = (int)(file.Size / DataChunk.ChunkSize);
            logger.DebugFormat("Total number of chunk for this file: {0}.", 
                totalChunkNumber);

            ChunkAccessStats[] chunkStatss = GetChunksOrderedByEarliestRead();
            logger.DebugFormat("Brought in totally {0} chunks from profile.", 
                chunkStatss.Length);
            
            // The hashes have two parts: in the profile and out of the profile.
            int[] fileIndices = new int[totalChunkNumber];
            byte[] hashes = new byte[totalChunkNumber * DataChunk.HashSize];
            HashSet<int> indicesSet = new HashSet<int>();

            // Number of chunks read directly from file instead of pulled from DB.
            int numDirectRead = 0;
            int fileIndicesCur = 0;
            for (; fileIndicesCur < chunkStatss.Length; fileIndicesCur++) {
                var chunk = chunkStatss[fileIndicesCur];
                fileIndices[fileIndicesCur] = chunk.ChunkNumber;
                indicesSet.Add(chunk.ChunkNumber);
                bool readFile;
                byte[] hash = _fileHelper.GetHashFromChunkDbOrFile(DataFilePath, 
                    chunk.ChunkNumber, out readFile);
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
                    byte[] hash = _fileHelper.GetHashFromChunkDbOrFile(DataFilePath, 
                        i, out readFile);
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
    }
}
