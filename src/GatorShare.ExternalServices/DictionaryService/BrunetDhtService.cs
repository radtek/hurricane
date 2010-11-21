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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Brunet.Services.XmlRpc;
using System.Collections;
using CookComputing.XmlRpc;

namespace GatorShare.External.DictionaryService {
  /// <summary>
  /// Implementation of dictionary service using Brunet DHT.
  /// </summary>
  public class BrunetDhtService : DhtServiceBase {
    /// <summary>
    /// TTL in seconds.
    /// Default to one week. 
    /// </summary>
    readonly int _defaultTtl = 60 * 60 * 24 * 7;
    readonly IXmlRpcManager _dht;

    public BrunetDhtService(string host, int port, string relativeUri) {
      _dht = XmlRpcManagerClient.GetXmlRpcManager(host, port, relativeUri, false);
    }


    #region DhtServiceBase Methods
    public override void Put(byte[] key, byte[] value, int ttl) {
      try {
        bool succ = (bool)_dht.localproxy("DhtClient.Put", key, value, ttl);
        if (!succ) {
          throw new DictionaryServiceException("Put operation returned unsuccessful.") {
            DictionaryKey = key
          };
        }
      } catch (XmlRpcException ex) {
        throw new DictionaryServiceException("Put operation failed.", ex) {
          DictionaryKey = key
        };
      }
    }

    public override void Create(byte[] key, byte[] value, int ttl) {
      try {
        bool succ = (bool)_dht.localproxy("DhtClient.Create", key, value, ttl);
        if (!succ) {
          throw new DictionaryKeyException() { DictionaryKey = key };
        }
      } catch (XmlRpcFaultException ex) {
        throw new DictionaryKeyException(
          "Cannot create such a key because it already exists.", ex) { 
          DictionaryKey = key };
      } catch (XmlRpcException ex) {
        throw new DictionaryServiceException("Error while accessing DHT.", ex) { 
          DictionaryKey = key };
      }
    }

    public override DictionaryServiceData Get(byte[] key) {
      var data = _dht.localproxy("DhtClient.Get", key) as object[];
      if (data == null || data.Length == 0) {
        throw new DictionaryKeyNotFoundException() { DictionaryKey = key } ;
      }
      var hts = Array.ConvertAll<object, Hashtable>(data, x => (Hashtable)x);
      return new BrunetDhtServiceData(key, hts);
    }

    /// <summary>
    /// Puts the value to DHT and keeps renewing it.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public override void Put(byte[] key, byte[] value) {
      try {
        bool succ = (bool)_dht.localproxy("RpcDhtProxy.Register", key, value, _defaultTtl);
        if (!succ) {
          throw new DictionaryServiceException("Put operation returned unsuccessful.") { 
            DictionaryKey = key };
        }
      } catch (XmlRpcException ex) {
        throw new DictionaryServiceException("Put operation failed.", ex) { 
          DictionaryKey = key } ;
      }
    }


    /// <summary>
    /// Creates the value and keeps renewing it.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="DictionaryKeyException">Thrown when key already exists.</exception>
    public override void Create(byte[] key, byte[] value) {
      // Create for 1 minute and then register for put.
      Create(key, value, 60);
      Put(key, value);
    }
    #endregion
  }
}
