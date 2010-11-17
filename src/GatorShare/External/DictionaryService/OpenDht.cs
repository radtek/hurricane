/*
Copyright (c) 2010 Jiangyan Xu <jiangyan@ufl.edu>, University of Florida

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using OpenDHTLib;

namespace GatorShare.External.DictionaryService {
  /// <summary>
  /// An adapter class between fushare and OpenDHTLib.
  /// </summary>
  /// <remarks>Not to confuse with OpenDHT ("DHT" in uppercase) in namespace OpenDHTLib</remarks>
  public class OpenDht : DictionaryServiceBase {
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
    public override DictionaryServiceData Get(byte[] key) {
      var values = _dht.GetValues(key);
      DictionaryServiceData results = new DictionaryServiceData();
      foreach (object value in values) {
        DictionaryServiceDataEntry entry = new DictionaryServiceDataEntry(value as byte[]);
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
