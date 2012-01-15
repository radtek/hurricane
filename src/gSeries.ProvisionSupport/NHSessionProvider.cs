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

    /// <summary>
    /// Provides a session object for a certain context.
    /// </summary>
    public class NHSessionProvider : IDisposable {
        private readonly ISessionFactory _sessionFactory;

        #region Non-Readonly Fields
        private ISession _currentSession;
        #endregion

        public NHSessionProvider(ISessionFactory sessionFactory) {
            _sessionFactory = sessionFactory;
        }

        public ISession CurrentSession {
            get {
                if (null == _currentSession)
                    _currentSession = _sessionFactory.OpenSession();
                return _currentSession;
            }
        }

        public IStatelessSession OpenStatelessSession() {
            return _sessionFactory.OpenStatelessSession();
        }

        public void ReplaceCurrentSession() {
            _currentSession.Dispose();
            _currentSession = null;
        }

        public void Dispose() {
            if (_currentSession != null)
                _currentSession.Dispose();
            _currentSession = null;
        }
    }
}
