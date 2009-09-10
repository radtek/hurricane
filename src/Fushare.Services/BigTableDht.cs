using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fushare.Services;
using System.Net;
using System.Web;

namespace Fushare.Services {
  /// <summary>
  /// DhtBase Implementation using BigTable web service.
  /// </summary>
  /// <remarks>See http://bigtable.appspot.com </remarks>
  public class BigTableDht :  CloudDht {
    #region Fields
    static readonly IDictionary _log_props = 
      Logger.PrepareLoggerProperties(typeof(BigTableDht));
    readonly string _user;
    readonly string _secret;
    readonly string _table;
    const string DefaultTableName = "fushare001";
    const string DefaultColumnName = "col"; 
    #endregion

    string AuthString {
      get {
        return _user + ":" + _secret;
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
      _serverProxy = new ServerProxy(BaseUrl);
    }
    #endregion

    #region DhtBase Members

    #endregion

    #region Public Methods
    public byte[] GetMostRecentValAsOctetStream(string key) {
      string relativeUri = string.Format(
        "/get/{0}/{1}/{2}?content-type=application/octet-stream", _table, key,
        DefaultColumnName);
      byte[] result = _serverProxy.Get(new Uri(relativeUri, UriKind.Relative));
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
      var resultString = _serverProxy.GetUTF8String(relativeUri);
      BigTableRetVal[] tuples = ConvertFromJsonString<BigTableRetVal[]>(resultString);
      if (tuples.Length == 0) {
        return new NullBigTableRetVal();
      } else {
        return tuples[0];
      }
    }
    #endregion

    #region CloudDht Members
    public override DhtResults GetMultiple(string key, int count) {
      string relativeUri = string.Format("/getVer/{0}/{1}/{2}/{3}", _table, key,
        DefaultColumnName, count);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Getting by the URL: {0}", relativeUri));
      var resultBytes = _serverProxy.Get(new Uri(relativeUri, UriKind.Relative));
      var resultString = Encoding.UTF8.GetString(resultBytes);
      var vals = ConvertFromJsonString<BigTableRetVal[]>(resultString);
      return ConvertToDhtResults(vals);
    }

    public override void Put(string key, byte[] value) {
      string relativeUri = string.Format("/put/{0}/{1}/{2}/{3}", AuthString, _table,
        key, DefaultColumnName);
      _serverProxy.Put(relativeUri, value);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Put or Create successfully by the URL: {0}", relativeUri));
    }

    /// <summary>
    /// Creates an entry in BigTable.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <remarks>This operation is a combination of get and put and is not atomic
    /// </remarks>
    public override void Create(string key, byte[] value) {
      BigTableRetVal result;
      result = GetMostRecent(key);

      if (result is NullBigTableRetVal) {
        Put(key, value);
      } else {
        throw new DuplicateResourceKeyException(
          string.Format("The same key {0} alreay exists in current table.", key));
      }
    } 
    #endregion

    #region Private Methods

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
