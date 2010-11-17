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
using System.Net;
using System.Text;
using GatorShare.External.DictionaryService;

namespace GatorShare.External.DictionaryService {
  /// <summary>
  /// Base class for (D)HTs built on clouds.
  /// </summary>
  /// <remarks>
  /// A set of DHT operations with string typed keys are provided as this is the 
  /// common case for cloud services over HTTP and keys embedded in URLs. <br/>
  /// Methods in this class throw DHT exceptions in erroneous situations. 
  /// </remarks>
  public abstract class CloudDictionary : DictionaryServiceBase {
    #region Fields
    protected string _domain;
    protected int _port;
    protected ServerProxy _serverProxy;
    protected const int DefaultGetCount = 1000; 
    #endregion

    protected string BaseUrl {
      get {
        return string.Format("http://{0}:{1}", _domain, _port);
      }
    }

    #region DhtBase Memebers
    /// <summary>
    /// Gets or sets the default TTL.
    /// </summary>
    /// <value>The default TTL.</value>
    /// <remarks>We don't really need TTL in BigTable.</remarks>
    public override int DefaultTtl {
      get { return -1; }
      set { }
    }

    /// <summary>
    /// Dht Put.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="ttl">The TTL.</param>
    /// <remarks>Note: If the underlying code returns a boolean, we simply pass it
    /// along. But if the problem that causes the failure is clear, a more meaningful
    /// exception is always thrown.</remarks>
    /// <exception cref="DhtException">WebException caught.</exception>
    public override void Put(byte[] key, byte[] value, int ttl) {
      var keyStr = EncodeKeyBytes(key);
      try {
        Put(keyStr, value);
      } catch (WebException ex) {
        throw BuildDictionaryServiceException(ex, keyStr);
      }
    }

    /// <summary>
    /// Creates the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="ttl">The TTL.</param>
    /// <exception cref="DhtException">WebException caught.</exception>
    public override void Create(byte[] key, byte[] value, int ttl) {
      var keyStr = EncodeKeyBytes(key);
      try {
        Create(keyStr, value);
      } catch (WebException ex) {
        throw BuildDictionaryServiceException(ex, keyStr);
      }
    }

    /// <summary>
    /// Gets the value by the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The wrapper of returned value.</returns>
    /// <exception cref="DhtException">WebException caught.</exception>
    /// <exception cref="DictionaryKeyNotFoundException">Such key doesn't exist.
    /// </exception>
    public override DictionaryServiceData Get(byte[] key) {
      var keyStr = EncodeKeyBytes(key);
      DictionaryServiceData results;
      try {
        results = GetMultiple(keyStr, DefaultGetCount);
      } catch (WebException ex) {
        // There won't be an exception thrown if the key doesn't exist.
        throw BuildDictionaryServiceException(ex, keyStr);
      }

      if (results.Values.Count == 0) {
        var ex = new DictionaryKeyNotFoundException(string.Format(
          "Key {0} doesn't exist.", keyStr));
        ex.DictionaryKey = keyStr;
        throw ex;
      }

      return results;
    }
    #endregion

    #region Protected Methods
    /// <summary>
    /// Encodes the key bytes into a string.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    protected static string EncodeKeyBytes(byte[] key) {
      var ret = Encoding.UTF8.GetString(key);
      return ret;
    }

    protected static DictionaryServiceException BuildDictionaryServiceException(WebException ex, string keyStr) {
      var newEx = new DictionaryServiceException(string.Format(
        "WebException thrown when communicating with Dht. \nReturned Data:{0}",
        ex.Data), ex);
      newEx.DictionaryKey = keyStr;
      throw newEx;
    }

    protected static T ConvertFromJsonString<T>(string jsonString) {
      return JsonUtil.ConvertFromJsonString<T>(jsonString);
    }
    #endregion

    #region Abstracts
    public abstract DictionaryServiceData GetMultiple(string key, int count);
    public abstract void Put(string key, byte[] value);
    public abstract void Create(string key, byte[] value); 
    #endregion
  }
}
