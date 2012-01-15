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
using System.Data;

    /// <summary>
    /// Provides access to NHibernate transactions.
    /// </summary>
    public class NHTransactionProvider : IDisposable {
        private readonly NHSessionProvider _sessionProvider;
        public NHSessionProvider SessionProvider { 
            get { return _sessionProvider; } 
        }

        public NHTransactionProvider(NHSessionProvider sessionProvider) {
            _sessionProvider = sessionProvider;
        }

        public ITransaction BeginTransaction() {
            var session = _sessionProvider.CurrentSession;
            return session.BeginTransaction();
        }

        public ITransaction BeginTransaction(IsolationLevel isolationLevel) {
            var session = _sessionProvider.CurrentSession;
            return session.BeginTransaction(isolationLevel);
        }

        public void Dispose() {
            _sessionProvider.Dispose();
        }
    }
}
