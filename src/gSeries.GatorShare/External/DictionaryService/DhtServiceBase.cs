using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GSeries.External.DictionaryService {
  /// <summary>
  /// DHT is a special type of dictionary service. It has a TTL (Time-to-live)
  /// value associated with value entries.
  /// </summary>
  public abstract class DhtServiceBase : DictionaryServiceBase {
    public abstract void Put(byte[] key, byte[] value, int ttl);
    public abstract void Create(byte[] key, byte[] value, int ttl);
  }
}
