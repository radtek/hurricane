using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using Mono.Fuse;
using Mono.Unix.Native;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.IO;
using Fushare.Filesystem;

namespace Fushare.Fuse {
  /// <summary>
  /// File system based on FUSE.
  /// </summary>
  public class FuseFilesys : FileSystem, IFushareFilesys {

    #region Fields
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FuseFilesys));
    private static readonly IDictionary _fslog_props = Logger.PrepareNamedLoggerProperties("Filesys");
    private readonly string _shadowDirPath;
    private RedirectFHFSHelper _rfs;
    #endregion

    #region Properties
    public static IDictionary FilesysLogProps {
      get {
        return _fslog_props;
      }
    }

    public string ShadowDirPath {
      get { return _shadowDirPath; }
    }
    #endregion

    #region Event-triggering Methods
    
    protected override Errno OnGetPathStatus(string path, out Stat buf) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props,
        string.Format("OnGetPathStatus, path={0}", path));

      if (GettingPathStatus != null) {
        GettingPathStatus(this, new GetPathStatusEventArgs(new VirtualRawPath(path)));
      }

      Errno ret = this._rfs.OnGetPathStatus(path, out buf);

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props,
          string.Format("FilePermission of the path={0}", buf.st_mode));
      return ret;
    }

    protected override unsafe Errno OnReadHandle(string path, OpenedPathInfo info, byte[] buf,
      long offset, out int bytesRead) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format(
        "OnReadHandle, path={0}, handle={1}, buflength={2}, offset={3}", 
        path, info.Handle, buf.Length, offset));

      if (this.ReadingFile != null) {
        ReadingFile(this, new ReadFileEventArgs(new VirtualRawPath(path), buf, offset));
      }

      return this._rfs.OnReadHandle(path, info, buf, offset, out bytesRead);
    }

    protected override Errno OnReleaseHandle(string path, OpenedPathInfo info) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format(
        "OnReleaseHandle, path={0}, handle={1}, openflags={2}", 
        path, info.Handle, info.OpenFlags));

      if (ReleasedFile != null) {
        ReleasedFile(this, new ReleaseFileEventArgs(new VirtualRawPath(path)));
      }

      return _rfs.OnReleaseHandle(path, info);
    }
 
    #endregion

    #region Non-event-triggering Methods
    protected override Errno OnRenamePath(string from, string to) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("OnRenamePath, from={0}, to={1}", from, to));
      return _rfs.OnRenamePath(from, to);
    }

    protected override Errno OnCreateDirectory(string path, FilePermissions mode) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props,
          string.Format("OnCreateDirectory, path={0}, mode={1}", path, mode));
      return _rfs.OnCreateDirectory(path, mode);
    }

    protected override Errno OnReadDirectory(string path, OpenedPathInfo fi,
        out IEnumerable<DirectoryEntry> subPaths) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props,
          string.Format("OnReadDirectory, path={0}, handle={1}", path, fi.Handle));
      return this._rfs.OnReadDirectory(path, fi, out subPaths);
    }

    protected override Errno OnReadSymbolicLink(string path, out string target) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props,
          string.Format("OnReadSymbolicLink, path={0}", path));
      return _rfs.OnReadSymbolicLink(path, out target);
    }

    protected override Errno OnRemoveFile(string path) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnRemoveFile, path={0}", path));
      return this._rfs.OnRemoveFile(path);
    }

    protected override Errno OnRemoveDirectory(string path) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnRemoveDirectory, path={0}", path));
      return this._rfs.OnRemoveDirectory(path);
    }

    protected override unsafe Errno OnWriteHandle(string path, OpenedPathInfo info,
        byte[] buf, long offset, out int bytesWritten) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnWriteHandle, path={0}, handle={1}, buflength={2}, offset={3}", path, info.Handle, buf.Length, offset));

      return this._rfs.OnWriteHandle(path, info, buf, offset, out bytesWritten);
    }

    protected override Errno OnChangePathPermissions(string path, FilePermissions mode) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnChangePathPermissions, path={0}, filepermission={1}", path, mode));

      return this._rfs.OnChangePathPermissions(path, mode);
    }

    protected override Errno OnOpenHandle(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnOpenHandle, path={0}, openflags={1}", path, info.OpenFlags));

      return this._rfs.OnOpenHandle(path, info);
    }

    protected override Errno OnFlushHandle(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnFlushHandle, path={0}, handle={1}", path, info.Handle));

      return this._rfs.OnFlushHandle(path, info);
    }

    protected override Errno OnCreateHandle(string path, OpenedPathInfo info, FilePermissions mode) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnCreateHandle, path={0}, openflags={1}, filepermission={2}", path, info.OpenAccess, mode));

      return this._rfs.OnCreateHandle(path, info, mode);
    }

    protected override Errno OnGetHandleStatus(string path, OpenedPathInfo info, out Stat buf) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnGetHandleStatus, path={0}, handle={1}", path, info.Handle));

      return this._rfs.OnGetHandleStatus(path, info, out buf);
    }

    protected override Errno OnOpenDirectory(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnOpenDirectory, path={0}", path));

      return this._rfs.OnOpenDirectory(path, info);
    }

    protected override Errno OnAccessPath(string path, AccessModes mask) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnAccessPath, path={0}, mask={1}", path, mask));

      return this._rfs.OnAccessPath(path, mask);
    }

    protected override Errno OnReleaseDirectory(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnReleaseDirectory, path={0}, handle={1}", path, info.Handle));

      return this._rfs.OnReleaseDirectory(path, info);
    }

    protected override Errno OnCreateSpecialFile(string path, FilePermissions mode, ulong rdev) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnCreateSpecialFile, path={0}, mode={1}, rdev={2}", path, mode, rdev));

      return this._rfs.OnCreateSpecialFile(path, mode, rdev);
    }

    protected override Errno OnCreateSymbolicLink(string from, string to) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnCreateSymbolicLink, from={0}, to={1}", from, to));

      return this._rfs.OnCreateSymbolicLink(from, to);
    }

    protected override Errno OnGetFileSystemStatus(string path, out Statvfs stbuf) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnGetFileSystemStatus, path={0}", path));
      Errno rs = this._rfs.OnGetFileSystemStatus(path, out stbuf);
      return rs;
    }

    protected override Errno OnSynchronizeHandle(string path, OpenedPathInfo info, bool onlyUserData) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnSynchronizeHandle, path={0}, handle={1}, onlyUserData={2}", path, info.Handle, onlyUserData));

      return this._rfs.OnSynchronizeHandle(path, info, onlyUserData);
    }

    protected override Errno OnSetPathExtendedAttribute(string path, string name, byte[] value, XattrFlags flags) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnSetPathExtendedAttribute, path={0}, name={1}, value={2}, flags={3}", path, name, Encoding.UTF8.GetString(value), flags));

      return this._rfs.OnSetPathExtendedAttribute(path, name, value, flags);
    }

    protected override Errno OnGetPathExtendedAttribute(string path, string name, byte[] value, out int bytesWritten) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnGetPathExtendedAttribute, path={0}, name={1}, value={2}", path, name, Encoding.UTF8.GetString(value)));

      return this._rfs.OnGetPathExtendedAttribute(path, name, value, out bytesWritten);
    }

    protected override Errno OnListPathExtendedAttributes(string path, out string[] names) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnListPathExtendedAttributes, path={0}", path));

      return this._rfs.OnListPathExtendedAttributes(path, out names);
    }

    protected override Errno OnRemovePathExtendedAttribute(string path, string name) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnRemovePathExtendedAttribute, path={0}, name={1}", path, name));

      return this._rfs.OnRemovePathExtendedAttribute(path, name);
    }

    protected override Errno OnCreateHardLink(string from, string to) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnCreateHardLink, from={0}, to={1}", from, to));

      return this._rfs.OnCreateHardLink(from, to);
    }

    protected override Errno OnChangePathOwner(string path, long uid, long gid) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnChangePathOwner, path={0}, uid={1}, gid={2}", path, uid, gid));

      return this._rfs.OnChangePathOwner(path, uid, gid);
    }

    protected override Errno OnTruncateFile(string path, long size) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnTruncateFile, path={0}, size={1}", path, size));

      return this._rfs.OnTruncateFile(path, size);
    }

    protected override Errno OnTruncateHandle(string path, OpenedPathInfo info, long size) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnTruncateHandle, path={0}, handle={1}, size={2}", path, info.Handle, size));

      return this._rfs.OnTruncateHandle(path, info, size);
    }

    protected override Errno OnChangePathTimes(string path, ref Utimbuf buf) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnChangePathTimes, path={0}, buf={1}", path, buf));

      return this._rfs.OnChangePathTimes(path, ref buf);
    } 
    #endregion

    #region IFushareFilesys Members

    public event EventHandler<ReleaseFileEventArgs> ReleasedFile;

    public event EventHandler<ReadFileEventArgs> ReadingFile;

    public event EventHandler<GetPathStatusEventArgs> GettingPathStatus;

    #endregion
  }

}
