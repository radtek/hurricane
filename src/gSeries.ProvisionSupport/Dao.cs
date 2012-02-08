// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Dao<T> {
        ISession _session;

        public Dao(ISession session) {
            _session = session;
        }

        /// <summary>
        /// Make this transient or persistent entity persistent in the database.
        /// </summary>
        public void MakePersistent(T entity) {
            try {
                _session.SaveOrUpdate(entity);
            } catch (HibernateException ex) {
                throw new ChunkDbException("Failed to make entity persistent.", 
                    ex);
            }
        }

        /// <summary>
        /// Make the entity transient
        /// </summary>
        /// <remarks>
        /// Making something transient means that it doesn't have a corresponding record in the database; 
        /// It is no longer persistent.
        /// </remarks>
        /// <param name="entity">The entity who's database state will be deleted.</param>
        public virtual void MakeTransient(T entity) {
            try {
                _session.Delete(entity);
            } catch (HibernateException ex) {
                throw new ChunkDbException(ex);
            }
        }

        /// <summary>
        /// Get an entity by ID.
        /// </summary>
        /// <param name="id">The id of the entity to load.</param>
        /// <param name="lockIt">Specify true if you want an upgrade lock. 
        /// This will db lock the item for update until the current transaction ends. 
        /// It will also do a version check (comparing columns or version no).</param>
        public virtual T GetById(int id, bool lockIt) {
            try {
                if (lockIt)
                    return _session.Get<T>(id, LockMode.Upgrade);
                else
                    return _session.Get<T>(id);
            } catch (HibernateException ex) {
                throw new ChunkDbException(ex);
            }
        }

        /// <summary>
        /// Get an entity by ID, with no upgrade lock.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual T GetById(int id) {
            return GetById(id, false);
        }


        /// <summary>
        /// Find entities matching this the example one given
        /// </summary>
        /// <param name="exampleEntity">Entity with example properties we'd like to match</param>
        public virtual IList<T> FindByExample(T exampleEntity) {
            IList<T> entities;
            try {
                ICriteria crit = _session.CreateCriteria(typeof(T));
                entities = crit.Add(Example.Create(exampleEntity).ExcludeZeroes()).List<T>();
            } catch (HibernateException ex) {
                throw new ChunkDbException(ex);
            }
            return entities;
        }

        public T UniqueResultByExample(T exampleEntity) {
            try {
                ICriteria crit = _session.CreateCriteria(typeof(T));
                return crit.Add(Example.Create(exampleEntity).ExcludeZeroes()).UniqueResult<T>();
            } catch (HibernateException ex) {
                throw new ChunkDbException(ex);
            }
        }

    }
}
