using System;
using System.Collections.Specialized;
using System.Collections;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
#if FUSHARE_NUNIT
using NUnit.Framework; 
#endif

namespace GSeries {
  /**
   * Wrapper of regular data which indicates that this data is only
   * part of a larger datum.
   */
  public class FingerprintedData : DictionaryDataDecorator {
    #region Fields
    private byte[] _sha1;
    #endregion

    public FingerprintedData() : base() { }

    public FingerprintedData(DictionaryData inner) : base(inner) { }

    /**
     * The fileds
     */
    public override IDictionary ToDictionary() {
      //type info and inner data added
      IDictionary dict = this.InitIDictionaryWithInnerData();
      //add sha1 of inner_data integrity check
      using (SHA1 hash = new SHA1CryptoServiceProvider()) {
        dict.Add("sha1", hash.ComputeHash(_inner_data.SerializeTo()));
      }
      return dict;
    }

    public override void FromDictionary(IDictionary dict) {
      this.FillInnerData(dict);
      _sha1 = (byte[])dict["sha1"];
    }

    /**
     * @exception Exception Thrown if the sha1 hash of "data" doesn't match "sha1"
     */
    public override void DeserializeFrom(byte[] serializedBytes, ISerializer serializer) {
      base.DeserializeFrom(serializedBytes, serializer);
      byte[] sha1_of_inner = null;
      using (SHA1 hash = new SHA1CryptoServiceProvider()) {
        sha1_of_inner = hash.ComputeHash(_inner_data.SerializeTo());
      }

      if (sha1_of_inner.SequenceEqual(_sha1))
        throw new Exception("Data lost might have happened on the wire");
      //if euqals, we are good to go
    }
  }

#if FUSHARE_NUNIT
  [TestFixture]
  public class DataWithFingerprintTest : FushareTestBase {
    [Test]
    public void TestSerializeAndDeserialize() {
      string str_to_check = "Test String 1";
      RegularData d = new RegularData(Encoding.UTF8.GetBytes(str_to_check));
      FingerprintedData p = new FingerprintedData(d);
      byte[] serializedObj = p.SerializeTo();
      Logger.WriteLineIf(LogLevel.Verbose, _log_props, Encoding.UTF8.GetString(serializedObj));
      FingerprintedData actual_p = (FingerprintedData)DictionaryData.CreateDictionaryData(serializedObj, new AdrSerializer());
      RegularData actual_d = (RegularData)actual_p.InnerData;
      Assert.AreEqual(str_to_check, Encoding.UTF8.GetString(actual_d.PayLoad));
    }

    [Test]
    [ExpectedException(typeof(Exception))]
    public void TestSerializeAndDeserializeWithDataLoss() {
      string str_to_check = "Test String2";
      RegularData d = new RegularData(Encoding.UTF8.GetBytes(str_to_check));
      FingerprintedData p = new FingerprintedData(d);
      byte[] serializedObj = p.SerializeTo();
      string serializedObj_string = Encoding.UTF8.GetString(serializedObj);
      serializedObj_string = serializedObj_string.Replace("Test String2", "Test String3");
      Logger.WriteLineIf(LogLevel.Verbose, _log_props, Encoding.UTF8.GetString(serializedObj));
      Logger.WriteLineIf(LogLevel.Verbose, _log_props, serializedObj_string);
      FingerprintedData actual_p = (FingerprintedData)DictionaryData.CreateDictionaryData(Encoding.UTF8.GetBytes(serializedObj_string), new AdrSerializer());
      RegularData actual_d = (RegularData)actual_p.InnerData;
      Assert.AreEqual(str_to_check, Encoding.UTF8.GetString(actual_d.PayLoad));
    }
  } 
#endif
}
