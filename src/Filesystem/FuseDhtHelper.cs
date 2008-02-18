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
using Fushare.Services;
#if FUSE_NUNIT
using NUnit.Framework;
#endif

namespace Fushare.Filesystem {  
  /// <summary>
  /// Deal with Dht operations for FuseDht class
  /// </summary>
  public class FuseDhtHelper {

    #region Fields
    public const int DhtPutRetryTimes = 3;
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FuseDhtHelper));
    private BrunetDht _dht;
    private string _shadowdir;
    private readonly string _metadir;
    private readonly string _dht_addr;
    private readonly string _ipop_ns;
    private IXmlRpcManager _rpc; 
    #endregion

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
    
    public FuseDhtHelper(int xmlRpcPort, string shadowdir) {
      _dht = (BrunetDht)DictionaryServiceFactory.GetServiceInstance(typeof(BrunetDht));
      this._shadowdir = shadowdir;
      this._metadir = Path.Combine(Path.Combine(_shadowdir, Constants.DIR_DHT_ROOT), Constants.DIR_META);
      try {
        this._rpc = XmlRpcManagerClient.GetXmlRpcManager(xmlRpcPort);
        object rs = _rpc.localproxy("ipop.Information");
        if (rs != null) {
          IDictionary dic = (IDictionary)rs;
          _ipop_ns = dic["ipop_namespace"] as string;
          _dht_addr = ((IDictionary)dic["neighbors"])["self"] as string;
        } else {
          _ipop_ns = string.Empty;
        }
      } catch (Exception e) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props, e);
        _ipop_ns = string.Empty;
      } finally {
        Logger.WriteLineIf(LogLevel.Info, _log_props,
            string.Format("IPOP Namespace: {0}", _ipop_ns));
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
     * This method uses the ISoapDht only BlockingQueue feature.
     * So if other interfaces (XmlRpc) are used here, a casting exception will be thrown.
     * @deprecated Since now I switched to XmlRpc approach
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
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("Getting {0}", dht_key));
      IBlockingQueue bq = dht.GetAsBlockingQueue(dht_key);
      int i = 0;
      bool set = false;
      DateTime dt = DateTime.MinValue;
      while (true) {
        // Still a chance for Dequeue to execute on an empty closed queue 
        // so we'll do this instead.
        try {
          DhtGetResult result = (DhtGetResult)bq.Dequeue();
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("Got #{0} item", i++));
          DhtDataFile file = new DhtDataFile(s_parent_path, result);
          file.WriteToFile();
          DateTime end = DateTime.UtcNow - new TimeSpan(0, 0, file.Age) + new TimeSpan(0, 0, file.TTL);
          if (dt == DateTime.MinValue || end < dt) {
            //if hasn't been set, set it. Otherwise only set if we can get a smaller datetime
            dt = end;
          }
          if(waitingFileName != null && file.Name.Equals(waitingFileName)) {
            //notify the waiting thread that the expected file arrives
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("Got the expected file"));
            re.Set();
            set = true;
          }
        } catch (Exception e) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,e);
          break;
        }
      }
      //set again in case no such filename in Dht
      File.WriteAllText(Path.Combine(s_parent_path, Constants.FILE_DONE), "1"); //done
      string refresh = Path.Combine(new DirectoryInfo(s_parent_path).Parent.GetDirectories(Constants.DIR_ETC)[0].FullName, 
        Constants.FILE_REFRESH);
      //we use utc to compare, but we write local time string for easy to read
      File.WriteAllText(refresh, dt.ToLocalTime().ToString());
      if (!set) {
        //no filename matched. So I release the waiting thread at the end
        re.Set(); 
      }
    }

    /**
     * A synchronous way of accessing DHT. The method gets blocked until all the wanted results
     * returned or DHT-GET failed, in which case there is no result returned.
     */
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

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("Getting {0}", dht_key));
      DhtGetResult[] results = _dht.Get(dht_key);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("Got {0} item(s)", results.Length));

      #region Fragmentation logic
      //Check the first value for frag info
      DhtGetResult dgr = results[0];
      DictionaryData dd = null;
      try {
         //dd = DictionaryData.CreateDictionaryData(dgr.value);
         dd = DictionaryData.CreateDictionaryData(Convert.FromBase64String(dgr.valueString));
      } catch (Exception ex) {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
            ex);
        throw ex;
      }
      if (dd is FragmentationInfo) {
        FragmentationInfo frag_info = dd as FragmentationInfo;
        Logger.WriteLineIf(LogLevel.Info, _log_props,
            string.Format("Retrieved data is a FragmentationInfo: {0}", frag_info.ToString()));
        BrunetDhtEntry bde = _dht.GetFragments(frag_info) as BrunetDhtEntry;
        results = new DhtGetResult[] { bde.ToDhtGetResult() };
      }

      #endregion

      //We need the earliest expiration time of all the returned items
      DateTime dt = DateTime.MinValue;
      foreach (DhtGetResult result in results) {
        DhtDataFile file = new DhtDataFile(s_parent_path, result);
        file.WriteToFile();
        DateTime end = DateTime.UtcNow - new TimeSpan(0, 0, file.Age) + new TimeSpan(0, 0, file.TTL);
        if (dt == DateTime.MinValue || end < dt) {
          //if hasn't been set, set it. Otherwise only set if we can get a smaller datetime
          dt = end;
        }
      }

      //set again in case no such filename in Dht
      File.WriteAllText(Path.Combine(s_parent_path, Constants.FILE_DONE), "1"); //done
      string refresh_path = Path.Combine(new DirectoryInfo(s_parent_path).Parent.GetDirectories(Constants.DIR_ETC)[0].FullName,
        Constants.FILE_REFRESH);
      //We use utc to compare, but write local time string for easy to read
      File.WriteAllText(refresh_path, dt.ToLocalTime().ToString());
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
      byte[] value = (byte[])state[3];
      int ttl = (int)state[4];
      string s_file_path = state[5] as string;
      PutMode put_mode = (PutMode)state[6];

      bool result;

      for (int i = 0; i < DhtPutRetryTimes; i++) {

        #region Fragmentation logic
        //Check the length of the value. If too large, fragment it.
        int size_limit = 600;  //bytes
        if (value.Length > size_limit) {
          BrunetDhtEntry bde = new BrunetDhtEntry(dht_key, value, ttl);
          FragmentationInfo frag_info = new FragmentationInfo(dht_key);
          frag_info.PieceLength = size_limit;
          result = _dht.PutFragments(bde, frag_info);
        }  
        #endregion

        else {
          string value_string = Encoding.UTF8.GetString(value);
          if (put_mode == PutMode.Create) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format("Creating {0}, {1}", dht_key, new FileInfo(s_file_path).Name));
            result = _dht.Create(dht_key, value_string, ttl);
          } else {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format("Putting {0}, {1}", dht_key, new FileInfo(s_file_path).Name));
            result = _dht.Put(dht_key, value_string, ttl);
          } 
        }
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("Put/Create returned: {0}", result));

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
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("Dropping file {0} to meta folder. {1}", file._meta_filename, DateTime.Now));
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
