// -----------------------------------------------------------------------
// <copyright file="ChunkNotInDbException.cs" company="Jiangyan Xu">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Thrown when a chunk cannot be found in ChunkDB.
    /// </summary>
    public class ChunkNotInDbException : ChunkDbException {
        public string File { set; get; }
        public int ChunkIndex { get; set; }
        public ChunkNotInDbException() : base() { }
        public ChunkNotInDbException(string msg) : base(msg) { }
        public ChunkNotInDbException(string msg, Exception innerException) : 
            base(msg, innerException) { }
        public ChunkNotInDbException(Exception innerException) : base(innerException) { }
    }
}
