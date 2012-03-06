// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DataDistributionService {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.ServiceModel;
    using System.ServiceModel.Description;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public sealed class WcfProxy<T> : ClientBase<T>, IDisposable where T : class {
        public T Service { get { return base.Channel; } }

        public WcfProxy(ServiceEndpoint endpoint) : base(endpoint) { }

        public void Dispose() {
            try {
                switch (State) {
                    case CommunicationState.Closed:
                        break; // nothing to do
                    case CommunicationState.Faulted:
                        Abort();
                        break;
                    case CommunicationState.Opened:
                        Close();
                        break;
                }
            } catch { } // best efforts...
        }
    }
}
