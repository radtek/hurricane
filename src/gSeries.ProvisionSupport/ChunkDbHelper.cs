// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using NHibernate;
    using NHibernate.Criterion;
    using System.Data;
    using System.Diagnostics;
    using log4net;
    using System.IO;

    /// <summary>
    /// A helper class that executes Db operations without session management.
    /// </summary>
    /// <remarks>
    /// The helper is instantiated per session.
    /// </remarks>
    public class ChunkDbHelper {
        readonly ISession _session;
        readonly ITransaction _transaction;
        static readonly ILog logger = LogManager.GetLogger(typeof(ChunkDbHelper));

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkDbHelper"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="transaction">The transaction.</param>
        internal ChunkDbHelper(ISession session, ITransaction transaction) {
            _session = session;
            _transaction = transaction;
        }

        /// <summary>
        /// Adds the specified entry.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <exception cref="DuplicateNameException">Thrown when there is 
        /// already an entry with the same hash exists.</exception>
        public void AddChunk(DataChunk chunk) {
            try {
                _session.Save(chunk);
            } catch (NHibernate.Exceptions.GenericADOException ex) {
                throw new DuplicateNameException(
                    "Chunk hash needs to be unique.", ex);
            }
        }

        public void AddChunkIfNotExists(DataChunk chunk) {
            ICriteria crit = _session.CreateCriteria(typeof(DataChunk));
            ICriterion hashEq = Expression.Eq("Hash", chunk.Hash);
            crit.Add(hashEq);
            if ((DataChunk)crit.UniqueResult() == null) {
                // Save if no duplicate is found.
                _session.Save(chunk);
            }
        }

        /// <summary>
        /// Adds the chunk if not exists.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="chunk">The chunk.</param>
        /// <returns>True if the chunk already exists and this chunk is not 
        /// inserted.</returns>
        public static bool AddChunkIfNotExists(IStatelessSession session, 
            DataChunk chunk) {
            ICriteria crit = session.CreateCriteria(typeof(DataChunk));
            ICriterion hashEq = Expression.Eq("Hash", chunk.Hash);
            crit.Add(hashEq);
            if ((DataChunk)crit.UniqueResult() == null) {
                // Save if no duplicate is found.
                session.Insert(chunk);
                return false;
            } else {
                return true;
            }
        }

        /// <summary>
        /// Gets the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Null if the file isn't managed in the DB.</returns>
        public ManagedFile GetManagedFile(string path) {
            return _session.CreateCriteria<ManagedFile>()
                .Add(Expression.Eq(Projections.Property<ManagedFile>(
                x => x.Path), path))
                .UniqueResult<ManagedFile>();
        }

        /// <summary>
        /// Gets or creates the managed file for the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public ManagedFile GetOrCreateManagedFile(string path) {
            ManagedFile file = GetManagedFile(path);
            if (file == null) {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                byte[] fileHash = FileHelper.GetFileHash(path);
                sw.Stop();
                logger.DebugFormat("Computing file hash took {0} milliseconds.", 
                    sw.ElapsedMilliseconds);
                file = new ManagedFile {
                    FileHash = fileHash,
                    Size = new FileInfo(path).Length,
                    Path = path
                };
                _session.Save(file);
                return file;
            } else {
                return file;
            }
        }
    }
}
