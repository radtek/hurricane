﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GSeries.Filesystem {
  public class PathFactory {
    public enum FilesysOp {
      Read,
      Write
    }

    readonly string _shadowDirPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathFactory"/> class.
    /// </summary>
    /// <param name="shadowDirPath">The shadow dir path.</param>
    public PathFactory(ShadowDirPath shadowDirPath) {
      _shadowDirPath = shadowDirPath.PathString;
    }

    #region Creator Methods
    public VirtualPath CreateVirtualPath(VirtualRawPath vrp, FilesysOp op) {
      if (op == FilesysOp.Read) {
        return new VirtualMetaPath(vrp);
      } else {
        return new VirtualPath(vrp);
      }
    }

    public ShadowFullPath CreateShadowFullPath(VirtualPath vp, FilesysOp op) {
      if (op == FilesysOp.Read) {
        return new ShadowMetaFullPath(_shadowDirPath, vp);
      } else {
        return new ShadowFullPath(_shadowDirPath, vp);
      }
    }

    public ShadowFullPath CreateShadowFullPath4Read(VirtualPath vp) {
      return CreateShadowFullPath(vp, FilesysOp.Read);
    }

    public ShadowFullPath CreateShadwoFullPath4Write(VirtualPath vp) {
      return CreateShadowFullPath(vp, FilesysOp.Write);
    }

    #endregion

    #region Convenience Methods
    public string CreateVirtualPath4Read(VirtualRawPath vrp) {
      return CreateVirtualPath(vrp, FilesysOp.Read).PathString;
    }

    public string CreateVirtualPath4Write(VirtualRawPath vrp) {
      return CreateVirtualPath(vrp, FilesysOp.Write).PathString;
    }
    #endregion
  }
}
