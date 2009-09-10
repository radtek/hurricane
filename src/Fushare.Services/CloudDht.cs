using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using JsonExSerializer;
using JsonExSerializer.Collections;

namespace Fushare.Services {
  /// <summary>
  /// Base class for (D)HTs built on clouds.
  /// </summary>
  /// <remarks>
  /// A set of DHT operations with string typed keys are provided as this is the 
  /// common case for cloud services over HTTP and keys embedded in URLs. <br/>
  /// Methods in this class throw DHT exceptions in erroneous situations. 
  /// </remarks>
  public abstract class CloudDht : DhtBase {
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
        throw BuildDhtException(ex, keyStr);
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
        throw BuildDhtException(ex, keyStr);
      }
    }

    /// <summary>
    /// Gets the value by the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The wrapper of returned value.</returns>
    /// <exception cref="DhtException">WebException caught.</exception>
    /// <exception cref="ResourceNotFoundException">Such key doesn't exist.
    /// </exception>
    public override DhtResults Get(byte[] key) {
      var keyStr = EncodeKeyBytes(key);
      DhtResults results;
      try {
        results = GetMultiple(keyStr, DefaultGetCount);
      } catch (WebException ex) {
        // There won't be an exception thrown if the key doesn't exist.
        throw BuildDhtException(ex, keyStr);
      }

      if (results.Values.Count == 0) {
        var ex = new ResourceNotFoundException(string.Format(
          "Key {0} doesn't exist.", keyStr));
        ex.ResourceKey = keyStr;
        throw ex;
      }

      return results;
    }
    #endregion

    #region Protected Methods
    protected static string EncodeKeyBytes(byte[] key) {
      var ret = UrlBase64.Encode(key);
      return ret;
    }

    protected static DhtException BuildDhtException(WebException ex, string keyStr) {
      var newEx = new DhtException(string.Format(
        "WebException thrown when communicating with Dht. \nReturned Data:{0}",
        ex.Data), ex);
      newEx.ResourceKey = keyStr;
      throw newEx;
    }

    protected static T ConvertFromJsonString<T>(string jsonString) {
      Serializer serializer = new Serializer(typeof(T));
      T results;
      try {
        results = (T)serializer.Deserialize(jsonString);
      } catch (Exception ex) {
        Console.WriteLine("testline. {0}", ex);
        throw;
      }
      return results;
    }
    #endregion

    #region Abstracts
    public abstract DhtResults GetMultiple(string key, int count);
    public abstract void Put(string key, byte[] value);
    public abstract void Create(string key, byte[] value); 
    #endregion
  }
}
