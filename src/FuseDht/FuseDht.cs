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

    private FuseDhtHelperFactory.HelperType _helpe_type = FuseDhtHelperFactory.HelperType.Dht;
    
    public static void Main(string[] args) {

      try {
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
      } catch (System.Net.WebException) {
        Console.Error.WriteLine("Soap/XmlRpc Dht interface not started. Please start it first");
      } catch (Exception e) {
        Console.WriteLine("System cannot started");
        Debug.WriteLine(e);
        //if caught unhandled exception, terminates.
        Thread.CurrentThread.Abort();
      }
    }

    private bool ParseArguments(string[] args) {
      for (int i = 0; i < args.Length; ++i) {
        switch (args[i]) {
          case "-h":
          case "--help":
            FileSystem.ShowFuseHelp("FuseDht");
            Console.Error.WriteLine("FuseDht Arguments:");
            string help = "1.\t" + "Mounting Point\n"
                        + "2.\t" + "Shadow Path"
                        +"options:\n"
                        + "-l[ocal]:\tUse LocalHT instead of Dht";
            Console.WriteLine("FuseDht Options");
            Console.Error.WriteLine(help);
            return false;
          case "-l":
            //if not specified, use Dht
            _helpe_type = FuseDhtHelperFactory.HelperType.Local;
            break;
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
      Console.WriteLine("Connecting to {0}", _helpe_type);
      this._helper = FuseDhtHelperFactory.GetFuseDhtHelper(_helpe_type, this._shadowdir);
      this._util.InitDhtRootFileStructure();
      this._util.CreateSelfBaseDir(this._helper.DhtAddress);
    }

    protected override Errno OnRenamePath(string from, string to) {

      Debug.WriteLine(string.Format("OnRenamePath, from={0}, to={1}: {2}", from, to, System.DateTime.Now));
      
      //Don't allow renaming of keydir and basedir
      string[] paths = FuseDhtUtil.ParsePath(from);
      switch (paths.Length - 1) {
        case Constants.LVL_BASE_DIR:
          if (paths[Constants.LVL_KEY_DIR_GENTR].Equals(Constants.DIR_KEY_DIR_GENERATOR)) {
            //could also be KeyGenerateor
            return Errno.EACCES;
          } else {
            //basedir
            DirectoryInfo di = new DirectoryInfo(_util.GetShadowPath(from));
            if (di.GetDirectories().Length > 0) {
              //not empty
              return Errno.EACCES;
            }
            //all right, allow you to rename, but only on the same lvl
            if(FuseDhtUtil.ParsePath(to).Length - 1 != Constants.LVL_BASE_DIR) {
              return Errno.EACCES;
            }
            //ok
            return this._rfs.OnRenamePath(from, to);
          }
        default :
          return Errno.EACCES;
      }
    }

    protected override Errno OnCreateDirectory(string path, FilePermissions mode) {

      Debug.WriteLine(string.Format("OnCreateDirectory, path={0}, mode={1}: {2}", path, mode, System.DateTime.Now));

      //dht/basedir or dht/basedir/keydir
      //or dht/KeyDirGenerator/basedir
      string[] paths = FuseDhtUtil.ParsePath(path);
      if (paths.Length > Constants.LVL_KEY_DIR + 1) { //3
        return Errno.EACCES;
      }
      this._rfs.OnCreateDirectory(path, mode);
      
      //sucessfully created. Initialize the directory structure
      if(paths.Length == Constants.LVL_KEY_DIR + 1 && !paths[Constants.LVL_BASE_DIR].Equals(Constants.DIR_KEY_DIR_GENERATOR)) {
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
        out IEnumerable<DirectoryEntry> subPaths) {
      
      Debug.WriteLine(string.Format("OnReadDirectory, path={0}, handle={1}: {2}", path, fi.Handle, System.DateTime.Now));

      //only block read of "cache"
      string[] paths = FuseDhtUtil.ParsePath(path);
      if (paths.Length == Constants.LVL_SUB_KEY_FOLDERS + 1) {
        string dir = paths[paths.Length - 1];
        if (dir.Equals(Constants.DIR_CACHE)) {
          //remote or local?
          DirectoryInfo cache = new DirectoryInfo(this._shadowdir + path);
          string key = cache.Parent.Name;
          string basedirName = cache.Parent.Parent.Name;

          bool? invalidate = (bool?)_util.ReadParam(basedirName, key, Constants.FILE_INVALIDATE);
          if (invalidate == null) {
            subPaths = null;
            return Errno.EACCES;
          }
          int? lifespan;
          if (!invalidate) {
            lifespan = (int?)_util.ReadParam(basedirName, key, Constants.FILE_LIFESPAN);
            if(lifespan == null) {
              subPaths = null;
              return Errno.EACCES;
            }

            TimeSpan ts = new TimeSpan(0, 0, lifespan.GetValueOrDefault());
            bool stale = false;

            if (cache.GetFiles().Length == 0) {
              stale = true;
            } else {
              foreach (FileInfo finfo in cache.GetFiles()) {
                if (finfo.CreationTimeUtc.Add(ts) < System.DateTime.UtcNow) {
                  stale = true;
                  break;
                }
              }
            }
            
            if (stale) {
              Console.WriteLine("Stale. Calling DhtGet");
              _helper.AsDhtGet(basedirName, key);
            }
          } else {
            Console.WriteLine("Invalidated");
            FileInfo[] fdone = cache.GetFiles(Constants.FILE_DONE);
            bool shouldCallDht = false;
            if (fdone.Length != 0) {
              int done;
              bool succ = Int32.TryParse(File.ReadAllText(fdone[0].FullName), out done);
              if (succ && done == 1) {
                Debug.WriteLine(".done found and equals 1");
                shouldCallDht = true;
              } else {
                /*
                 * If done == 0, there is an ongoing read, so don't start another one
                 */
              }
            } else {
              Console.WriteLine("No .done found");
              shouldCallDht = true;
            }
            if (shouldCallDht) {
              _util.WriteToParamFile(basedirName, key, Constants.FILE_INVALIDATE, "0");
              Debug.WriteLine("Calling DhtGet");
              _helper.AsDhtGet(basedirName, key);
            } else {
              Console.WriteLine("DhtGet not called because of on-going read on the same key");
            }
          }
        }
      }

      return this._rfs.OnReadDirectory(path, fi, out subPaths);
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
      Errno ret = this._rfs.OnReleaseHandle(path, info);
      
      if(ret != 0) {
        return ret;
      }

      //successful closed.
      string[] paths = FuseDhtUtil.ParsePath(path);
      switch (paths.Length - 1) {
        case Constants.LVL_DATA_FILE:
          //i.e dht/basedir/key/my/p2paddr1.txt
          //On write of this file
          string filename = paths[Constants.LVL_DATA_FILE];
          string folder = paths[Constants.LVL_SUB_KEY_FOLDERS];
          string key = paths[Constants.LVL_KEY_DIR];
          string basedir = paths[Constants.LVL_BASE_DIR];

          if (folder.Equals(Constants.DIR_MY)) {
            //Release of a write and only in "my" folder
            //O_RDONLY == 0
            if ((OpenFlags.O_WRONLY == (info.OpenFlags & OpenFlags.O_WRONLY)
                || OpenFlags.O_RDWR == (info.OpenFlags & OpenFlags.O_RDWR))
              && FuseDhtUtil.IsValidMyFileName(filename)) {
              byte[] value = File.ReadAllBytes(Path.Combine(_shadowdir, path.Remove(0, 1)));
              int? ttl = (int?)_util.ReadParam(basedir, key, Constants.FILE_TTL);
              PutMode? put_mode = (PutMode?)_util.ReadParam(basedir, key, Constants.FILE_PUT_MODE);
              if (ttl == null || put_mode == null) {
                return Errno.EACCES;
              }
              Debug.WriteLine("Calling DhtPut");
              _helper.AsDhtPut(basedir, key, value,ttl.GetValueOrDefault(), 
                  put_mode.GetValueOrDefault(), Path.Combine(_shadowdir, path.Remove(0, 1))); //remove the first "/" of path
            }
          }
          break;
        case Constants.LVL_BIN_KEY_FILE:
          //i.e. dht/KeyDirGenerator/basedir/binkey1.bin
          string s_file_path = Path.Combine(_shadowdir, path.Remove(0, 1));
          FileInfo fi = new FileInfo(s_file_path);
          DirectoryInfo bd = fi.Directory;
          DirectoryInfo generator = bd.Parent;

          if (generator.Name.Equals(Constants.DIR_KEY_DIR_GENERATOR)) {
            byte[] b = File.ReadAllBytes(s_file_path);
            string k = Base32.Encode(b);
            if(k.Length > Constants.MAX_KEY_LENGTH) {
              k = k.Substring(0, Constants.MAX_KEY_LENGTH);
            }
            _util.InitKeyDirStructure(bd.Name, k);
          }
          break;
        default:
          break;
      }
      return 0;
    }

    protected override Errno OnReadSymbolicLink(string path, out string target) {

      Debug.WriteLine(string.Format("OnReadSymbolicLink, path={0}: {1}", path, System.DateTime.Now));

      Errno err = this._rfs.OnReadSymbolicLink(path, out target);

      /*
       * Here it will return a real path which cause us losing control of the user operations
       * So we modify it with a relative path
       */
      string[] paths = FuseDhtUtil.ParsePath(path);

      switch (paths.Length - 1) {
        case Constants.LVL_BASE_DIR:
          //dht/myself
          string basedir = paths[Constants.LVL_BASE_DIR];
          if (basedir.Equals(Constants.LN_SELF_BASEDIR)) {
            target = target.Remove(0, Path.Combine(_shadowdir, Constants.DIR_DHT_ROOT).Length);
            if (target.StartsWith(Path.DirectorySeparatorChar.ToString()))
              target = target.Remove(0, 1);
          }
          break;
        default:
          break;
      }
      Debug.WriteLine(string.Format("Linked to {0}", target));

      return err;
    }

    #region UnmodifiedMethods
    protected override Errno OnRemoveFile(string path) {

      Debug.WriteLine(string.Format("OnRemoveFile, path={0}: {1}", path, System.DateTime.Now));
      return this._rfs.OnRemoveFile(path);
    }

    protected override Errno OnRemoveDirectory(string path) {

      Debug.WriteLine(string.Format("OnRemoveDirectory, path={0}: {1}", path, System.DateTime.Now));
      return this._rfs.OnRemoveDirectory(path);
    }

    protected override unsafe Errno OnWriteHandle(string path, OpenedPathInfo info,
        byte[] buf, long offset, out int bytesWritten) {

      Debug.WriteLine(string.Format("OnWriteHandle, path={0}, handle={1}, buflength={2}, offset={3}: {4}", path, info.Handle, buf.Length, offset, System.DateTime.Now));

      return this._rfs.OnWriteHandle(path, info, buf, offset, out bytesWritten);
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