// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using CuttingEdge.Conditions;

    /// <summary>
    /// Currently this class only serves original file stored in its entirety 
    /// and not using deduplication services.
    /// </summary>
    public class LocalFileService {
        readonly string _baseDir;

        public LocalFileService(string baseDir) {
            _baseDir = baseDir;
        }

        public byte[] ReadFile(string name, long startOffset, long endOffset) {
            string fullPath = Path.Combine(_baseDir, name);
            var file = new FileInfo(fullPath);
            Condition.Requires(startOffset).IsInRange(0, file.Length);
            Condition.Requires(endOffset).IsInRange(0, file.Length);
            Condition.Requires(endOffset).IsGreaterOrEqual(startOffset);
            return ReadFileInternal(fullPath, startOffset, (int)(endOffset - startOffset + 1));
        }

        byte[] ReadFileInternal(string path, long startOffset, int count) {
            using (var stream = File.OpenRead(path)) {
                var buffer = new byte[count];
                stream.Seek(startOffset, SeekOrigin.Begin);
                stream.Read(buffer, 0, count);
                return buffer;
            }
        }
    }
}
