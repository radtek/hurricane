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
        public ChunkDbService(string dbFile, bool exportSchema) {
            _dbFile = dbFile;
            _sessionFactory = CreateSessionFactory(exportSchema);
        } 
        #endregion

        /// <summary>
        /// Adds the specified entry.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <exception cref="DuplicateNameException">Thrown when there is 
        /// already an entry with the same hash exists.</exception>
        public void AddChunk(byte[] hash, string filePath, int fileIndex) {
            var txnProvider = new NHTransactionProvider(
                new NHSessionProvider(_sessionFactory));
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    var helper = new ChunkDbHelper(
                        txnProvider.SessionProvider.CurrentSession, transaction);
                    ManagedFile file = helper.GetManagedFile(filePath);
                    var entry = new DataChunk {
                        File = file,
                        FileIndex = fileIndex,
                        Hash = hash,
                        Count = 0
                    };
                    helper.AddChunk(entry);
                    transaction.Commit();
                }
            }   // Dispose session.
        }

        /// <summary>
        /// Adds a file to the Chunk DB.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public void AddFile(string filePath) {
            var txnProvider = new NHTransactionProvider(
                new NHSessionProvider(_sessionFactory));

            var session = txnProvider.SessionProvider.CurrentSession;
            ManagedFile file;
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    var helper = new ChunkDbHelper(
                        session, transaction);
                    file = helper.GetOrCreateManagedFile(filePath);
                    // Have the file committed to DB.
                    transaction.Commit();
                }
            }


            // Choose stateless session for bulk insert.
            var statelessSession = _sessionFactory.OpenStatelessSession();
            using (var transaction = statelessSession.BeginTransaction()) {
                SHA1 sha = new SHA1CryptoServiceProvider();
                using (var stream = File.OpenRead(filePath)) {
                    var chunk = new byte[DataChunk.ChunkSize];
                    int duplicates = 0;
                    while (true) {
                        long offset = stream.Position;
                        int readLength = stream.Read(chunk, 0, chunk.Length);
                        if (readLength == 0) {
                            break;
                        }
                        byte[] hash = sha.ComputeHash(chunk);
                        bool alreadyExists = ChunkDbHelper.AddChunkIfNotExists(statelessSession, 
                            new DataChunk {
                            Hash = hash,
                            File = file,
                            FileIndex = (int)(offset / DataChunk.ChunkSize),
                            Count = 0,
                        });
                        if (alreadyExists) duplicates++;
                    }
                    transaction.Commit();
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
                        txnProvider.SessionProvider.CurrentSession, transaction);
                    var ret = helper.GetManagedFile(filePath);
                    transaction.Commit();
                    return ret;
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
                        txnProvider.SessionProvider.CurrentSession, transaction);
                    ICriterion hashEq = Expression.Eq("Hash", hash);
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
        /// <param name="fileIndex">The file index.</param>
        /// <returns></returns>
        public DataChunk GetChunkEntry(string filePath, int fileIndex) {
            var txnProvider = new NHTransactionProvider(
                    new NHSessionProvider(_sessionFactory));
            using (txnProvider) {
                using (var transaction = txnProvider.BeginTransaction()) {
                    var helper = new ChunkDbHelper(
                        txnProvider.SessionProvider.CurrentSession, transaction);
                    var session = txnProvider.SessionProvider.CurrentSession;
                    ManagedFile file = helper.GetManagedFile(filePath);
                    ICriteria crit = session.CreateCriteria<DataChunk>();
                    crit.Add(Expression.Eq(Projections.Property<DataChunk>(x => x.File), file))
                        .Add(Expression.Eq(Projections.Property<DataChunk>(x => x.FileIndex), fileIndex));
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
                    .UsingFile(DbFile).AdoNetBatchSize(500))
                //.Database(MySQLConfiguration.Standard.ConnectionString(
                //    "Server=localhost;Database=newChunkDb;Uid=root;Pwd=password;")
                //    .AdoNetBatchSize(500))
                .Mappings(m =>
                    m.FluentMappings.Add<DataChunkMap>().Add<ManagedFileMap>())
                .ExposeConfiguration(c => 
                    HandleConfiguration(c, exportSchema))
                .BuildSessionFactory();
        }

        private static void HandleConfiguration(Configuration config, 
            bool exportSchema) {
            var scopes = new Iesi.Collections.Generic.HashedSet<string>();
            scopes.Add("NHibernate.Dialect.MySQLDialect");
            config.AddAuxiliaryDatabaseObject(new SimpleAuxiliaryDatabaseObject(
                "CREATE UNIQUE INDEX HashUniqueness_Index ON `DataChunk` (Hash(20))",
                "DROP INDEX HashUniqueness_Index", scopes));
            var scopes1 = new Iesi.Collections.Generic.HashedSet<string>();
            scopes1.Add("NHibernate.Dialect.SQLiteDialect");
            config.AddAuxiliaryDatabaseObject(new SimpleAuxiliaryDatabaseObject(
                "CREATE UNIQUE INDEX HashUniqueness_Index ON `DataChunk` (Hash)",
                "DROP INDEX HashUniqueness_Index", scopes1));
            
            //config.Properties.ToList().ForEach(x => logger.DebugFormat("Configuration Property: {0}", x));
            // delete the existing db on each run
            //if (File.Exists(DbFile))
            //    File.Delete(DbFile);

            if (exportSchema) {
                new SchemaExport(config)
                    .Create(true, true);
            }
        } 
        #endregion
    }
}