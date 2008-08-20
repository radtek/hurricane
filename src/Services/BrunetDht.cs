using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Threading;
#if FUSHARE_NUNIT
using NUnit.Framework; 
#endif
using Brunet.DistributedServices;
using Brunet;
using Brunet.Rpc;

namespace Fushare.Services {
  /**
   * Derives the the DhtService with the Implementation against Brunet Dht.
   * 
   */
  public partial class BrunetDht : DhtService {
    private IXmlRpcDht _dht;
    private static IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(BrunetDht));
    public const int DefaultTtl = 3600;  //Default ttl : 1 hour
    /// <summary>
    /// Max piece size in bytes that is allowed in Brunet. 
    /// </summary>
    public const int BrunetMaxPieceSize = 900;
    private Uri _svc_uri;

    #region Constructors
    public BrunetDht() { }

    internal BrunetDht(IXmlRpcDht dht) {
      dht.Timeout = 360000; // 3 minutes
      _dht = dht;
    }

    public BrunetDht(Uri uri)
      : this(DhtServiceClient.GetXmlRpcDhtClient(uri.Port)) {
      this._svc_uri = uri;
      new XmlRpcTracer().Attach(_dht as CookComputing.XmlRpc.IXmlRpcProxy);
    }

    #endregion

    #region IDictionaryService Members

    public override void Put(object key, object value) {
      _dht.Put(key, value, DefaultTtl);
    }

    /// <summary>
    /// Returns result from IDht
    /// </summary>
    /// <param name="key">string or byte[] typed key</param>
    /// <returns>Values in <c>DhtGetResult[]</c></returns>
    /// <remarks>string typed key is converted to byte[] using UTF-8 on the receiver</remarks>
    public override object Get(object key) {
      DhtGetResult[] dgrs = _dht.Get(key);
      return dgrs;
    }

    #endregion

    /// <summary>
    /// Puts key/value pair into Brunet DHT
    /// </summary>
    /// <param name="ttl">Time-To-Live in seconds</param>
    /// <returns>true if successful, false if not</returns>
    public bool Put(byte[] key, byte[]value, int ttl) {
      return Put(key, value, ttl, BrunetMaxPieceSize, true);
    }

    /// <summary>
    /// Same as Put except that it fails in case of key collision
    /// </summary>
    /// <param name="ttl">Time-To-Live in seconds</param>
    /// <returns>true if successful, false if not</returns>
    public bool Create(byte[] key, byte[] value, int ttl) {
      return _dht.Create(key, value, ttl);
    }

    /// <summary>
    /// Put with a size limit. Data larger than the limit will be splitted into
    /// pieces and put to different under different DHT keys.
    /// </summary>
    /// <param name="maxSize">The size limit</param>
    /// <param name="split">Whether to split the oversized data</param>
    /// <exception cref="ArgumentException">Data larger than the limit and 
    /// split specified as false</exception>
    public bool Put(byte[] key, byte[] value, int ttl, int maxSize, bool split) {
      bool result;
      if (value.Length > maxSize) {
        if (split) {
          BrunetDhtEntry bde = new BrunetDhtEntry(key, value, ttl);
          FragmentationInfo frag_info = new FragmentationInfo(key);
          frag_info.PieceLength = maxSize;
          result = PutFragments(bde, frag_info);
        } else {
          throw new ArgumentException("Data too large but split opted out.");
        }
      } else {
        result = _dht.Put(key, value, ttl);
      }

      return result;
    }

    public enum OneDatumMode {
      FirstOne,
      LastOne,
      NewestOne,
      LengthiestOne
    }

    public DhtGetResult GetOneDatum(byte[] key, bool getPieces) {
      return GetOneDatum(key, getPieces, OneDatumMode.LastOne);
    }

    /// <summary>
    /// Gets the first data item of the given key and possibly gets the 
    /// indicated pieces from DHT.
    /// </summary>
    /// <param name="getPieces">Whether to get pieces if the data at the 
    /// specified DHT key is a FragmentationInfo</param>
    public DhtGetResult GetOneDatum(byte[] key, bool getPieces, 
      OneDatumMode mode) {
      DhtGetResult[] results =  _dht.Get(key);
      DhtGetResult ret;
      if (results.Length == 0) {
        ret = null;
      } else {
        DhtGetResult dgr;
        if (mode == OneDatumMode.FirstOne) {
          dgr = results[0];
        } else if (mode == OneDatumMode.LastOne) {
          dgr = results[results.Length - 1];
        } else {
          throw new NotImplementedException(
            "This OneDatumMode not implemented.");
        }
        DictionaryData dd = null;
        try {
          dd = DictionaryData.CreateDictionaryData(dgr.value);
        } catch (Exception ex) {
          // Not an error in this case. Log with verbose level.
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              ex);
        }
        if (dd != null && dd is FragmentationInfo) {
          FragmentationInfo frag_info = dd as FragmentationInfo;
          BrunetDhtEntry bde = null;
          try {
            bde = GetFragments(frag_info) as BrunetDhtEntry;
            RegularData rd = (RegularData)DictionaryData.CreateDictionaryData(
              bde.Value);
            //Only 1 entry (if any) in this array
            ret = new DhtGetResult(rd.PayLoad, bde.Age, bde.Ttl);
          } catch (Exception ex) {
            Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format("Can't get fragments."), ex);
            ret = null;
          }
        } else {
          ret = dgr;
        }
      }
      return ret;
    }

    public DictionaryData GetFragments(FragmentationInfo info) {
      return this.GetFragments(info, true);
    }

    /// <exception cref="Exception">Operation Failed</exception>
    /// <returns>A BrunetDhtEntry that has the whole chunk of data</returns>
    public DictionaryData GetFragments(FragmentationInfo info, bool concurrently) {
      BrunetDhtEntry ret = null;
      byte[] base_key = info.BaseKey;
      int piece_num = info.PieceNum;
      MemBlock fragments;
      int largest_age;  
      int smallest_ttl;

      /* 
       * @TODO: It doesn't have to be sequential but let's first get it work with
       * this approach 
       */
      
#if FUSHARE_PF
      DateTime get_started = DateTime.UtcNow; 
#endif
      if (!concurrently) {
        fragments = GetFragsSequentially(base_key, piece_num, out largest_age, out smallest_ttl);
      } else {
        //fragments = GetFragsConcurrently(base_key, piece_num, out largest_age, out smallest_ttl);
        fragments = GetFragsInBulk(base_key, piece_num, out largest_age, out smallest_ttl);
      }

#if FUSHARE_PF
      DateTime get_finished = DateTime.UtcNow;
      TimeSpan get_time = get_finished - get_started;
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Total time used to get {0}: {1} seconds", 
        Encoding.UTF8.GetString(base_key), get_time)); 
#endif
      if (fragments != null) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Fragments successfully got from DHT"));

        //Got the fragments correctly
        ret = new BrunetDhtEntry(base_key, (new RegularData(fragments)).SerializeTo(),
            largest_age, smallest_ttl);
        return ret;
      } else {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
          string.Format("DHT Get of fragments failed"));
        throw new Exception("DHT Get of fragments failed.");
      }
    }

    /// <returns>The memblock assembled from pieces. Null if operation failed.</returns>
    private MemBlock GetFragsSequentially(byte[] base_key, int piece_num, 
      out int largest_age, out int smallest_ttl) {
      largest_age = 0;
      smallest_ttl = Int32.MaxValue;
      MemBlock fragments = new MemBlock();
      for (int i = 0; i < piece_num; i++) {
        byte[] piece_key = BuildFragmentKey(base_key, i);
        bool succ = false; //set to false iff bad things happen
        int retries = 3;  //After that we fail the operation
        for (; !succ && retries > 0; retries--) {
          if (retries == 3) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                string.Format("Getting: {0}", piece_key));
          } else {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                string.Format("Retrying..."));
          }
          try {
            DhtGetResult[] dgrs = _dht.Get(piece_key);
            //It should have only one entry. If not, just let the the exception caught
            //and retry.
            DhtGetResult dgr = dgrs[0];
            FingerprintedData fpd =
                (FingerprintedData)DictionaryData.CreateDictionaryData(dgr.value);
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                string.Format("Piece {0} retrieved and successfully parsed", piece_key));
            RegularData rd = fpd.InnerData as RegularData;
            fragments = MemBlock.Concat(fragments, MemBlock.Reference(rd.PayLoad));
            if (smallest_ttl > dgr.ttl)
              smallest_ttl = dgr.ttl;
            if (largest_age < dgr.age)
              largest_age = dgr.age;
            //Now it's safe to say, this attempt succeeded.
            succ = true;
          } catch (Exception ex) {
            Logger.WriteLineIf(LogLevel.Error, _log_props,
                ex);
            succ = false;
          }
        } //if succ then we are good

        if (retries <= 0) {
          //Quit because retries exhausted.
          throw new Exception(string.Format("Retries exhausted when retrieving "
              + "and deserializing piece : {0}", piece_key));
        } else {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
              "Done with piece {0}", 
              Encoding.UTF8.GetString(piece_key)));
        }
      }
      return fragments;
    }

    /// <summary>
    /// Puts fragments and chooses the concurrent option
    /// </summary>
    public bool PutFragments(BrunetDhtEntry bde, FragmentationInfo fragInfo) {
      return this.PutFragments(bde, fragInfo, true);
    }

    /// <summary>
    /// Puts fragments to Dht.
    /// </summary>
    /// <param name="bde">The data object.</param>
    /// <param name="fragInfo">Object that contains the meta info.</param>
    /// <param name="concurrently">Puts concurrently if set to true</param>
    /// <returns>True if the operation succeeds.</returns>
    public bool PutFragments(BrunetDhtEntry bde, FragmentationInfo fragInfo, 
      bool concurrently) {
      byte[] info_key = bde.Key;
      int ttl = bde.Ttl;  //This ttl is used by every piece and the frag info
      MemBlock data = MemBlock.Reference(bde.Value);
      int piece_length = 0;
      int offset = 0;
      IList<FingerprintedData> fragments = new List<FingerprintedData>();
      while (offset < data.Length) {
        piece_length = (offset + fragInfo.PieceLength > data.Length) ?
            data.Length - offset : fragInfo.PieceLength;
        MemBlock piece = data.Slice(offset, piece_length);
        offset += piece_length;
        FingerprintedData fpd = new FingerprintedData(new RegularData(piece));
        fragments.Add(fpd);
      }

      //Update the piece number in fragInfo.
      Logger.WriteLineIf(LogLevel.Info, _log_props,
          string.Format("Data fragmented into {0} pieces", fragments.Count));
      fragInfo.PieceNum = fragments.Count;

#if FUSHARE_PF
      DateTime put_started = DateTime.UtcNow;
#endif
      bool succ;
      if (!concurrently) {
        succ = PutFragsSequentially(fragInfo, info_key, ttl, fragments);
      } else {
        //succ  = PutFragsConcurrently(fragInfo, info_key, ttl, fragments);
        succ = PutFragsInBulk(fragInfo, info_key, ttl, fragments);
      }

#if FUSHARE_PF
      DateTime put_finished = DateTime.UtcNow;
      TimeSpan put_time = put_finished - put_started;
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Total time used to put {0}: {1} seconds", 
        Encoding.UTF8.GetString(fragInfo.BaseKey), put_time));
#endif

      return succ;
    }

    private bool PutFragsSequentially(FragmentationInfo fragInfo, 
        byte[] infoKey, int ttl, IList<FingerprintedData> fragments) {
      string info_key_string = Encoding.UTF8.GetString(infoKey);
      //Put pieces
      int index = 0;
      foreach (FingerprintedData fpd in fragments) {
        byte[] serializedFpd = fpd.SerializeTo();
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Size after serialization {0}", serializedFpd.Length));
        int i = index++;
        bool succ = false;
        int retries = 3;
        byte[] piece_key = BuildFragmentKey(fragInfo.BaseKey, i);
        string piece_key_string = Encoding.UTF8.GetString(piece_key);
        for (; !succ && retries > 0; retries--) {
          if (retries < 3) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                string.Format("Retrying..."));
          }

          succ = _dht.Put(piece_key, serializedFpd, ttl);
          if (!succ) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                      string.Format("Put failed."));
          }
        }

        if (retries <= 0) {
          Logger.WriteLineIf(LogLevel.Error, _log_props,
              string.Format("Retries exhausted when putting piece : {0}", 
              piece_key_string));
          return false;
        } else {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("Piece {0} successfully put to DHT", piece_key_string));
        }
      }

      //Put info
      bool succ_info = false;
      int retries_info = 3;
      for (; !succ_info && retries_info > 0; retries_info--) {
        if (retries_info < 3) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("Retrying..."));
        }
        succ_info = _dht.Put(infoKey, fragInfo.SerializeTo(), ttl);
      }

      if (retries_info <= 0) {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
            string.Format("Retries exhausted when putting FragmentationInfo : {0}",
            info_key_string));
        return false;
      } else {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("FragmentationInfo successfully put to DHT with key {0}",
            info_key_string));
      }
      return true;
    }

    public static string BuildFragmentKey(string baseKey, int index) {
      return baseKey + ":" + index.ToString();
    }

    public static byte[] BuildFragmentKey(byte[] baseKey, int index) {
      return Encoding.UTF8.GetBytes(BuildFragmentKey(Encoding.UTF8.GetString(baseKey), index));
    }

    public static int GetPieceIndexFromFragmentKey(byte[] fragKey) {
      string keyStr = Encoding.UTF8.GetString(fragKey);
      int commaIndx = keyStr.LastIndexOf(':');
      string indexStr = keyStr.Substring(commaIndx + 1);
      return Int32.Parse(indexStr);
    }

  }


  public class MockBrunetDht : IDht {
    private Hashtable _ht = new Hashtable();

    public Hashtable HTStorage {
      get {
        return _ht;
      }
      set {
        _ht = value;
      }
    }

    #region IDht Members

    public string BeginGet(object key) {
      throw new NotImplementedException();
    }

    public bool Create(object key, object value, int ttl) {
      throw new NotImplementedException();
    }

    public DhtGetResult[] Get(object key) {
      int age = 1000;
      return new DhtGetResult[] { new DhtGetResult(_ht[key] as string, age) };
    }

    public bool Put(object key, object value, int ttl) {
      _ht.Add(key, value);
      return true;
    }

    public DhtGetResult ContinueGet(string token) {
      throw new NotImplementedException();
    }

    public void EndGet(string token) {
      throw new NotImplementedException();
    }

    public IDictionary GetDhtInfo() {
      throw new NotImplementedException();
    }

    public string BeginPut(object key, object value, int ttl) {
      throw new NotImplementedException();
    }

    public bool EndPut(string token) {
      throw new NotImplementedException();
    }

    #endregion
  }

#if FUSHARE_NUNIT
  [TestFixture]
  public class BrunetDhtTest : FushareTestBase {
    private byte[] MakeByteArray(int size) {
      byte[] ret = new byte[size];
      Random rnd = new Random();
      rnd.NextBytes(ret);
      return ret;
    }

    [Test]
    public void TestPutAndGetFragments() {
      MockBrunetDht mock_dht = new MockBrunetDht();
      BrunetDht dht = new BrunetDht(mock_dht);
      FragmentationInfo frag_info = new FragmentationInfo();
      byte[] b_key = MakeByteArray(20);
      string key = Encoding.UTF8.GetString(b_key);
      frag_info.BaseKey = Encoding.UTF8.GetString(b_key);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("key : {0}", Base32.Encode(b_key)));
      byte[] expected = MakeByteArray(1024 * 10);
      int ttl = 1000;
      BrunetDhtEntry bde = new BrunetDhtEntry(key, expected, ttl);
      dht.PutFragments(bde, frag_info);
      Hashtable ht = mock_dht.HTStorage;
      Assert.AreEqual(11, ht.Count);
      ////
      byte[] serialized_fragInfo = ((DhtGetResult[])dht.Get(key))[0].value;
      FragmentationInfo frag_info_actual =
        DictionaryData.CreateDictionaryData(serialized_fragInfo) as FragmentationInfo;
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("BaseKey: {0}", Base32.Encode(Encoding.UTF8.GetBytes(frag_info_actual.BaseKey))));
      BrunetDhtEntry actual = dht.GetFragments(frag_info) as BrunetDhtEntry;
      MemBlock mb = MemBlock.Reference(actual.Value);
      MemBlock mb_exp = MemBlock.Reference(expected);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          mb.ToBase32String().Substring(0, 50));
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          mb_exp.ToBase32String().Substring(0, 50));
      Assert.AreEqual(mb_exp, mb);
    }
  } 
#endif
}
