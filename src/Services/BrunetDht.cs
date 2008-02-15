using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Ipop;
#if FUSHARE_NUNIT
using NUnit.Framework; 
#endif
using Brunet.Dht;
using Brunet;

namespace Fushare.Services {
  /**
   * Derives the the DhtService with the Implementation against Brunet Dht.
   * 
   */
  public class BrunetDht : DhtService {
    private IDht _dht;
    private static IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(BrunetDht));
    public const int DefaultTtl = 3600;  //Default ttl : 1 hour

    #region Constructors
    public BrunetDht() { }

    internal BrunetDht(IDht dht) {
      _dht = dht;
    }

    public BrunetDht(Uri uri) {
      _dht = Ipop.DhtServiceClient.GetXmlRpcDhtClient(uri.Port);
    }

    #endregion

    #region IDictionaryService Members

    public override void Put(object key, object value) {
      string str_key = key as string;
      string str_value = value.ToString();
      _dht.Put(str_key, str_value, DefaultTtl);
    }

    public override object Get(object key) {
      DhtGetResult[] dgrs = Get(key as string);
      return dgrs;
    }

    #endregion

    public DhtGetResult[] Get(string key) {
      return _dht.Get(key);
    }

    public bool Put(string key, string value, int ttl) {
      return _dht.Put(key, value, ttl);
    }

    public bool Create(string key, string value, int ttl) {
      return _dht.Create(key, value, ttl);
    }

    /**
     * Note: binary data are passed to Brunet Dht as Base64 string
     */
    public DictionaryData GetFragments(FragmentationInfo info) {
      BrunetDhtEntry ret = null;
      string base_key = info.BaseKey;
      int piece_num = info.PieceNum;
      MemBlock fragments = new MemBlock();
      int largest_age = 0;  
      int smallest_ttl = Int32.MaxValue;
      int retries = 3;  //After that we fail the operation
      /* 
       * @TODO: It doesn't have to be sequential but let's first get it work with
       * this approach 
       */
      for (int i = 0; i < piece_num; i++) {
        string key = BuildFragmentKey(base_key, i);
        bool succ = true; //set to false iff bad things happen
        do {
          DhtGetResult[] dgrs = _dht.Get(key);
          try {
            //It should have only one entry. If not, just let the the exception caught
            //and retry.
            DhtGetResult dgr = dgrs[0];
            FingerprintedData fpd =
                (FingerprintedData)DictionaryData.CreateDictionaryData(Convert.FromBase64String(dgr.valueString));
            RegularData rd = fpd.InnerData as RegularData;
            fragments = MemBlock.Concat(fragments, MemBlock.Reference(rd.PayLoad));
            if (smallest_ttl > dgr.ttl)
              smallest_ttl = dgr.ttl;
            if (largest_age < dgr.age)
              largest_age = dgr.age;
          } catch (Exception ex) {
            Logger.WriteLineIf(LogLevel.Error, _log_props,
                ex);
            succ = false;
          }
        } while (!succ && retries-- > 0);  //if succ then we are good
        if (retries == 0) {
          //Quit because retries exhausted.
          throw new Exception(string.Format("Retries exhausted when retrieving" +
              "and deserializing piece : {0}", key));
        }
      }
      //Got the fragments correctly
      ret = new BrunetDhtEntry(base_key, fragments,
          largest_age, smallest_ttl);
      return ret;
    }

    /**
     * Note: binary data are parsed as Base64 string from Brunet Dht
     */
    public bool PutFragments(BrunetDhtEntry bde, FragmentationInfo fragInfo) {
      string info_key = bde.Key;
      int ttl = bde.Ttl;
      MemBlock data = MemBlock.Reference(bde.Value);
      string base_key =  fragInfo.BaseKey;
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
      fragInfo.PieceNum = fragments.Count;

      //Put pieces
      int index = 0;
      foreach (FingerprintedData fpd in fragments) {
        byte[] serializedFpd = fpd.SerializeTo();
        int i = index++;
        bool succ = true;
        int retries = 3;
        string piece_key = BuildFragmentKey(base_key, i);
        do {
          succ = _dht.Put(piece_key,
              Convert.ToBase64String(serializedFpd), ttl);
        } while (!succ && retries-- > 0);
        if (retries == 0) {
          Logger.WriteLineIf(LogLevel.Error, _log_props,
              string.Format("Retries exhausted when putting piece : {0}", piece_key));
          return false;
        }
      }

      //Put info
      bool succ_info = true;
      int retries_info = 3;
      do {
        succ_info = _dht.Put(info_key, Encoding.UTF8.GetString(fragInfo.SerializeTo()), ttl);
      } while (!succ_info && retries_info-- > 0) ;
      if (retries_info == 0) {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
              string.Format("Retries exhausted when putting fragInfo : {0}", info_key));
        return false;
      }
      //If code reaches here without problems, then we can call it successful.
      return true;
    }

    private string BuildFragmentKey(string baseKey, int index) {
      return baseKey + ":" + index.ToString();
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

    public string BeginGet(string key) {
      throw new Exception("The method or operation is not implemented.");
    }

    public DhtGetResult ContinueGet(string token) {
      throw new Exception("The method or operation is not implemented.");
    }

    public bool Create(string key, string value, int ttl) {
      throw new Exception("The method or operation is not implemented.");
    }

    public void EndGet(string token) {
      throw new Exception("The method or operation is not implemented.");
    }

    public DhtGetResult[] Get(string key) {
      int age = 1000;
      return new DhtGetResult[] { new DhtGetResult(_ht[key] as string, age) };
    }

    public IDictionary GetDhtInfo() {
      throw new Exception("The method or operation is not implemented.");
    }

    public bool Put(string key, string value, int ttl) {
      _ht.Add(key, value);
      return true;
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
