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
  public class OpenDht : DhtBase {
    public enum PutResult {
      Success,
      OverCapacity,
      TryAgain
    }
    
    #region Fields
    private static IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(OpenDht));
    private int? _defaultTtl;
    private OpenDHT _dht = new OpenDHT();
    /// <summary>
    /// In seconds
    /// </summary>
    public const int DefaultOpenDhtTtl = 3600;
    /// <summary>
    /// In bytes
    /// </summary>
    public const int MaxValueSize = 1024;
    #endregion

    public override void Put(byte[] key, byte[] value, int ttl) {
      Convert.ToBoolean(_dht.Put(key, value, ttl, ""));
    }

    /// <summary>
    /// Returns an array of values (in byte[]) of given name
    /// </summary>
    public override DhtResults Get(byte[] key) {
      var values = _dht.GetValues(key);
      DhtResults results = new DhtResults();
      foreach (object value in values) {
        DhtResultEntry entry = new DhtResultEntry(value as byte[]);
        results.ResultEntries.Add(entry);
      }
      return results;
    }

    public override void Create(byte[] key, byte[] value, int ttl) {
      throw new NotImplementedException();
    }

    public override int DefaultTtl {
      get {
        if (!_defaultTtl.HasValue) {
          return DefaultOpenDhtTtl;
        } else {
          return _defaultTtl.Value;
        }
      }
      set {
        _defaultTtl = value;
      }
    }
  }
}
