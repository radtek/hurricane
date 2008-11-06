using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
#if FUSHARE_NUNIT
using NUnit.Framework; 
#endif

namespace Fushare {
  /**
   * Text or Binary data, without other type information registered in the system.
   * 
   * Detailed.
   */
  class RegularData : DictionaryData {
    public enum RegularDataType {
      Text,
      Binary
    }

    #region Fields
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(RegularData));
    private RegularDataType _type;
    private byte[] _payload; 

    #endregion

    #region Properties
    public byte[] PayLoad {
      get {
        return _payload;
      }
    }

    public RegularDataType DataType {
      get {
        return _type;
      }
    } 
    #endregion

    #region Constructors
    public RegularData() : base() { }

    public RegularData(byte[] payload) {
      _payload = payload;
    }
    #endregion

    #region DictionaryData Members 

    public override IDictionary ToDictionary() {
      IDictionary dict = InitIDictionaryFromType();
      dict.Add("type", (int)_type);
      dict.Add("payload", _payload);
      return dict;
    }

    public override void FromDictionary(IDictionary dict) {
      _type = (RegularDataType)dict["type"];
      _payload = dict["payload"] as byte[];
    }

    #endregion
  }


#if FUSHARE_NUNIT
  [TestFixture]
  public class RegularDataTest : FushareTestBase {
    [Test]
    public void TestSerializeAndDeserialize() {
      string str_to_compare = "Test String 1";
      byte[] b = Encoding.UTF8.GetBytes(str_to_compare);
      RegularData d = new RegularData(b);
      Brunet.MemBlock mb = Brunet.MemBlock.Reference(d.SerializeTo());
      Logger.WriteLineIf(LogLevel.Verbose, _log_props, mb.GetString(Encoding.UTF8));
      RegularData d_actual = (RegularData)DictionaryData.CreateDictionaryData(mb, new AdrSerializer());
      Assert.AreEqual(str_to_compare, Encoding.UTF8.GetString(d_actual.PayLoad));
    }
  }
#endif
}
