using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Text;
using Mono.Fuse;
using Mono.Unix.Native;
using System.Threading;
using Brunet;
using System.Runtime.Remoting.Messaging;
using System.IO;
using NUnit.Framework;


namespace FuseDht {
  /// <summary>
  /// Interrupts system calls and weaves in FuseDht logic.
  /// </summary>
  public class FuseDht : FileSystem {
    
    private FuseDhtHelper _helper;

    private FuseDhtUtil _util;

    private string _shadowdir;

    private RedirectFHFSHelper _rfs;
    
    public static void Main(string[] args) {

      using (FuseDht fs = new FuseDht()) {
        string[] unhandled = fs.ParseFuseArguments(args);
        foreach (string key in fs.FuseOptions.Keys) {
          Console.WriteLine("Option: {0}={1}", key, fs.FuseOptions[key]);
        }
        if (!fs.ParseArguments(unhandled))
          return;
        fs.InitFuseDhtSystem();
        fs.Start();
      }
    }

    private bool ParseArguments(string[] args) {
      if (args.Length != 2 && args.Length != 1) {
        Console.Error.WriteLine("Invalid number of arguments.");
        Console.Error.WriteLine("Use -h|--help for help");
        return false;
      }
      for (int i = 0; i < args.Length; ++i) {
        switch (args[i]) {
          case "-h":
          case "--help":
            FileSystem.ShowFuseHelp("FuseDht");
            Console.Error.WriteLine("FuseDht Arguments:");
            string help = "1.\t" + "Mounting Point"
                        + "2.\t" + "Shadow Path";
            Console.WriteLine("FuseDht Options");
            Console.Error.WriteLine(help);
            return false;
          default:
            if (string.IsNullOrEmpty(base.MountPoint)) {
              base.MountPoint = args[i];
              Console.WriteLine("MountPoint: {0}", args[i]);
            } else if (string.IsNullOrEmpty(this._shadowdir)) {
              _shadowdir = args[i];
              Console.WriteLine("Shadow: {0}", args[i]);
            }
            break;
        }
      }
      return true;
    }

    void InitFuseDhtSystem() {
      this._rfs = new RedirectFHFSHelper(this._shadowdir);
      this._util = new FuseDhtUtil(this._shadowdir);
      this._helper = FuseDhtHelperFactory.GetFuseDhtHelper(FuseDhtHelperFactory.HelperType.Local, this._shadowdir);
      this._util.InitDhtRootFileStructure();
      this._util.CreateSelfBaseDir(this._helper.DhtAddress);
    }

    protected override Errno OnRenamePath(string from, string to) {

      Debug.WriteLine(string.Format("OnRenamePath, from={0}, to={1}: {2}", from, to, System.DateTime.Now));

      //string[] frompaths = FuseDhtUtil.ParsePath(from);
      //if (frompaths.Length == 1) {
      //  //key1
      //  string[] topaths = FuseDhtUtil.ParsePath(to);
      //  if (topaths.Length == 1) {
      //    //key2
      //    DirectoryInfo cache = new DirectoryInfo(this._shadowdir + from + "/" + Constants.DIR_CACHE);
      //    if (cache.Exists && cache.GetFiles().Length == 0) {
      //      //rename allowed
      //      return this._rfs.OnRenamePath(from, to);
      //    }
      //  }
      //}
      ////otherwise, permission denied.
      //return Errno.EACCES;
      return this._rfs.OnRenamePath(from, to);
    }

    protected override Errno OnCreateDirectory(string path, FilePermissions mode) {

      Debug.WriteLine(string.Format("OnCreateDirectory, path={0}, mode={1}: {2}", path, mode, System.DateTime.Now));

      //dht/basedir or dht/basedir/keydir
      string[] paths = FuseDhtUtil.ParsePath(path);
      if (paths.Length > Constants.LVL_KEY_DIR + 1) {
        return Errno.EACCES;
      }
      this._rfs.OnCreateDirectory(path, mode);
      
      //sucessfully created. Initialize the directory structure
      if(paths.Length == Constants.LVL_KEY_DIR + 1) {
        string s_path = Path.Combine(_shadowdir, path);
        DirectoryInfo keydir = new DirectoryInfo(s_path);
        DirectoryInfo basedir = keydir.Parent;
        string basedirName = basedir.Name;
        try {
          _util.InitKeyDirStructure(basedirName, keydir.Name);
        } catch (Exception e) {
          Debug.WriteLine(e);
          Console.Error.WriteLine("Init key dir failed. Removing dir...");
          Directory.Delete(s_path, true);
          return Errno.EACCES;
        }
      }
      return 0;
    }

    protected override Errno OnReadDirectory(string path, OpenedPathInfo fi,
        out IEnumerable<DirectoryEntry> paths) {
      
      Debug.WriteLine(string.Format("OnReadDirectory, path={0}, handle={1}: {2}", path, fi.Handle, System.DateTime.Now));                 

      ////only block read of "cache"
      //string[] parsedpath = FuseDhtUtil.ParsePath(path);
      //if (parsedpath.Length == 2) {
      //  string dir = parsedpath[parsedpath.Length - 1];
      //  if (dir.Equals(Constants.DIR_CACHE)) {
      //    //remote or local?
      //    DirectoryInfo cache = new DirectoryInfo(this._shadowdir + path);
      //    string finvalidate = cache.Parent.FullName + "/" + Constants.DIR_ETC + "/" + Constants.FILE_INVALIDATE;
      //    //					if(File.Exists(finvalidate))
      //    //					{
      //    //						int invalidate = Int32.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(finvalidate)));
      //    int invalidate = (int)FuseDhtUtil.ReadParam(_shadowdir, parsedpath[0], Constants.FILE_INVALIDATE);
      //    Console.WriteLine("Invalidate={0}", invalidate);
      //    int lifespan;
      //    if (invalidate == 0) {
      //      //							try
      //      //							{
      //      //								//check whether stale
      //      //								string flifespan = cache.Parent.FullName + "/" + Constants.DIR_ETC + "/" + Constants.FILE_LIFESPAN;
      //      //								lifespan = Int32.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(flifespan)));
      //      //							}
      //      //							catch
      //      //							{
      //      //								Console.WriteLine("Invalid lifespan or file not found, use value in conf file");
      //      //								lifespan = Int32.Parse(FuseDhtUtil.GetValueFromConf(this._shadowdir, Constants.FILE_LIFESPAN)); 
      //      //							}
      //      lifespan = (int)FuseDhtUtil.ReadParam(_shadowdir, parsedpath[0], Constants.FILE_LIFESPAN);
      //      TimeSpan ts = new TimeSpan(0, 0, lifespan);
      //      bool stale = false;

      //      if (cache.GetFiles().Length == 0) {
      //        stale = true;
      //      } else {
      //        foreach (FileInfo finfo in cache.GetFiles()) {
      //          if (finfo.CreationTime.Add(ts) < System.DateTime.Now) {
      //            stale = true;
      //            break;
      //          }
      //        }
      //      }
      //      Console.WriteLine("Stale={0}", stale);
      //      if (stale) {
      //        Console.WriteLine("Stale. Calling DhtGet");
      //        //get remote data
      //        this._helper.DhtGet(this._shadowdir, path);
      //      }
      //    } else if (invalidate == 1) {
      //      Console.WriteLine("Invalidated");
      //      FileInfo[] fdone = cache.GetFiles(Constants.FILE_DONE);
      //      bool shouldCallDht = false;
      //      if (fdone.Length != 0) {
      //        int done = Int32.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(fdone[0].FullName)));
      //        if (done == 1) {
      //          //the last dhtread already completed
      //          Console.WriteLine(".done found and equals 1");
      //          shouldCallDht = true;
      //        }
      //      } else {
      //        Console.WriteLine("No .done");
      //        shouldCallDht = true;
      //      }
      //      if (shouldCallDht) {
      //        Console.WriteLine("Calling DhtGet");
      //        this._helper.DhtGet(this._shadowdir, path);
      //        using (StreamWriter wr = File.CreateText(finvalidate)) {
      //          wr.WriteLine("0");
      //        }
      //      } else {
      //        Console.WriteLine("DhtGet not called because of on-going read on the same key");
      //      }
      //    }
      //    //					}
      //  }
      //}

      //IntPtr dp = (IntPtr)fi.Handle;

      //paths = ReadDirectory(dp);

      //return 0;

      return this._rfs.OnReadDirectory(path, fi, out paths);
    }

    private IEnumerable<DirectoryEntry> ReadDirectory(IntPtr dp) {
      Dirent de;
      while ((de = Syscall.readdir(dp)) != null) {
        DirectoryEntry e = new DirectoryEntry(de.d_name);
        e.Stat.st_ino = de.d_ino;
        e.Stat.st_mode = (FilePermissions)(de.d_type << 12);
        yield return e;
      }
    }

    protected override Errno OnReleaseHandle(string path, OpenedPathInfo info) {

      Debug.WriteLine(string.Format("OnReleaseHandle, path={0}, handle={1}, openflags={2}: {3}", path, info.Handle, info.OpenFlags, System.DateTime.Now));

      //int r = Syscall.close((int)info.Handle);
      //if (r == -1)
      //  return Stdlib.GetLastError();

      ////successful closed.
      //string[] paths = FuseDhtUtil.ParsePath(path);
      //switch (paths.Length) {
      //  case 4:
      //    //i.e /keydir/key/my/p2paddr1.txt
      //    //On write of this file
      //    string filename = paths[Constants.LVL_DATA_FILE];
      //    string folder = paths[Constants.LVL_FOLDER];
      //    string key = paths[Constants.LVL_KEY];
      //    string keydir = paths[Constants.LVL_KEY_DIR];

      //    if (folder.Equals(Constants.DIR_MY)) {
      //      Console.WriteLine("Written in MY");
      //      //Release of a write and only in "my" folder
      //      //O_RDONLY == 0
      //      if ((OpenFlags.O_WRONLY == (info.OpenFlags & OpenFlags.O_WRONLY)
      //          || OpenFlags.O_RDWR == (info.OpenFlags & OpenFlags.O_RDWR))
      //        && FuseDhtUtil.IsValidMyFileName(filename)
      //        && FuseDhtUtil.IsPutAllowed(_shadowdir, key)) {
      //        Console.WriteLine("Calling DhtPut");
      //        _helper.DhtPut(_shadowdir, path);
      //      }
      //    }
      //    break;
      //  case 1:
      //    //i.e. /KeyDirGenerator/binkey1.txt
      //    string dir = paths[Constants.LVL_KEY_DIR_GENTR];
      //    if (dir.Equals(Constants.DIR_KEY_DIR_GENERATOR)) {
      //      byte[] b = File.ReadAllBytes(path);
      //      FuseDhtUtil.InitKeyDirStructure(this._shadowdir, b);
      //    }
      //    break;
      //  default:
      //    break;
      //}
      //return 0;
      return this._rfs.OnReleaseHandle(path, info);
    }

    protected override Errno OnRemoveFile(string path) {

      Debug.WriteLine(string.Format("OnRemoveFile, path={0}: {1}", path, System.DateTime.Now));

    //  string[] paths = FuseDhtUtil.ParsePath(path);
    //  switch (paths.Length) {
    //    case 4:	///keydir/key1/my(cache)/sample.txt
    //      if (!File.Exists(this._shadowdir + "/" + paths[0] + "/" + Constants.FILE_OFFLINE)) {
    //        //only block deletes in online folders
    //        bool dhtCalled = this._helper.DhtDelete(this._shadowdir, path);
    //        if (!dhtCalled) {
    //          return Errno.EACCES;
    //        }
    //      } else {
    //        goto default;
    //      }
    //      break;
    //    case 3:	///keydir/key1/.offline or /keydir/key1/.conf
    //      if (path[1].Equals(Constants.FILE_OFFLINE)) {
    //        bool ifput;
    //        try {
    //          string filename;
    //          ifput = FuseDhtUtil.DeleteOfflineFile(this._shadowdir, paths[0], out filename);
    //          if (ifput) {
    //            this._helper.DhtPut(this._shadowdir, "/" + paths[0] + "/" + Constants.DIR_MY + "/" + filename);
    //          }
    //        } catch {
    //          return Errno.EACCES;
    //        }
    //      }
    //      break;
    //    default:
    //      int r = Syscall.unlink(_shadowdir + path);
    //      if (r == -1)
    //        return Stdlib.GetLastError();
    //      return 0;
    //  }
    //  return 0;
      return this._rfs.OnRemoveFile(path);
    }

    #region UnmodifiedMethods

    protected override unsafe Errno OnWriteHandle(string path, OpenedPathInfo info,
        byte[] buf, long offset, out int bytesWritten) {

      Debug.WriteLine(string.Format("OnWriteHandle, path={0}, handle={1}, buflength={2}, offset={3}: {4}", path, info.Handle, buf.Length, offset, System.DateTime.Now));

      return this._rfs.OnWriteHandle(path, info, buf, offset, out bytesWritten);
    }

    protected override Errno OnRemoveDirectory(string path) {

      Debug.WriteLine(string.Format("OnRemoveDirectory, path={0}: {1}", path, System.DateTime.Now));

      return this._rfs.OnRemoveDirectory(path);
    }

    protected override unsafe Errno OnReadHandle(string path, OpenedPathInfo info, byte[] buf,
        long offset, out int bytesRead) {

      Debug.WriteLine(string.Format("OnReadHandle, path={0}, handle={1}, buflength={2}, offset={3}: {4}", path, info.Handle, buf.Length, offset, System.DateTime.Now));

      return this._rfs.OnReadHandle(path, info, buf, offset, out bytesRead);
    }

    protected override Errno OnChangePathPermissions(string path, FilePermissions mode) {

      Debug.WriteLine(string.Format("OnChangePathPermissions, path={0}, filepermission={1}: {2}", path, mode, System.DateTime.Now));

      return this._rfs.OnChangePathPermissions(path, mode);
    }

    protected override Errno OnOpenHandle(string path, OpenedPathInfo info) {

      Debug.WriteLine(string.Format("OnOpenHandle, path={0}, openflags={1}: {2}", path, info.OpenFlags, System.DateTime.Now));

      return this._rfs.OnOpenHandle(path, info);
    }

    protected override Errno OnFlushHandle(string path, OpenedPathInfo info) {

      Debug.WriteLine(string.Format("OnFlushHandle, path={0}, handle={1}: {2}", path, info.Handle, System.DateTime.Now));

      return this._rfs.OnFlushHandle(path, info);
    }

    protected override Errno OnCreateHandle(string path, OpenedPathInfo info, FilePermissions mode) {

      Debug.WriteLine(string.Format("OnCreateHandle, path={0}, openflags={1}, filepermission={2}: {3}", path, info.OpenAccess, mode, System.DateTime.Now));

      return this._rfs.OnCreateHandle(path, info, mode);
    }

    protected override Errno OnGetHandleStatus(string path, OpenedPathInfo info, out Stat buf) {

      Debug.WriteLine(string.Format("OnGetHandleStatus, path={0}, handle={1}: {2}", path, info.Handle, System.DateTime.Now));

      return this._rfs.OnGetHandleStatus(path, info, out buf);
    }

    protected override Errno OnGetPathStatus(string path, out Stat buf) {

      Debug.WriteLine(string.Format("OnGetPathStatus, path={0}: {1}", path, System.DateTime.Now));      

      return this._rfs.OnGetPathStatus(path, out buf);
    }

    protected override Errno OnReadSymbolicLink(string path, out string target) {

      Debug.WriteLine(string.Format("OnReadSymbolicLink, path={0}: {1}", path, System.DateTime.Now));

      return this._rfs.OnReadSymbolicLink(path, out target);
    }

    protected override Errno OnOpenDirectory(string path, OpenedPathInfo info) {

      Debug.WriteLine(string.Format("OnOpenDirectory, path={0}: {1}", path, System.DateTime.Now));

      return this._rfs.OnOpenDirectory(path, info);
    }

    protected override Errno OnAccessPath(string path, AccessModes mask) {

      Debug.WriteLine(string.Format("OnAccessPath, path={0}, mask={1}: {2}", path, mask, System.DateTime.Now));

      return this._rfs.OnAccessPath(path, mask);
    }

    protected override Errno OnReleaseDirectory(string path, OpenedPathInfo info) {

      Debug.WriteLine(string.Format("OnReleaseDirectory, path={0}, handle={1}: {2}", path, info.Handle, System.DateTime.Now));

      return this._rfs.OnReleaseDirectory(path, info);
    }

    protected override Errno OnCreateSpecialFile(string path, FilePermissions mode, ulong rdev) {

      Debug.WriteLine(string.Format("OnCreateSpecialFile, path={0}, mode={1}, rdev={2}: {3}", path, mode, rdev, System.DateTime.Now));

      return this._rfs.OnCreateSpecialFile(path, mode, rdev);
    }

    protected override Errno OnCreateSymbolicLink(string from, string to) {

      Debug.WriteLine(string.Format("OnCreateSymbolicLink, from={0}, to={1}: {2}", from, to, System.DateTime.Now));

      return this._rfs.OnCreateSymbolicLink(from, to);
    }

    protected override Errno OnGetFileSystemStatus(string path, out Statvfs stbuf) {

      Debug.WriteLine(string.Format("OnGetFileSystemStatus, path={0}: {1}", path, System.DateTime.Now));

      return this._rfs.OnGetFileSystemStatus(path, out stbuf);
    }

    protected override Errno OnSynchronizeHandle(string path, OpenedPathInfo info, bool onlyUserData) {

      Debug.WriteLine(string.Format("OnSynchronizeHandle, path={0}, handle={1}, onlyUserData={2}: {3}", path, info.Handle, onlyUserData, System.DateTime.Now));

      return this._rfs.OnSynchronizeHandle(path, info, onlyUserData);
    }

    protected override Errno OnSetPathExtendedAttribute(string path, string name, byte[] value, XattrFlags flags) {

      Debug.WriteLine(string.Format("OnSetPathExtendedAttribute, path={0}, name={1}, value={2}, flags={3}: {4}", path, name, Encoding.UTF8.GetString(value), flags, System.DateTime.Now));

      return this._rfs.OnSetPathExtendedAttribute(path, name, value, flags);
    }

    protected override Errno OnGetPathExtendedAttribute(string path, string name, byte[] value, out int bytesWritten) {

      Debug.WriteLine(string.Format("OnGetPathExtendedAttribute, path={0}, name={1}, value={2}: {3}", path, name, Encoding.UTF8.GetString(value), System.DateTime.Now));

      return this._rfs.OnGetPathExtendedAttribute(path, name, value, out bytesWritten);
    }

    protected override Errno OnListPathExtendedAttributes(string path, out string[] names) {

      Debug.WriteLine(string.Format("OnListPathExtendedAttributes, path={0}: {1}", path, System.DateTime.Now));

      return this._rfs.OnListPathExtendedAttributes(path, out names);
    }

    protected override Errno OnRemovePathExtendedAttribute(string path, string name) {

      Debug.WriteLine(string.Format("OnRemovePathExtendedAttribute, path={0}, name={1}: {2}", path, name, System.DateTime.Now));

      return this._rfs.OnRemovePathExtendedAttribute(path, name);
    }

    protected override Errno OnCreateHardLink(string from, string to) {

      Debug.WriteLine(string.Format("OnCreateHardLink, from={0}, to={1}: {2}", from, to, System.DateTime.Now));

      return this._rfs.OnCreateHardLink(from, to);
    }

    protected override Errno OnChangePathOwner(string path, long uid, long gid) {

      Debug.WriteLine(string.Format("OnChangePathOwner, path={0}, uid={1}, gid={2}: {3}", path, uid, gid, System.DateTime.Now));

      return this._rfs.OnChangePathOwner(path, uid, gid);
    }

    protected override Errno OnTruncateFile(string path, long size) {

      Debug.WriteLine(string.Format("OnTruncateFile, path={0}, size={1}: {2}", path, size, System.DateTime.Now));

      return this._rfs.OnTruncateFile(path, size);
    }

    protected override Errno OnTruncateHandle(string path, OpenedPathInfo info, long size) {

      Debug.WriteLine(string.Format("OnTruncateHandle, path={0}, handle={1}, size={2}: {3}", path, info.Handle, size, System.DateTime.Now));

      return this._rfs.OnTruncateHandle(path, info, size);
    }

    protected override Errno OnChangePathTimes(string path, ref Utimbuf buf) {

      Debug.WriteLine(string.Format("OnChangePathTimes, path={0}, buf={1}: {2}", path, buf, System.DateTime.Now));

      return this._rfs.OnChangePathTimes(path, ref buf);
    }

    #endregion
  }

  [TestFixture]
  /**
   * Just test some Mono.Fuse system and Main class features in here
   */
  public class FuseDhtTest {
  }
}