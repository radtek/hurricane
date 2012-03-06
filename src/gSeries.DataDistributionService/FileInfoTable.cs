// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DataDistributionService {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Collections.Concurrent;
    using MonoTorrent.Client;
    using GSeries.ProvisionSupport;

    /// <summary>
    /// A context object that holds some runtime data for files being downloaded
    /// and served. It's a strong-typed wrapper for a 
    /// <see cref="ConcurrentDictionary"/> to make it easier for dependency 
    /// injection.
    /// </summary>
    public class FileInfoTable<T> {
        ConcurrentDictionary<string, T> _table = 
            new ConcurrentDictionary<string, T>();

        public T this[string path] {
            get { return _table[path]; }
            set { _table[path] = value; }
        }
    }
}
