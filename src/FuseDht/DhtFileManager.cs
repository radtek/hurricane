using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
#if FUSE_DEBUG
using NUnit.Framework;
#endif

namespace FuseSolution.FuseDht {
  public class DhtFileManager {
    List<DhtMetadataFile> _expiringFiles = new List<DhtMetadataFile>();
    DateTime _wakeup_time;
    readonly string _s_meta_dir;
    readonly string _renew_log;
    AutoResetEvent _wakeup_event = new AutoResetEvent(false);
    readonly FuseDhtHelper _helper;

    public DhtFileManager(string sMetaDir, FuseDhtHelper helper) {
      _s_meta_dir = sMetaDir;
      _renew_log = Path.Combine(new DirectoryInfo(_s_meta_dir).Parent.GetDirectories(Constants.DIR_LOG)[0].FullName, 
          Constants.FILE_RENEW_LOG);
      _helper = helper;
      ExpiringEvent += new EventHandler(this.DhtPutHandler);
    }

    public void Run(object ostate) {
      DirectoryInfo dir = new DirectoryInfo(_s_meta_dir);
      if (!dir.Exists)
        throw new FuseDhtStructureException("meta dir expected", _s_meta_dir);
      long l = 0;
      while (true) {
        ScanDirectory(dir);
        Debug.WriteLine(string.Format("DhtFileManager: #{0} scan at {1}", l++, DateTime.Now));
        TimeSpan sleeping_time = (_wakeup_time - DateTime.UtcNow);
        if (sleeping_time < TimeSpan.Zero) {
          //hurry up. we are already late
          continue;
        } else {
          //kinda tired, wait here.
          _wakeup_event.Reset();
          //never feed a sleeping_time < -1 to this
          Debug.WriteLine(string.Format("DhtFileManager: Wait until LocalTime {0}", _wakeup_time.ToLocalTime()));
          _wakeup_event.WaitOne(sleeping_time, true);
        }
      }
    }
    
    public static void StartAsThread(string sMetaDir, FuseDhtHelper helper) {
      FileSystemWatcher watcher = new FileSystemWatcher();
      watcher.Path = sMetaDir;

      DhtFileManager m = new DhtFileManager(sMetaDir, helper);
      watcher.Created += new FileSystemEventHandler(m.OnNewFileComes);
      watcher.EnableRaisingEvents = true;

      Thread t = new Thread(m.Run);
      t.Start(new object());
    }

    public void OnNewFileComes(object source, FileSystemEventArgs e) {
      Debug.WriteLine(string.Format("DhtFileManager: New File {0} comes at {1}, threadID: {2}", 
          e.Name, DateTime.Now, Thread.CurrentThread.GetHashCode()));
      string s_file = e.FullPath;
      DhtMetadataFile meta = DhtMetadataFileHandler.ReadFromXml(s_file);
      if(meta.EndTimeUtc < _wakeup_time) {
        Debug.WriteLine(string.Format("EndTimeUtc of this file earilier than _wakeup_time, Set event. {0}", 
            DateTime.Now));
        _wakeup_event.Set();
      }
    }

    public void ScanDirectory(DirectoryInfo dir) {
      FileInfo[] files = dir.GetFiles();
      if (files.Length == 0) {
        //if the folder is empty, wait 5 more seconds
        Debug.WriteLine(string.Format("DhtFileManager: Folder empty. Wait 10 more seconds"));
        _wakeup_time = DateTime.UtcNow + new TimeSpan(0, 0, 10);
        return;
      } else {
        Debug.WriteLine(string.Format("{0} files under this folder", files.Length));
      }

      foreach(FileInfo f in files) {
        DhtMetadataFile meta = DhtMetadataFileHandler.ReadFromXml(f.FullName);
#if FUSE_NUNIT
        Assert.AreEqual(DateTimeKind.Local ,meta.end_time.Kind);
#endif
        if (IsExpiring(meta)) {
          _expiringFiles.Add(meta);
        } else {
          if (_wakeup_time <= DateTime.UtcNow && meta.EndTimeUtc > DateTime.UtcNow) {
            _wakeup_time = meta.EndTimeUtc;
          } else if (_wakeup_time > DateTime.UtcNow && meta.EndTimeUtc < _wakeup_time) {
            //to find the earliest expring time as wakeup time
            _wakeup_time = meta.EndTimeUtc;
          }
        }
      }
      //after one scan
      if (_wakeup_time <= DateTime.UtcNow) {
        //this means that all of the files would be executed this time
        _wakeup_time = DateTime.UtcNow + new TimeSpan(0, 0, 10);
      }

      Debug.WriteLine(string.Format("DhtFileManager: Found {0} expiring files after one scan, threadID: {1}", 
          _expiringFiles.Count, Thread.CurrentThread.GetHashCode()));
      foreach(DhtMetadataFile m in _expiringFiles) {
        ExpiringEvent(this, new ExpiringArgs(m.s_data_file_path, m.ttl, 
            Path.Combine(_s_meta_dir, m._meta_filename)));
      }
      _expiringFiles.Clear();
    }

    public bool IsExpiring(DhtMetadataFile file) {
      if (file.EndTimeUtc - DateTime.UtcNow < new TimeSpan(0, 0, 30)) {
        //if EndTimeUtc is somehow less than utcnow, we still add it in
        return true;
      } else {
        return false;
      }
    }

    public void DhtPutHandler(Object sender, EventArgs e) {
      Debug.WriteLine(string.Format("DhtFileManager: DhtPutHanlder caught an event, threadID: {0}", 
          Thread.CurrentThread.GetHashCode()));
      ExpiringArgs args = (ExpiringArgs)e;
      string data_file = args.DataFileSPath;
      string filename = FuseDhtUtil.TrimPathExtension(new FileInfo(data_file).Name);

      FileInfo f = new FileInfo(data_file);
      DirectoryInfo keydir = f.Directory.Parent;
      DirectoryInfo basedir = keydir.Parent;

      int ttl = args.TTL;
      byte[] data = File.ReadAllBytes(data_file);
      byte[] dht_data = FuseDhtUtil.GenerateDhtValue(filename, data);
      File.Delete(args.MetaFileSPath);
      _helper.AsDhtPut(basedir.Name, keydir.Name, dht_data, ttl, PutMode.Put, data_file);
      File.AppendAllText(_renew_log, string.Format("File {0} renewed for {1} seconds at {2}\n", data_file, ttl, DateTime.Now));
    }

    /**
     * When the meta file is expired, the real data on Dht should be
     * "expiring".
     */
    public event EventHandler ExpiringEvent;

    public class ExpiringArgs : EventArgs {
      public readonly string DataFileSPath;
      public readonly int TTL;
      public readonly string MetaFileSPath;

      public ExpiringArgs(string dataFilePath, int ttl, string metaFilePath) {
        this.DataFileSPath = dataFilePath;
        this.TTL = ttl;
        this.MetaFileSPath = metaFilePath;
      }
    }
  }

#if FUSE_DEBUG
  [TestFixture]
  public class DhtFileManagerTest {
    [TestFixtureSetUp]
    public void SetUp() {
      Directory.CreateDirectory("/tmp/dhtmeta");
    }
  }
#endif
}
