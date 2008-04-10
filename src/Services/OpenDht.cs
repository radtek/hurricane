using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using OpenDHTLib;

namespace Fushare.Services {
  /// <summary>
  /// An adapter class between fushare and OpenDHTLib.
  /// </summary>
  /// <remarks>Not to confuse with OpenDHT ("DHT" in uppercase) in namespace OpenDHTLib</remarks>
  public class OpenDht : DhtService {
    public enum PutResult {
      Success,
      OverCapacity,
      TryAgain
    }
    
    #region Fields
    private static IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(OpenDht));
    private OpenDHT _dht = new OpenDHT();
    /// <summary>
    /// In seconds
    /// </summary>
    public const int DefaultTtl = 3600;
    /// <summary>
    /// In bytes
    /// </summary>
    public const int MaxValueSize = 1024;
    #endregion

    #region DhtServices Members
    public override void Put(object key, object value) {
      throw new NotImplementedException();
    }

    public override object Get(object key) {
      throw new NotImplementedException();
    }
    #endregion

    public PutResult Put(byte[] key, byte[] value, int ttl) {
      return (PutResult)_dht.Put(key, value, ttl, "");
    }

    /// <summary>
    /// Returns an array of values (in byte[]) of given key
    /// </summary>
    public byte[][] Get(byte[] key) {
      return (byte[][])_dht.GetValues(key);
    }
  }
}
