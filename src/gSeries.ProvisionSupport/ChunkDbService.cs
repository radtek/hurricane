// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Data;
    using System.IO;
    using System.Linq;
    using FluentNHibernate.Cfg;
    using FluentNHibernate.Cfg.Db;
    using log4net;
    using NHibernate;
    using NHibernate.Cfg;
    using NHibernate.Criterion;
    using NHibernate.Tool.hbm2ddl;
    using NHibernate.Context;
    using System.Security.Cryptography;
    using System.Collections.Generic;
    using NHibernate.Mapping;
    using NHibernate.Caches.SysCache;

    /// <summary>
    /// A service class that handles tasks pertaining to ChunkDb but not limited
    /// to only DB access.
    /// </summary>
    /// <remarks>This implementation uses Fluent NHibernate.
    /// Db/NHibernate specific operations are put in <see cref="ChunkDbHelper"/>.
    /// </remarks>
    public class ChunkDbService {
        #region Fields
        static readonly ILog logger = LogManager.GetLogger(typeof(ChunkDbService));
        readonly string _dbFile;
        /// <summary>
        /// The session factory shared by all methods in this object.
        /// </summary>
        private readonly ISessionFactory _sessionFactory; 
        #endregion

        #region Properties & Constructors
        public string DbFile { get { return _dbFile; } }
        public ISessionFactory SessionFactory { get { return _sessionFactory; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkDbService"/> class with 
        /// the path to the db file.
        /// </summary>
        /// <param name="dbFile">The db file.</param>
        internal ChunkDbService(string dbFile, bool exportSchema) {
            _dbFile = dbFile;
            _sessionFactory = CreateSessionFactory(exportSchema);
        } 
        #endregion

        public void AddFileToDownload(string path, byte[] chunkMap, 
            byte[] torrent, long fileSize) {
            var txnProvider = new NHTransactionProvider(
                new NHSessionProvider(_sessionFactory));
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    ManagedFile managedFile =
                        new ManagedFile {
                            Path = path,
                            ChunkMap = chunkMap,
                            TorrentFile = torrent,
                            Size = fileSize
                        };
                    var session = txnProvider.SessionProvider.CurrentSession;
                    session.Save(managedFile);
                    transaction.Commit();
                }
            }
            logger.DebugFormat("ChunkMap is added for file {0}", path);
        }

        /// <summary>
        /// Adds the specified entry.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <exception cref="DuplicateNameException">Thrown when there is 
        /// already an entry with the same hash exists.</exception>
        public void AddChunk(byte[] hash, string filePath, int chunkIndex) {
            var txnProvider = new NHTransactionProvider(
                new NHSessionProvider(_sessionFactory));
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    var helper = new ChunkDbHelper(
                        txnProvider.SessionProvider.CurrentSession);
                    ManagedFile file = helper.GetManagedFile(filePath);
                    var entry = new DataChunk {
                        File = file,
                        ChunkIndex = chunkIndex,
                        Hash = hash,
                        Count = 0
                    };
                    helper.AddChunk(entry);
                    transaction.Commit();
                }
            }   // Dispose session.
        }

        public void AddChunks(string filePath, int[] chunks) {
            logger.DebugFormat("Adding chunks {0} for file {1}.", 
                string.Join(",", System.Array.ConvertAll<int, string>(chunks, 
                x => x.ToString())), filePath);
            var txnProvider = new NHTransactionProvider(
                new NHSessionProvider(_sessionFactory));
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    var helper = new ChunkDbHelper(
                        txnProvider.SessionProvider.CurrentSession);
                    ManagedFile file = helper.GetManagedFile(filePath);
                    var chunkMap = ChunkMap.Create(file.ChunkMap);
                    int numAlreadyExist = 0;
                    foreach (var chunkIndex in chunks) {
                        byte[] hash = chunkMap.HashAt(chunkIndex);
                        var entry = new DataChunk {
                            File = file,
                            ChunkIndex = chunkIndex,
                            Hash = hash,
                            Count = 0
                        };
                        bool added = helper.AddChunkIfNotExists(entry);
                        if (!added) { numAlreadyExist++; }
                    }
                    transaction.Commit();
                    logger.DebugFormat(
                        "Chunks added. {0} out of {1} chunks already exist.", 
                        numAlreadyExist, chunks.Length);
                }
            }
        }

        public void AddFileAllChunks(string filePath) {
            AddFileAllChunks(filePath, null, null);
        }

        public void AddFileWithBasicChunkMap(string filePath) {
            FileUtil.PadFileWithZeros(filePath);

            var fileIndices = new List<int>();
            var hashes = new MemoryStream();
            int eofIndex = 0;
            int eofChunkSize = 0;

            AddFileAllChunks(filePath, 
                delegate(int chunkIndex, byte[] hash) {
                    fileIndices.Add(chunkIndex);
                    hashes.Write(hash, 0, hash.Length);
                }, 
                (i, s) => { eofIndex = i; eofChunkSize = s; });

            logger.DebugFormat("(eofIndex, eofChunkSize) = ({0}, {1})", 
                eofIndex, eofChunkSize);

            var txnProvider = new NHTransactionProvider(
                new NHSessionProvider(_sessionFactory));
            var session = txnProvider.SessionProvider.CurrentSession;
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    var helper = new ChunkDbHelper(session);
                    // File should have been inserted already.
                    var file = helper.GetManagedFile(filePath);
                    var dto = new ChunkMapDto {
                        FileIndices = fileIndices.ToArray(),
                        Hashes = hashes.ToArray(),
                        EofChunkIndex = eofIndex,
                        EofChunkSize = eofChunkSize
                    };
                    hashes.Dispose();
                    var chunkMapBytes = new MemoryStream();
                    ChunkMapSerializer.Serialize(chunkMapBytes, dto);
                    if (logger.IsDebugEnabled) {
                        var tempFilePath = Path.Combine(Path.GetTempPath(), 
                            Path.GetTempFileName());
                        File.WriteAllBytes(tempFilePath, chunkMapBytes.ToArray());
                        logger.DebugFormat("ChunkMap is logged to file {0}", tempFilePath);
                    }
                    file.ChunkMap = chunkMapBytes.ToArray();
                    chunkMapBytes.Dispose();
                    // Have the file committed to DB.
                    transaction.Commit();
                }
                logger.DebugFormat("ChunkMap added to file.");
            }
        }

        /// <summary>
        /// Adds a file to the Chunk DB.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="forEachChunk">(index, hash)</param>
        /// <param name="forEofChunk">(index, chunk size)</param>
        public void AddFileAllChunks(string filePath, Action<int, byte[]> forEachChunk, Action<int, int> forEofChunk) {
            var txnProvider = new NHTransactionProvider(
                new NHSessionProvider(_sessionFactory));

            var session = txnProvider.SessionProvider.CurrentSession;
            ManagedFile file;
            // In a stateful session.
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    var helper = new ChunkDbHelper(session);
                    file = helper.CreateManagedFileFromLocalFile(filePath);
                    // Have the file committed to DB.
                    transaction.Commit();
                }
            }

            // Choose stateless session for bulk insert.
            var statelessSession = _sessionFactory.OpenStatelessSession();
            using (var transaction = statelessSession.BeginTransaction()) {
                SHA1 sha = new SHA1CryptoServiceProvider();
                using (var stream = File.OpenRead(filePath)) {
                    int chunkIndex = 0;
                    var chunk = new byte[DataChunk.ChunkSize];
                    int duplicates = 0;
                    bool isEofChunk = false;
                    for (; ; chunkIndex++) {
                        long offset = stream.Position;
                        int readLength = stream.Read(chunk, 0, chunk.Length);

                        if (readLength == 0) {
                            if (forEofChunk != null) {
                                forEofChunk(chunkIndex - 1, DataChunk.ChunkSize);
                            }
                            break;
                        }

                        if (readLength < DataChunk.ChunkSize) {
                            // Last chunk.
                            isEofChunk = true;
                            // The rest of the buffer is padded with 0s.
                            System.Array.Clear(chunk, readLength, 
                                chunk.Length - readLength);
                            if (forEofChunk != null) {
                                forEofChunk(chunkIndex, readLength);
                            }
                        }

                        // Hash is computed over the full chunk buffer with 
                        // padding in case of a small chunk.
                        byte[] hash = sha.ComputeHash(chunk);

                        if (forEachChunk != null) {
                            forEachChunk(chunkIndex, hash);
                        }

                        bool alreadyExists = ChunkDbHelper.AddChunkIfNotExists(statelessSession, 
                            new DataChunk {
                            Hash = hash,
                            File = file,
                            ChunkIndex = chunkIndex,
                            Count = 0
                        });
                        if (alreadyExists) duplicates++;

                        if (isEofChunk) break;
                    }
                    transaction.Commit();
                    logger.DebugFormat("File {0} added to ChunkDb.", file);
                    logger.DebugFormat("Number of duplicates {0}", duplicates);
                }
            }
        }

        public ManagedFile GetManagedFile(string filePath) {
            var txnProvider = new NHTransactionProvider(
                new NHSessionProvider(_sessionFactory));
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    var helper = new ChunkDbHelper(
                        txnProvider.SessionProvider.CurrentSession);
                    return helper.GetManagedFile(filePath);
                }
            }
        }

        /// <summary>
        /// Gets a list of the tuples <path, chunk index, chunk size> for each input chunk.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="chunkIndices">The chunk indices.</param>
        public IList<Tuple<string, int, int>> GetChunkLocations(string filePath, int[] chunkIndices) {
            logger.DebugFormat("Going to get chunks {0}.", string.Join(",", 
                System.Array.ConvertAll<int, string>(chunkIndices, x => x.ToString())));
            var sessionProvider = new NHSessionProvider(_sessionFactory);
            using (sessionProvider) {
                var helper = new ChunkDbHelper(sessionProvider.CurrentSession);
                var txnProvider = new NHTransactionProvider(sessionProvider);
                ManagedFile file;
                using (var transaction = txnProvider.BeginTransaction()) {
                    file = helper.GetManagedFile(filePath);
                }
                var chunkMap = ChunkMap.Create(file.ChunkMap);
                var eofChunk = chunkMap.EofChunk;
                var ret = new List<Tuple<string, int, int>>();
                using (var transaction = txnProvider.BeginTransaction()) {
                    var dao = new Dao<DataChunk>(sessionProvider.CurrentSession);
                    foreach (int chunkIndex in chunkIndices) {
                        byte[] chunkHash = chunkMap.HashAt(chunkIndex);
                        int chunkSize = chunkIndex == eofChunk.ChunkIndex ?
                            eofChunk.ChunkSize : DataChunk.ChunkSize;
                        var chunkInfo = dao.UniqueResultByExample(new 
                            DataChunk { Hash = chunkHash });

                        if (chunkInfo == null) {
                            throw new ChunkNotInDbException(string.Format(
                                "Chunk {0} in the file hasn't been added yet.",
                                chunkIndex)) {
                                File = filePath,
                                ChunkIndex = chunkIndex
                            };
                        }

                        var fileTuple = Tuple.Create<string, int, int>(
                            chunkInfo.File.Path, 
                            chunkInfo.ChunkIndex,
                            chunkSize);
                        ret.Add(fileTuple);
                    }
                }
                return ret;
            }
        }

        public void MakePersistent<T>(T entity) {
            var txnProvider = new NHTransactionProvider(
                new NHSessionProvider(_sessionFactory));
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    new Dao<T>(txnProvider.SessionProvider.CurrentSession)
                        .MakePersistent(entity);
                }
            }
        }



        /// <summary>
        /// Gets the chunk entry.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns>The entry can be null if not found.</returns>
        public DataChunk GetChunkEntry(byte[] hash) {
            var txnProvider = new NHTransactionProvider(
                new NHSessionProvider(_sessionFactory));
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    var helper = new ChunkDbHelper(
                        txnProvider.SessionProvider.CurrentSession);
                    ICriterion hashEq = 
                        Expression.Eq(Projections.Property<DataChunk>(x => x.Hash), 
                            hash);
                    var session = txnProvider.SessionProvider.CurrentSession;
                    ICriteria crit = session.CreateCriteria(typeof(DataChunk));
                    crit.Add(hashEq);
                    DataChunk entry = (DataChunk)crit.UniqueResult();
                    if (entry != null) {
                        entry.Count++;
                        session.Update(entry);
                    }
                    return entry;
                }
            }
        }

        /// <summary>
        /// Gets the chunk entry by the path and fileIndex.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="chunkIndex">The file index.</param>
        /// <returns></returns>
        public DataChunk GetChunkEntry(string filePath, int chunkIndex) {
            var txnProvider = new NHTransactionProvider(
                    new NHSessionProvider(_sessionFactory));
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    var helper = new ChunkDbHelper(
                        txnProvider.SessionProvider.CurrentSession);
                    var session = txnProvider.SessionProvider.CurrentSession;
                    ManagedFile file = helper.GetManagedFile(filePath);
                    ICriteria crit = session.CreateCriteria<DataChunk>();
                    crit.Add(Expression.Eq(Projections.Property<DataChunk>(x => x.File), file))
                        .Add(Expression.Eq(Projections.Property<DataChunk>(x => x.ChunkIndex), chunkIndex));
                    DataChunk entry = crit.UniqueResult<DataChunk>();
                    return entry;
                }
            }
        }

        #region Private Methods - NHibernate Session Management
        /// <summary>
        /// Creates the HN session factory.
        /// </summary>
        /// <returns></returns>
        private ISessionFactory CreateSessionFactory(bool exportSchema) {
            return Fluently.Configure()
                .Database(SQLiteConfiguration.Standard
                    .UsingFile(DbFile)
                    //.ShowSql()
                    //.AdoNetBatchSize(20))
                    )
                //.Database(MySQLConfiguration.Standard.ConnectionString(
                //    "Server=localhost;Database=newChunkDb;Uid=root;Pwd=password;")
                //    .AdoNetBatchSize(500))
                .Cache(c => c.ProviderClass<SysCacheProvider>()
                    .UseSecondLevelCache()
                    .UseQueryCache())
                //.Diagnostics(x => x.Enable().OutputToConsole())
                .Mappings(m =>
                    m.FluentMappings.Add<DataChunkMap>().Add<ManagedFileMap>())
                .ExposeConfiguration(c => 
                    HandleConfiguration(c, exportSchema))
                .BuildSessionFactory();
        }

        private static void HandleConfiguration(Configuration config, 
            bool exportSchema) {
            //var scopes = new Iesi.Collections.Generic.HashedSet<string>();
            //scopes.Add("NHibernate.Dialect.MySQLDialect");
            //config.AddAuxiliaryDatabaseObject(new SimpleAuxiliaryDatabaseObject(
            //    "CREATE UNIQUE INDEX HashUniqueness_Index ON `DataChunk` (Hash(20))",
            //    "DROP INDEX HashUniqueness_Index", scopes));
            //var scopes1 = new Iesi.Collections.Generic.HashedSet<string>();
            //scopes1.Add("NHibernate.Dialect.SQLiteDialect");
            //config.AddAuxiliaryDatabaseObject(new SimpleAuxiliaryDatabaseObject(
            //    "CREATE UNIQUE INDEX HashUniqueness_Index ON `DataChunk` (Hash)",
            //    "DROP INDEX HashUniqueness_Index", scopes1));
            
            //config.Properties.ToList().ForEach(x => logger.DebugFormat("Configuration Property: {0}", x));
            // delete the existing db on each run
            //if (File.Exists(DbFile))
            //    File.Delete(DbFile);

            bool printSql = false;
            new SchemaExport(config)
                    .Create(printSql, exportSchema);
        } 
        #endregion
    }
}