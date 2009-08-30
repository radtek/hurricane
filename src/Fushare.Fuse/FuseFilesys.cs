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

namespace Fushare.Filesystem {
  /// <summary>
  /// File system based on FUSE.
  /// </summary>
  public class FuseFilesys : FileSystem, IFushareFilesys {

    #region Fields
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FuseFilesys));
    private static readonly IDictionary _fslog_props = Logger.PrepareNamedLoggerProperties("Filesys");
    private readonly string _shadowDirPath;
    readonly FushareRedirectFSHelper _rfs;
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

    #region Constructors
    public FuseFilesys(string mountPoint, string shadowDirPath, FushareRedirectFSHelper 
      redirectFSHelper, string[] fuseArgs) : base(mountPoint) {
      _shadowDirPath = shadowDirPath;
      _rfs = redirectFSHelper;
      ParseFuseArguments(fuseArgs);
    }
    #endregion

    #region Event-triggering Methods
    
    protected override Errno OnGetPathStatus(string path, out Stat buf) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props,
        string.Format("OnGetPathStatus, path={0}", path));

      if (GettingPathStatus != null) {
        try {
          GettingPathStatus(this, new GetPathStatusEventArgs(new VirtualRawPath(path)));
        } catch (Exception ex) {
          Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
          "Exception thrown when handling GettingPathStatus event. Exception: {0}", ex));
          throw;
        }
      }

      // Get path status
      Errno ret = this._rfs.GetPathStatus(path, out buf);

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props,
          string.Format("::FilePermission of the path={0}. Errno={1}", buf.st_mode, ret));
      return ret;
    }

    protected override unsafe Errno OnReadHandle(string path, OpenedPathInfo info, byte[] buf,
      long offset, out int bytesRead) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format(
        "OnReadHandle, path={0}, handle={1}, buflength={2}, offset={3}", 
        path, info.Handle, buf.Length, offset));

      var eventArgs = new ReadFileEventArgs(new VirtualRawPath(path), buf, offset);
      if (this.ReadingFile != null) {
        try {
          ReadingFile(this, eventArgs);
        } catch (Exception ex) {
          Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
            "Exception thrown when handling ReadingFile event. Exception: {0}", ex));
          throw;
        }
      }

      bytesRead = eventArgs.BytesRead;
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, 
        string.Format("::Read {0} bytes.", bytesRead));
      // @TODO FIXME: Now always return 0 but should handle error cases.
      return 0;
    }

    protected override Errno OnReleaseHandle(string path, OpenedPathInfo info) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format(
        "OnReleaseHandle, path={0}, handle={1}, openflags={2}", 
        path, info.Handle, info.OpenFlags));

      if (ReleasedFile != null) {
        try {
          ReleasedFile(this, new ReleaseFileEventArgs(new VirtualRawPath(path)));
        } catch (Exception ex) {
          Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
            "Exception thrown when handling ReleasedFile event. Exception: {0}", ex));
          throw;
        }
      }

      return _rfs.ReleaseHandle(path, info);
    }
 
    #endregion

    #region Non-event-triggering Methods
    protected override Errno OnRenamePath(string from, string to) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("OnRenamePath, from={0}, to={1}", from, to));
      return _rfs.RenamePath(from, to);
    }

    protected override Errno OnCreateDirectory(string path, FilePermissions mode) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props,
          string.Format("OnCreateDirectory, path={0}, mode={1}", path, mode));
      return _rfs.CreateDirectory(path, mode);
    }

    protected override Errno OnReadDirectory(string path, OpenedPathInfo fi,
        out IEnumerable<DirectoryEntry> subPaths) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props,
          string.Format("OnReadDirectory, path={0}, handle={1}", path, fi.Handle));
      return this._rfs.ReadDirectory(path, fi, out subPaths);
    }

    protected override Errno OnReadSymbolicLink(string path, out string target) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props,
          string.Format("OnReadSymbolicLink, path={0}", path));
      return _rfs.ReadSymbolicLink(path, out target);
    }

    protected override Errno OnRemoveFile(string path) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnRemoveFile, path={0}", path));
      return this._rfs.RemoveFile(path);
    }

    protected override Errno OnRemoveDirectory(string path) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnRemoveDirectory, path={0}", path));
      return this._rfs.RemoveDirectory(path);
    }

    protected override unsafe Errno OnWriteHandle(string path, OpenedPathInfo info,
        byte[] buf, long offset, out int bytesWritten) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnWriteHandle, path={0}, handle={1}, buflength={2}, offset={3}", path, info.Handle, buf.Length, offset));

      return this._rfs.WriteHandle(path, info, buf, offset, out bytesWritten);
    }

    protected override Errno OnChangePathPermissions(string path, FilePermissions mode) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnChangePathPermissions, path={0}, filepermission={1}", path, mode));

      return this._rfs.ChangePathPermissions(path, mode);
    }

    protected override Errno OnOpenHandle(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnOpenHandle, path={0}, openflags={1}", path, info.OpenFlags));

      return this._rfs.OpenHandle(path, info);
    }

    protected override Errno OnFlushHandle(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnFlushHandle, path={0}, handle={1}", path, info.Handle));

      return this._rfs.FlushHandle(path, info);
    }

    protected override Errno OnCreateHandle(string path, OpenedPathInfo info, FilePermissions mode) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnCreateHandle, path={0}, openflags={1}, filepermission={2}", path, info.OpenAccess, mode));

      return this._rfs.CreateHandle(path, info, mode);
    }

    protected override Errno OnGetHandleStatus(string path, OpenedPathInfo info, out Stat buf) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnGetHandleStatus, path={0}, handle={1}", path, info.Handle));

      return this._rfs.GetHandleStatus(path, info, out buf);
    }

    protected override Errno OnOpenDirectory(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnOpenDirectory, path={0}", path));

      return this._rfs.OpenDirectory(path, info);
    }

    protected override Errno OnAccessPath(string path, AccessModes mask) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnAccessPath, path={0}, mask={1}", path, mask));

      return this._rfs.AccessPath(path, mask);
    }

    protected override Errno OnReleaseDirectory(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnReleaseDirectory, path={0}, handle={1}", path, info.Handle));

      return this._rfs.ReleaseDirectory(path, info);
    }

    protected override Errno OnCreateSpecialFile(string path, FilePermissions mode, ulong rdev) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnCreateSpecialFile, path={0}, mode={1}, rdev={2}", path, mode, rdev));

      return this._rfs.CreateSpecialFile(path, mode, rdev);
    }

    protected override Errno OnCreateSymbolicLink(string from, string to) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnCreateSymbolicLink, from={0}, to={1}", from, to));

      return this._rfs.CreateSymbolicLink(from, to);
    }

    protected override Errno OnGetFileSystemStatus(string path, out Statvfs stbuf) {
      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnGetFileSystemStatus, path={0}", path));
      Errno rs = this._rfs.GetFileSystemStatus(path, out stbuf);
      return rs;
    }

    protected override Errno OnSynchronizeHandle(string path, OpenedPathInfo info, bool onlyUserData) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnSynchronizeHandle, path={0}, handle={1}, onlyUserData={2}", path, info.Handle, onlyUserData));

      return this._rfs.SynchronizeHandle(path, info, onlyUserData);
    }

    protected override Errno OnSetPathExtendedAttribute(string path, string name, byte[] value, XattrFlags flags) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnSetPathExtendedAttribute, path={0}, name={1}, value={2}, flags={3}", path, name, Encoding.UTF8.GetString(value), flags));

      return this._rfs.SetPathExtendedAttribute(path, name, value, flags);
    }

    protected override Errno OnGetPathExtendedAttribute(string path, string name, byte[] value, out int bytesWritten) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format(
        "OnGetPathExtendedAttribute, path={0}, name={1}", path, name));

      return this._rfs.GetPathExtendedAttribute(path, name, value, out bytesWritten);
    }

    protected override Errno OnListPathExtendedAttributes(string path, out string[] names) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnListPathExtendedAttributes, path={0}", path));

      return this._rfs.ListPathExtendedAttributes(path, out names);
    }

    protected override Errno OnRemovePathExtendedAttribute(string path, string name) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnRemovePathExtendedAttribute, path={0}, name={1}", path, name));

      return this._rfs.RemovePathExtendedAttribute(path, name);
    }

    protected override Errno OnCreateHardLink(string from, string to) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnCreateHardLink, from={0}, to={1}", from, to));

      return this._rfs.CreateHardLink(from, to);
    }

    protected override Errno OnChangePathOwner(string path, long uid, long gid) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnChangePathOwner, path={0}, uid={1}, gid={2}", path, uid, gid));

      return this._rfs.ChangePathOwner(path, uid, gid);
    }

    protected override Errno OnTruncateFile(string path, long size) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnTruncateFile, path={0}, size={1}", path, size));

      return this._rfs.TruncateFile(path, size);
    }

    protected override Errno OnTruncateHandle(string path, OpenedPathInfo info, long size) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnTruncateHandle, path={0}, handle={1}, size={2}", path, info.Handle, size));

      return this._rfs.TruncateHandle(path, info, size);
    }

    protected override Errno OnChangePathTimes(string path, ref Utimbuf buf) {

      Logger.WriteLineIf(LogLevel.Verbose, _fslog_props, string.Format("OnChangePathTimes, path={0}, buf={1}", path, buf));

      return this._rfs.ChangePathTimes(path, ref buf);
    } 
    #endregion

    #region IFushareFilesys Members

    /// <summary>
    /// Occurs when user released file. Handlers take action on it and possibility send
    /// it to network.
    /// </summary>
    public event EventHandler<ReleaseFileEventArgs> ReleasedFile;

    /// <summary>
    /// Occurs when user is reading a file. Handlers store the read data in event args.
    /// </summary>
    public event EventHandler<ReadFileEventArgs> ReadingFile;

    /// <summary>
    /// Occurs when user is getting path status. Event handlers ensures virtual files 
    /// be in place.
    /// </summary>
    public event EventHandler<GetPathStatusEventArgs> GettingPathStatus;

    /// <summary>
    /// Starts this file system daemon.
    /// </summary>
    void IFushareFilesys.Start() {
      Initialize();
      using (this) {
        base.Start();
      }
    }

    #endregion

    void Initialize() {

    }
  }

}
