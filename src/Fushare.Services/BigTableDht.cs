using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fushare.Services;
using System.Net;
using JsonExSerializer;
using JsonExSerializer.Collections;
using System.Web;

namespace Fushare.Services {
  /// <summary>
  /// DhtBase Implementation using BigTable web service.
  /// </summary>
  /// <remarks>See http://bigtable.appspot.com/. For DhtBase methods,
  /// WebExceptions are wrapped in DhtExceptions as the caller has no knowledge 
  /// about the infrasture being HTTP.</remarks>
  public class BigTableDht :  DhtBase {
    readonly string _domain;
    readonly int _port;
    readonly string _user;
    readonly string _secret;
    readonly string _table;
    WebClient _webClient = new WebClient();
    const string DefaultTableName = "fushare001";
    const string DefaultColumnName = "col";
    const int DefaultGetCount = 1000;

    string AuthString {
      get {
        return _user + ":" + _secret;
      }
    }

    string BaseUrl {
      get {
        return string.Format("http://{0}:{1}", _domain, _port);
      }
    }

    #region Constructors
    public BigTableDht(string user, string secret)
      : this("bigtable.appspot.com", 80, user, secret, DefaultTableName) {
    }

    public BigTableDht(string domain, int port, string user, string secret,
      string table) {
      _domain = domain;
      _port = port;
      _user = user;
      _secret = secret;
      _table = table;
      _webClient.BaseAddress = BaseUrl;
    } 
    #endregion

    #region DhtBase Members

    /// <summary>
    /// Gets or sets the default TTL.
    /// </summary>
    /// <value>The default TTL.</value>
    /// <remarks>We don't really need TTL in BigTable.</remarks>
    public override int DefaultTtl {
      get {
        return -1;
      }
      set {
      }
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
        throw BuildDhtException(ex);
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
        throw BuildDhtException(ex);
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
      BigTableRetVal[] vals;
      try {
        vals = GetMultiple(keyStr, DefaultGetCount);
      } catch (WebException ex) {
        // There won't be an exception thrown if the key doesn't exist.
        throw BuildDhtException(ex);
      }

      if (vals.Length == 0) {
        var ex = new ResourceNotFoundException("No such key exists.");
        ex.ResourceKey = keyStr;
        throw ex;
      }
      var results = ConvertToDhtResults(vals);
      return results;
    }

    #endregion

    #region Public Methods
    public void Put(string key, byte[] value) {
      string relativeUri = string.Format("/put/{0}/{1}/{2}/{3}", AuthString, _table,
        key, DefaultColumnName);
      _webClient.UploadData(relativeUri, "PUT", value);
    }

    /// <summary>
    /// Creates an entry in BigTable.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <remarks>This operation is a combination of get and put and is not atomic
    /// </remarks>
    public void Create(string key, byte[] value) {
      BigTableRetVal result;
      result = GetMostRecent(key);

      if (result is NullBigTableRetVal) {
        Put(key, value);
      } else {
        throw new DuplicateResourceKeyException(
          "The same key alreay exists in current table.");
      }
    }

    public byte[] GetMostRecentValAsOctetStream(string key) {
      string relativeUri = string.Format(
        "/get/{0}/{1}/{2}?content-type=application/octet-stream", _table, key,
        DefaultColumnName);
      byte[] result = _webClient.DownloadData(relativeUri);
      return result;
    }

    /// <summary>
    /// Achieves the same thing as GetMostRecentValAsOctetStream does.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public byte[] GetMostRecentVal(string key) {
      return GetMostRecent(key).Value;
    }

    public BigTableRetVal GetMostRecent(string key) {
      string relativeUri = string.Format("/get/{0}/{1}/{2}", _table, key,
        DefaultColumnName);
      var resultString = _webClient.DownloadString(relativeUri);
      BigTableRetVal[] tuples = ConvertFromJsonString(resultString);
      if (tuples.Length == 0) {
        return new NullBigTableRetVal();
      } else {
        return tuples[0];
      }
    }

    public BigTableRetVal[] GetMultiple(string key, int count) {
      string relativeUri = string.Format("/getVer/{0}/{1}/{2}/{3}", _table, key,
        DefaultColumnName, count);
      string resultString = _webClient.DownloadString(relativeUri);
      return ConvertFromJsonString(resultString);
    }

    public IEnumerable<byte[]> GetMultipleVals(string key, int count) {
      BigTableRetVal[] tuples = GetMultiple(key, count);
      foreach (BigTableRetVal tuple in tuples) {
        yield return tuple.Value;
      }
    } 
    #endregion

    #region Private Methods
    private static BigTableRetVal[] ConvertFromJsonString(string jsonString) {
      Serializer serializer = new Serializer(typeof(BigTableRetVal[]));
      serializer.Context.RegisterCollectionHandler(new ArrayHandler());
      var results = (BigTableRetVal[])serializer.Deserialize(jsonString);
      return results;
    }

    private static string EncodeKeyBytes(byte[] key) {
      var ret = UrlBase64.Encode(key);
      return ret;
    }

    private static DhtException BuildDhtException(WebException ex) {
      return new DhtException("Exception thrown when communicating with Dht.",
        ex);
    }

    private static DhtResults ConvertToDhtResults(BigTableRetVal[] vals) {
      var results = new DhtResults();
      foreach (var val in vals) {
        var entry = new DhtResultEntry(val.Value);
        entry.MetaInfo["timestamp"] = val.Timestamp;
        results.ResultEntries.Add(entry);
      }
      return results;
    } 
    #endregion
  }
}
