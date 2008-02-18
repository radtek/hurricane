using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using Mono.Fuse;
using Mono.Unix.Native;
using System.Threading;
using Brunet;
using System.Runtime.Remoting.Messaging;
using System.IO;
#if FUSE_NUNIT
using NUnit.Framework;
#endif


namespace Fushare.Filesystem {

  /**
   * Interrupts system calls and weaves in FuseDht logic.
   * 
   */
  public class FuseFS : FileSystem {

    #region Fields
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FuseFS));
    private FuseDhtHelper _helper;
    private FuseDhtUtil _util;
    private string _shadowdir;
    private RedirectFHFSHelper _rfs;
    private bool _auto_renew;
    private FuseDhtHelperFactory.HelperType _helper_type = FuseDhtHelperFactory.HelperType.Dht;
    private int _dht_port = 51515;
    private int _xmlrpc_port = 10000;
    private IDictionary _helper_options = new ListDictionary();
    
    #endregion

    public bool ParseArguments(string[] args) {
      for (int i = 0; i < args.Length; ++i) {
        switch (args[i]) {
          case "-h":
          case "--help":
            FileSystem.ShowFuseHelp("FuseDht");
            string help = "FuseDht Arguments:\n"
                        + "1.\t" + "Mounting Point\n"
                        + "2.\t" + "Shadow Path\n"
                        + "FuseDht Options:\n"
                        + "-l[ocal]:\tUse LocalHT instead of Dht";
            Console.Error.WriteLine(help);
            return false;
          case "-l":
            //if not specified, use Dht
            _helper_type = FuseDhtHelperFactory.HelperType.Local;
            break;
          case "-dp":
          case "-dht_port":
            if (i == args.Length - 1) {
              //no next value
              Console.Error.WriteLine("No dht service specified");
              return false;
            }
            if (!Int32.TryParse(args[++i], out _dht_port)) {
              Console.Error.WriteLine("Invalid dht service port");
              return false;
            }
            break;
          case "-xp":
          case "-xmlrpc_port":
            if (i == args.Length - 1) {
              //no next value
              Console.Error.WriteLine("No xmlrpc service specified");
              return false;
            }
            if (!Int32.TryParse(args[++i], out _xmlrpc_port)) {
              Console.Error.WriteLine("Invalid xmlrpc service port");
              return false;
            }
            break;
          case "-ar":
          case "-auto_renew":
            _auto_renew = true;
            break;
          default:
            if (string.IsNullOrEmpty(base.MountPoint)) {
              base.MountPoint = args[i];
              Console.WriteLine("MountPoint", args[i]);
            } else if (string.IsNullOrEmpty(this._shadowdir)) {
              _shadowdir = args[i];
              _helper_options.Add("shadow_dir", _shadowdir);
              Console.WriteLine("Shadow", args[i]);
            }
            break;
        }
      }
      return true;
    }

    public void InitAndStartFS(string[] args) {
      string[] unhandled = this.ParseFuseArguments(args);
      foreach (string key in this.FuseOptions.Keys) {
        Console.WriteLine("Option={1}", key, this.FuseOptions[key]);
      }
      if (!this.ParseArguments(unhandled))
        return;
      this.InitFuseDhtSystem();
      this.Start();
    }
    
    public void InitFuseDhtSystem() {
      _helper_options.Add("helper_type", _helper_type);
      _helper_options.Add("dht_port", _dht_port);
      _helper_options.Add("xmlrpc_port", _xmlrpc_port);

      this._rfs = new RedirectFHFSHelper(this._shadowdir);
      this._util = new FuseDhtUtil(this._shadowdir);
      Console.WriteLine("Connecting to {0}", _helper_type);
      this._helper = FuseDhtHelperFactory.GetFuseDhtHelper(this._helper_options);
      this._util.InitDhtRootFileStructure();
      this._util.CreateSelfBaseDir(this._helper.DhtAddress);
      if (_auto_renew) {
        DhtFileManager.StartAsThread(Path.Combine(Path.Combine(_shadowdir, Constants.DIR_DHT_ROOT), Constants.DIR_META), _helper); 
      }
    }

    protected override Errno OnRenamePath(string from, string to) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("OnRenamePath, from={0}, to={1}", from, to));
      
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
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("OnCreateDirectory, path={0}, mode={1}", path, mode));

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
          Logger.WriteLineIf(LogLevel.Error, _log_props,
              "Init key dir failed. Removing dir...", e);
          Directory.Delete(s_path, true);
          return Errno.EACCES;
        }
      }
      return 0;
    }

    protected override Errno OnReadDirectory(string path, OpenedPathInfo fi,
        out IEnumerable<DirectoryEntry> subPaths) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("OnReadDirectory, path={0}, handle={1}", path, fi.Handle));

      //only block read of "cache"
      string[] paths = FuseDhtUtil.ParsePath(path);
      if (paths.Length == Constants.LVL_SUB_KEY_FOLDERS + 1) {
        string dir = paths[paths.Length - 1];
        if (dir.Equals(Constants.DIR_CACHE)) {
          //remote or local?
          DirectoryInfo cache = new DirectoryInfo(_util.GetShadowPath(path));
          string key = cache.Parent.Name;
          string basedirName = cache.Parent.Parent.Name;
          bool shouldCallDht = false;
          try {
            shouldCallDht = ShouldCallDhtGet(path);
          } catch (Exception) {
            subPaths = null;
            return Errno.EACCES;
          }
          if (shouldCallDht) {
            //write to invalidate file even if it's already false/0
            _util.WriteToParamFile(basedirName, key, Constants.FILE_INVALIDATE, "0");
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                "Calling DhtGet");
            //_helper.AsDhtGet(basedirName, key);
            //_helper.DhtGet(basedirName, key, FuseDhtHelper.OpMode.BQ);
            _helper.DhtGet(basedirName, key, FuseDhtHelper.OpMode.Sync);
          } else {
            Logger.WriteLineIf(LogLevel.Info, _log_props,
                "DhtGet not called because of on-going read on the same key or cached files are still new");
          }
        }
      }
      return this._rfs.OnReadDirectory(path, fi, out subPaths);
    }

    /**
     * @return true if we should call DhtGet
     * @exception if arg files have invalid values
     */
    private bool ShouldCallDhtGet(string f_cachePath) {
      DirectoryInfo cache = new DirectoryInfo(_util.GetShadowPath(f_cachePath));
      string key = cache.Parent.Name;
      string basedirName = cache.Parent.Parent.Name;

      bool shouldCallDht = false;
      bool? invalidate = (bool?)_util.ReadParam(basedirName, key, Constants.FILE_INVALIDATE);
      if (invalidate == null) {
        //subPaths = null;
        //return Errno.EACCES;
        throw new FuseDhtStructureException();
      }

      /*
       * .done file should be the first condition to check. if there is another ongoing thread, then
       *  there is no need to see if anything else should be done
       */
      FileInfo[] fdone = cache.GetFiles(Constants.FILE_DONE);
      if (fdone.Length != 0) {
        int done;
        bool succ = Int32.TryParse(File.ReadAllText(fdone[0].FullName), out done);
        
        if (succ && done == 0) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            ".done found and equals 0");
          return false;
        }
      }

      int? lifespan;
      if (!invalidate.GetValueOrDefault() && cache.GetFiles().Length > 1) {
        //if cache directory is empty (only contains .done file), we still consider it as invalidated
        lifespan = (int?)_util.ReadParam(basedirName, key, Constants.FILE_LIFESPAN);
        if (lifespan == null) {
          //subPaths = null;
          //return Errno.EACCES;
          throw new FuseDhtStructureException();
        }

        bool stale = false;

        if (lifespan > 0) {
          TimeSpan ts = new TimeSpan(0, 0, lifespan.GetValueOrDefault());
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
        } else {
          //we don't use lifespan if it's <= 0
          DateTime? dt = (DateTime?)_util.ReadParam(basedirName, key, Constants.FILE_REFRESH);
          if (dt == null) {
            throw new FuseDhtStructureException();
          } else {
            if (dt <= DateTime.Now) {
              //comparison of local times
              Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                  string.Format("At least one of the files in this directories passed its end time, stale"));
              stale = true;
            }
          }
        }

        if (stale) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              "Stale. Calling DhtGet");
          shouldCallDht = true;
        }
      } else {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              "Invalidated");
        shouldCallDht = true;
      }
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("ShouldCallDht={0}", shouldCallDht));
      return shouldCallDht;
    }

    protected override Errno OnReleaseHandle(string path, OpenedPathInfo info) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("OnReleaseHandle, path={0}, handle={1}, openflags={2}", path, info.Handle, info.OpenFlags));
      
      Errno ret = this._rfs.OnReleaseHandle(path, info);
      if(ret != 0) {
        return ret;
      }

      //successful closed.
      string[] paths = FuseDhtUtil.ParsePath(path);
      try {
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
                string value = File.ReadAllText(Path.Combine(_shadowdir, path.Remove(0, 1)));
                byte[] dht_val = FuseDhtUtil.GenerateDhtValue(filename, value);

                int? ttl = (int?)_util.ReadParam(basedir, key, Constants.FILE_TTL);
                PutMode? put_mode = (PutMode?)_util.ReadParam(basedir, key, Constants.FILE_PUT_MODE);
                if (ttl == null || put_mode == null) {
                  return Errno.EACCES;
                }
                Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                    "Calling DhtPut");
                _helper.AsDhtPut(basedir, key, dht_val, ttl.GetValueOrDefault(),
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
              if (k.Length > Constants.MAX_KEY_LENGTH) {
                k = k.Substring(0, Constants.MAX_KEY_LENGTH);
              }
              _util.InitKeyDirStructure(bd.Name, k);
            }
            break;
          case Constants.LVL_CONF_FILE:
            //.ie. /dht/fusedht.config
            Console.WriteLine(FuseDhtConfigHandler.cfgPath);
            Console.WriteLine(_util.GetShadowPath(path));
            if (FuseDhtConfigHandler.cfgPath.Equals(_util.GetShadowPath(path))) {
              FuseDhtConfigHandler.Refresh();
              Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                  FuseDhtConfig.GetInstance().ToString());
            }
            break;
          default:
            break;
        }
      } catch (Exception e) {
        /*
         * if things caught in here. We just log the message but still return 0 because the release of
         * the handle succeeded.
         */
        Logger.WriteLineIf(LogLevel.Error, _log_props,
            e);
      }
      return 0;
    }

    protected override Errno OnReadSymbolicLink(string path, out string target) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("OnReadSymbolicLink, path={0}", path));
      Errno err = this._rfs.OnReadSymbolicLink(path, out target);

      /*
       * Here it will return a real path which causes us losing control of the user operations
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
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Linked to {0}", target));
      return err;
    }


    protected override Errno OnGetPathStatus(string path, out Stat buf) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("OnGetPathStatus, path={0}", path));

      string[] paths = FuseDhtUtil.ParsePath(path);

      switch (paths.Length - 1) {
        case Constants.LVL_DATA_FILE:
          //i.e. /dht/basedir/key1/cache/file1.txt
          FileInfo finfo = new FileInfo(_util.GetShadowPath(path));
          DirectoryInfo dir = finfo.Directory;
          DirectoryInfo keydir = dir.Parent;
          DirectoryInfo basedir = keydir.Parent;
          //Get the status from shadow path
          Errno rs = this._rfs.OnGetPathStatus(path, out buf);
          if (dir.Name.Equals(Constants.DIR_CACHE)) {
            // in cache dir
            bool shouldCallDht = false;
            /*
             * if there is another ongoing thread reading?
             * User console should be blocked by the 1st read but FUSE really could read twice.
             */
            FileInfo[] fdone = dir.GetFiles(Constants.FILE_DONE);
            if (fdone.Length != 0) {
              int done;
              bool succ = Int32.TryParse(File.ReadAllText(fdone[0].FullName), out done);

              if (succ && done == 0) {
                //Another read ongoing
                Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                    ".done found and equals 0");
                return rs;
              }
            }

            //Otherwise
            if (rs == Errno.ENOENT &&
                !FuseDhtUtil.IsIgnoredFilename(finfo.Name)) {
              //currently there is no such file
              Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                    "No such file in shadow FS");
              bool? blocking = (bool?)_util.ReadParam(basedir.Name, keydir.Name, Constants.FILE_BLOCKING_RD);
              if (blocking == null) {
                return Errno.EACCES;
              } else if (blocking.GetValueOrDefault()) {
                shouldCallDht = true;
              } else {
                //non blocking, return the result of the shadow
                return rs;
              }
            } else if (!FuseDhtUtil.IsIgnoredFilename(finfo.Name)) {
              //the file is already there, is it stale?
              try {
                Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                    "Calling ShouldCallDhtGet");
                shouldCallDht = this.ShouldCallDhtGet(_util.GetFusePath(dir.FullName));
              } catch (Exception e) {
                Logger.WriteLineIf(LogLevel.Error, _log_props,
                  e);
                return Errno.EACCES;
              }
            }
            if (shouldCallDht) {
              //blocking, get the file and then return
              AutoResetEvent re = new AutoResetEvent(false);
              try {
                _helper.DhtGet(basedir.Name, keydir.Name, FuseDhtHelper.OpMode.Sync, finfo.Name, re);
                DateTime t = DateTime.UtcNow;
                re.WaitOne();
                Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                    string.Format("Waited on expectedFileArrived Event for {0}",
                    Convert.ToString((DateTime.UtcNow - t).TotalMilliseconds)));
                //the file arrived, so we do this again
                _util.WriteToParamFile(basedir.Name, keydir.Name, Constants.FILE_INVALIDATE, "0");
              } catch (Exception ex) {
                //Log and just let the redirect FS read the content and return
                Logger.WriteLineIf(LogLevel.Verbose, _log_props, ex);
              }
              Errno e = this._rfs.OnGetPathStatus(path, out buf);
              Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                  string.Format("\tFilePermission of the path={0}", buf.st_mode));
              return e;
            }
          }
          break;
        default:
          break;
      }

      
      Errno ret = this._rfs.OnGetPathStatus(path, out buf);
     
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("\tFilePermission of the path={0}", buf.st_mode));
      return ret;
    }

    #region Unmodified Methods
    protected override Errno OnRemoveFile(string path) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnRemoveFile, path={0}", path));
      return this._rfs.OnRemoveFile(path);
    }

    protected override Errno OnRemoveDirectory(string path) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnRemoveDirectory, path={0}", path));
      return this._rfs.OnRemoveDirectory(path);
    }

    protected override unsafe Errno OnWriteHandle(string path, OpenedPathInfo info,
        byte[] buf, long offset, out int bytesWritten) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnWriteHandle, path={0}, handle={1}, buflength={2}, offset={3}", path, info.Handle, buf.Length, offset));

      return this._rfs.OnWriteHandle(path, info, buf, offset, out bytesWritten);
    }

    protected override unsafe Errno OnReadHandle(string path, OpenedPathInfo info, byte[] buf,
        long offset, out int bytesRead) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnReadHandle, path={0}, handle={1}, buflength={2}, offset={3}", path, info.Handle, buf.Length, offset));

      return this._rfs.OnReadHandle(path, info, buf, offset, out bytesRead);
    }

    protected override Errno OnChangePathPermissions(string path, FilePermissions mode) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnChangePathPermissions, path={0}, filepermission={1}", path, mode));

      return this._rfs.OnChangePathPermissions(path, mode);
    }

    protected override Errno OnOpenHandle(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnOpenHandle, path={0}, openflags={1}", path, info.OpenFlags));

      return this._rfs.OnOpenHandle(path, info);
    }

    protected override Errno OnFlushHandle(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnFlushHandle, path={0}, handle={1}", path, info.Handle));

      return this._rfs.OnFlushHandle(path, info);
    }

    protected override Errno OnCreateHandle(string path, OpenedPathInfo info, FilePermissions mode) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnCreateHandle, path={0}, openflags={1}, filepermission={2}", path, info.OpenAccess, mode));

      return this._rfs.OnCreateHandle(path, info, mode);
    }

    protected override Errno OnGetHandleStatus(string path, OpenedPathInfo info, out Stat buf) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnGetHandleStatus, path={0}, handle={1}", path, info.Handle));

      return this._rfs.OnGetHandleStatus(path, info, out buf);
    }

    protected override Errno OnOpenDirectory(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnOpenDirectory, path={0}", path));

      return this._rfs.OnOpenDirectory(path, info);
    }

    protected override Errno OnAccessPath(string path, AccessModes mask) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnAccessPath, path={0}, mask={1}", path, mask));

      return this._rfs.OnAccessPath(path, mask);
    }

    protected override Errno OnReleaseDirectory(string path, OpenedPathInfo info) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnReleaseDirectory, path={0}, handle={1}", path, info.Handle));

      return this._rfs.OnReleaseDirectory(path, info);
    }

    protected override Errno OnCreateSpecialFile(string path, FilePermissions mode, ulong rdev) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnCreateSpecialFile, path={0}, mode={1}, rdev={2}", path, mode, rdev));

      return this._rfs.OnCreateSpecialFile(path, mode, rdev);
    }

    protected override Errno OnCreateSymbolicLink(string from, string to) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnCreateSymbolicLink, from={0}, to={1}", from, to));

      return this._rfs.OnCreateSymbolicLink(from, to);
    }

    protected override Errno OnGetFileSystemStatus(string path, out Statvfs stbuf) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnGetFileSystemStatus, path={0}", path));
      Errno rs =  this._rfs.OnGetFileSystemStatus(path, out stbuf);
      return rs;
    }

    protected override Errno OnSynchronizeHandle(string path, OpenedPathInfo info, bool onlyUserData) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnSynchronizeHandle, path={0}, handle={1}, onlyUserData={2}", path, info.Handle, onlyUserData));

      return this._rfs.OnSynchronizeHandle(path, info, onlyUserData);
    }

    protected override Errno OnSetPathExtendedAttribute(string path, string name, byte[] value, XattrFlags flags) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnSetPathExtendedAttribute, path={0}, name={1}, value={2}, flags={3}", path, name, Encoding.UTF8.GetString(value), flags));

      return this._rfs.OnSetPathExtendedAttribute(path, name, value, flags);
    }

    protected override Errno OnGetPathExtendedAttribute(string path, string name, byte[] value, out int bytesWritten) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnGetPathExtendedAttribute, path={0}, name={1}, value={2}", path, name, Encoding.UTF8.GetString(value)));

      return this._rfs.OnGetPathExtendedAttribute(path, name, value, out bytesWritten);
    }

    protected override Errno OnListPathExtendedAttributes(string path, out string[] names) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnListPathExtendedAttributes, path={0}", path));

      return this._rfs.OnListPathExtendedAttributes(path, out names);
    }

    protected override Errno OnRemovePathExtendedAttribute(string path, string name) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnRemovePathExtendedAttribute, path={0}, name={1}", path, name));

      return this._rfs.OnRemovePathExtendedAttribute(path, name);
    }

    protected override Errno OnCreateHardLink(string from, string to) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnCreateHardLink, from={0}, to={1}", from, to));

      return this._rfs.OnCreateHardLink(from, to);
    }

    protected override Errno OnChangePathOwner(string path, long uid, long gid) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnChangePathOwner, path={0}, uid={1}, gid={2}", path, uid, gid));

      return this._rfs.OnChangePathOwner(path, uid, gid);
    }

    protected override Errno OnTruncateFile(string path, long size) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnTruncateFile, path={0}, size={1}", path, size));

      return this._rfs.OnTruncateFile(path, size);
    }

    protected override Errno OnTruncateHandle(string path, OpenedPathInfo info, long size) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnTruncateHandle, path={0}, handle={1}, size={2}", path, info.Handle, size));

      return this._rfs.OnTruncateHandle(path, info, size);
    }

    protected override Errno OnChangePathTimes(string path, ref Utimbuf buf) {

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("OnChangePathTimes, path={0}, buf={1}", path, buf));

      return this._rfs.OnChangePathTimes(path, ref buf);
    }

    #endregion
  }

#if FUSE_NUNIT 
  [TestFixture]
  /**
   * Just test some Mono.Fuse system and Main class features in here
   */
  public class FuseDhtTest {
  }
#endif
}