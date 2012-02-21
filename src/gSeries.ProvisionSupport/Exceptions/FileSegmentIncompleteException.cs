// -----------------------------------------------------------------------
// <copyright file="FileSegmentIncompleteException.cs" company="Jiangyan Xu">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Thrown when not all chunks in the file segment are available in ChunkDB.
    /// </summary>
    public class FileSegmentIncompleteException : ChunkDbException {
        public int? FirstMissingChunk { get; set; }
        public FileSegmentIncompleteException() : base() { }
        public FileSegmentIncompleteException(string msg) : base(msg) { }
        public FileSegmentIncompleteException(string msg, Exception innerException) : 
            base(msg, innerException) { }
        public FileSegmentIncompleteException(Exception innerException) : 
            base(innerException) { }
    }
}
