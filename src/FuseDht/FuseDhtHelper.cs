using System;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Security.Cryptography;
using Mono.Unix.Native;
using Brunet;
using Brunet.Dht;
using Ipop;
#if FUSE_NUNIT
using NUnit.Framework;
#endif

namespace FuseDht {  
  /// <summary>
  /// Deal with Dht operations for FuseDht class
  /// </summary>
  public class FuseDhtHelper {
    public const int DHT_PUT_RETRY_TIMES = 3;
    
    private IDht _dht;
    private string _shadowdir;
    private readonly string _metadir;
    private readonly string _dht_addr;
    private readonly string _ipop_ns;
    private IXmlRpcManager _rpc;

    /**
     * Sync : Block until all results retrieved
     * Async: Non-Blocking
     * BQ: Block util the next result retrieved
     */
    public enum OpMode {
      Sync, Async, BQ
    }

    public string DhtAddress {
      get { return _dht_addr; }
    }
    
    public FuseDhtHelper(IDht dht, string shadowdir) {
      _dht = dht;
      this._shadowdir = shadowdir;
      this._metadir = Path.Combine(Path.Combine(_shadowdir, Constants.DIR_DHT_ROOT), Constants.DIR_META);
      this._dht_addr = _dht.GetDhtInfo()["address"] as string;
      try {
        this._rpc = XmlRpcManagerClient.GetXmlRpcManager();
        object[] rs = _rpc.localproxy("ipop.GetState", new object[0]);
        if (rs != null && rs.Length > 0) {
          IDictionary dic = (IDictionary)rs[0];
          _ipop_ns = dic["ipop_namespace"] as string;
        } else {
          _ipop_ns = string.Empty;
        }
      } catch (Exception e) {
        Debug.WriteLine(e);
        _ipop_ns = string.Empty;
      }
    }

    /**
     * Achieve non-blocking by using ThreadPool
     */
    public void AsDhtGet(string basedirName, string key) {
      DhtGet(basedirName, key, OpMode.Async);
    }

    public void DhtGet(string basedirName, string key, OpMode mode) {
      DhtGet(basedirName, key, mode, null, null);
    }

    public void DhtGet(string basedirName, string key, OpMode mode, string expectedFileName, AutoResetEvent re) {
      string dht_key = FuseDhtUtil.GenDhtKey(basedirName, key, _ipop_ns);
      string s_cache = _shadowdir + Path.DirectorySeparatorChar
                     + Constants.DIR_DHT_ROOT + Path.DirectorySeparatorChar
                     + basedirName + Path.DirectorySeparatorChar
                     + key + Path.DirectorySeparatorChar
                     + Constants.DIR_CACHE;
      DirectoryInfo cache = new DirectoryInfo(s_cache);
      cache.Delete(true);
      cache.Create();
      File.WriteAllText(Path.Combine(s_cache, Constants.FILE_DONE), "0");
      ArrayList state = new ArrayList();
      state.Add(dht_key);
      state.Add(basedirName);
      state.Add(key);
      switch (mode) {
        case OpMode.Async:
          ThreadPool.QueueUserWorkItem(new WaitCallback(this.GetProc), state);
          break;
        case OpMode.Sync:
          this.GetProc(state);
          break;
        case OpMode.BQ:
          if (expectedFileName != null) {
            state.Add(expectedFileName);
            state.Add(re);
          }
          ThreadPool.QueueUserWorkItem(new WaitCallback(this.BQGetProc), state);
          break;
        default:
          break;
      }
    }

    /**
     * This is method uses the method that is only included in ISoapDht. So if other
     * interfaces are used here, a casting exception will be thrown.
     */
    public void BQGetProc(object ostate) {
      IList state = (IList)ostate;
      string dht_key = state[0] as string;
      string base_dir_name = state[1] as string;
      string key = state[2] as string;
      string waitingFileName = null;
      AutoResetEvent re = null;
      if (state.Count > 4) {
        waitingFileName = state[3] as string;
        re = state[4] as AutoResetEvent;
      }

      string s_parent_path = _shadowdir + Path.DirectorySeparatorChar
                           + Constants.DIR_DHT_ROOT + Path.DirectorySeparatorChar
                           + base_dir_name + Path.DirectorySeparatorChar
                           + key + Path.DirectorySeparatorChar
                           + Constants.DIR_CACHE;

      //Handle the exception if this casting fails
      ISoapDht dht = (ISoapDht)_dht;
      Debug.WriteLine(string.Format("Getting {0}", dht_key));
      IBlockingQueue bq = dht.GetAsBlockingQueue(dht_key);
      int i = 0;
      bool set = false;
      while (true) {
        // Still a chance for Dequeue to execute on an empty closed queue 
        // so we'll do this instead.
        try {
          DhtGetResult result = (DhtGetResult)bq.Dequeue();
          Debug.WriteLine(string.Format("Got #{0} item", i++));
          DhtDataFile file = new DhtDataFile(s_parent_path, result);
          file.WriteToFile();
          if(waitingFileName != null && file.Name.Equals(waitingFileName)) {
            //notify the waiting thread that the expected file arrives
            Debug.WriteLine(string.Format("Got the expected file"));
            re.Set();
            set = true;
          }
        } catch (Exception e) {
          Debug.WriteLine(e);
          break;
        }
      }
      //set again in case no such filename in Dht
      File.WriteAllText(Path.Combine(s_parent_path, Constants.FILE_DONE), "1"); //done
      if (!set) {
        //no filename matched. So I release the waiting thread at the end
        re.Set(); 
      }
    }

    public void GetProc(object ostate) {
      IList state = (IList)ostate;
      string dht_key = state[0] as string;
      string base_dir_name = state[1] as string;
      string key = state[2] as string;

      string s_parent_path = _shadowdir + Path.DirectorySeparatorChar
                           + Constants.DIR_DHT_ROOT + Path.DirectorySeparatorChar
                           + base_dir_name + Path.DirectorySeparatorChar
                           + key + Path.DirectorySeparatorChar
                           + Constants.DIR_CACHE;

      Debug.WriteLine(string.Format("Getting {0}", dht_key));
      DhtGetResult[] results = _dht.Get(dht_key);
      Debug.WriteLine(string.Format("Got {0} items", results.Length));
      foreach (DhtGetResult result in results) {
        DhtDataFile file = new DhtDataFile(s_parent_path, result);
        file.WriteToFile();
      }
      
      File.WriteAllText(Path.Combine(s_parent_path, Constants.FILE_DONE), "1"); //done
    }

    public void AsDhtPut(string basedirName, string key, byte[] value, int ttl, PutMode putMode, string s_filePath) {
      string dht_key = FuseDhtUtil.GenDhtKey(basedirName, key, _ipop_ns);
      ArrayList state = new ArrayList();
      state.Add(dht_key);
      state.Add(basedirName);
      state.Add(key);
      state.Add(value);
      state.Add(ttl);
      state.Add(s_filePath);
      state.Add(putMode);
      ThreadPool.QueueUserWorkItem(new WaitCallback(this.PutProc), state);
    }

    public void PutProc(object ostate) {
      IList state = (IList)ostate;
      string dht_key = state[0] as string;
      string base_dir_name = state[1] as string;
      string key = state[2] as string;
      string value = Encoding.UTF8.GetString((byte[])state[3]);
      int ttl = (int)state[4];
      string s_file_path = state[5] as string;
      PutMode put_mode = (PutMode)state[6];

      bool result;

      for (int i = 0; i < DHT_PUT_RETRY_TIMES; i++) {
        if (put_mode == PutMode.Create) {
          Debug.WriteLine(string.Format("Creating {0}, {1}", dht_key, new FileInfo(s_file_path).Name));
          result = _dht.Create(dht_key, value, ttl);
        } else {
          Debug.WriteLine(string.Format("Putting {0}, {1}", dht_key, new FileInfo(s_file_path).Name));
          result = _dht.Put(dht_key, value, ttl);
        }
        Debug.WriteLine(string.Format("Put/Create returned: {0}", result));

        FileInfo fi = new FileInfo(s_file_path);
        if (!result) {
          //add a suffix .offline to the file
          if (s_file_path.EndsWith(Constants.FILE_OFFLINE)) {
            //file.offline -> file.offline.1
            s_file_path += "." + i.ToString();
          } else if(s_file_path.Remove(s_file_path.Length - 2).EndsWith(Constants.FILE_OFFLINE)) {
            s_file_path = s_file_path.Remove(s_file_path.Length - 2) + "." + i.ToString();
          } else {
            //file -> file.offline
            s_file_path += Constants.FILE_OFFLINE;
          }
          fi.MoveTo(s_file_path);
        } else {
          //suffix .uploaded
          s_file_path = FuseDhtUtil.TrimPathExtension(s_file_path);
          //append .uploaded
          fi.MoveTo(s_file_path + Constants.FILE_UPLOADED);
          DhtMetadataFile file = new DhtMetadataFile(ttl, 
              s_file_path + Constants.FILE_UPLOADED);
          Debug.WriteLine(string.Format("Dropping file {0} to meta folder. {1}", file._meta_filename, DateTime.Now));
          DhtMetadataFileHandler.WriteAsXml(_metadir, file);
          break;
        } 
      }
    }

    
  }

#if FUSE_NUNIT
  [TestFixture]
  public class FuseDhtHelperTest {
  }
#endif
}
