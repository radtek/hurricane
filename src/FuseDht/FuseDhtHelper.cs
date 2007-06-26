using Mono.Unix.Native;
using System;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.IO;
using System.Collections;
using Ipop;
using Brunet;
using Brunet.Dht;
using System.Diagnostics;
using System.Security.Cryptography;
using NUnit.Framework;

namespace FuseDht {  
  /// <summary>
  /// Deal with Dht operations for FuseDht class
  /// </summary>
  public class FuseDhtHelper {
    private IDht _dht;
    private string _shadowdir;
    private readonly string _dht_addr;

    public string DhtAddress {
      get { return _dht_addr; }
    }
    
    public FuseDhtHelper(IDht dht, string shadowdir) {
      _dht = dht;
      this._shadowdir = shadowdir;

      this._dht_addr = _dht.GetDhtInfo()["address"] as string;
    }

    /**
     * Achieve non-blocking by using ThreadPool
     */
    public void AsDhtGet(string basedirName, string key) {
      string dht_key = FuseDhtUtil.GenDhtKey(basedirName, key);
      string s_cache = _shadowdir + Path.DirectorySeparatorChar
                     + Constants.DIR_DHT_ROOT + Path.DirectorySeparatorChar
                     + basedirName + Path.DirectorySeparatorChar
                     + key + Path.DirectorySeparatorChar
                     + Constants.DIR_CACHE;
      DirectoryInfo cache = new DirectoryInfo(s_cache);
      cache.Delete(true);
      cache.Create();
      File.WriteAllText(Path.Combine(s_cache, Constants.FILE_DONE),"0");
      ArrayList state = new ArrayList();
      state.Add(dht_key);
      state.Add(basedirName);
      state.Add(key);
      ThreadPool.QueueUserWorkItem(new WaitCallback(this.GetProc), state);
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
      
      File.WriteAllText(Path.Combine(s_parent_path, Constants.FILE_DONE), "1");
    }

    public void AsDhtPut(string basedirName, string key, byte[] value, int ttl, PutMode putMode, string s_filePath) {
      string dht_key = FuseDhtUtil.GenDhtKey(basedirName, key);
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
      if (put_mode == PutMode.Create) {
        Debug.WriteLine(string.Format("Creating {0}", dht_key));
        result = _dht.Create(dht_key, value, ttl);
      } else {
        Debug.WriteLine(string.Format("Putting {0}", dht_key));
        result = _dht.Put(dht_key, value, ttl);
      }
      Debug.WriteLine(string.Format("Put/Create returned: {0}", result));

      FileInfo fi = new FileInfo(s_file_path);
      if (!result) {
        //add a suffix .offline to the file
        fi.MoveTo(s_file_path + Constants.FILE_OFFLINE);
      } else {
        //suffix .uploaded
        fi.MoveTo(s_file_path + Constants.FILE_UPLOADED);
      }
    }
  }

  [TestFixture]
  public class FuseDhtHelperTest {
  }

}
