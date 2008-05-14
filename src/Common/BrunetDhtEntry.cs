using System;
using System.Collections;
using System.Text;
using Brunet.DistributedServices;

namespace Fushare {
  /**
   * Represents the data entry from BrunetDht.
   * 
   * The reason BrunetDhtEntry is 
   * DictionaryData instead of DictionaryDataDecorator is that
   * the inner data is optional (shared with non-fushare users)
   */
  public class BrunetDhtEntry : DictionaryData {
    #region Fields
    private byte[] _key;
    private byte[] _value;
    private int _age;
    private int _ttl = 3600;  //1 hour
    #endregion

    #region Properties
    public byte[] Value {
      get {
        return _value;
      }
    }

    public string Base64Value {
      get {
        return Convert.ToBase64String(_value);
      }
    }

    public byte[] Key {
      get {
        return _key;
      }
    }

    public string KeyUTF8String {
      get {
        return Encoding.UTF8.GetString(_key);
      }
    }

    public int Age {
      get {
        return _age;
      }
    }

    public int Ttl {
      get {
        return _ttl;
      }
    }
    #endregion

    #region Constructors
    public BrunetDhtEntry(byte[] key, byte[] value, int age, int ttl)
      : this(key, value, ttl) {
      _age = age;
    }

    /**
     * Ctor that doesn't set age.
     */
    public BrunetDhtEntry(byte[] key, byte[] value, int ttl) {
      _key = key;
      _value = value;
      _ttl = ttl;
    }

    public BrunetDhtEntry(byte[] key, Brunet.DistributedServices.DhtGetResult dgr)
      : this(key, dgr.value, dgr.age, dgr.ttl) { }
    #endregion

    #region DictionaryData Members
    public override IDictionary ToDictionary() {
      IDictionary dict = InitIDictionaryFromType();
      dict.Add("key", _key);
      dict.Add("value", _value);
      dict.Add("age", _age);
      dict.Add("ttl", _ttl);
      return dict;
    }

    public override void FromDictionary(System.Collections.IDictionary dict) {
      _key = dict["key"] as byte[];
      _value = dict["value"] as byte[];
      _age = (int)dict["age"];
      _ttl = (int)dict["ttl"];
    }
    #endregion

    public DhtGetResult ToDhtGetResult() {
      return new DhtGetResult(Value, Age, Ttl);
    }
  }
}
