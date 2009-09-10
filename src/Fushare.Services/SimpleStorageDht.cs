using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  /// <summary>
  /// See http://simple-storage.appspot.com
  /// </summary>
  public class SimpleStorageDht : CloudDht {
    #region Fields
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(SimpleStorageDht));
    const string _controller = "hashtable"; 
    #endregion

    #region Ctors
    public SimpleStorageDht() : this("simple-storage.appspot.com", 80) { }

    SimpleStorageDht(string domain, int port) {
      _domain = domain;
      _port = port;
      _serverProxy = new ServerProxy(BaseUrl);
    }
    #endregion

    #region CloudDht Memebers
    public override DhtResults GetMultiple(string key, int count) {
      string relativeUri = string.Format("/{0}/{1}", _controller, key);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Getting by the URL: {0}", relativeUri));
      byte[] resultBytes = _serverProxy.Get(relativeUri);
      string resultString = Encoding.UTF8.GetString(resultBytes);
      var val = ConvertFromJsonString<SimpleStorageDhtRetVal>(resultString);
      return ConvertToDhtResults(val);
    }

    public override void Put(string key, byte[] value) {
      string relativeUri = string.Format("/{0}/{1}", _controller, key);
      _serverProxy.Post(relativeUri, TextUtil.ToUTF8Base64(value));
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Put or Create successfully by the URL: {0}", relativeUri));
    }

    public override void Create(string key, byte[] value) {
      var results = GetMultiple(key, 1);
      if (results.Value == null) {
        Put(key, value);
      } else {
        throw new DuplicateResourceKeyException(
          string.Format("The same key {0} alreay exists in current table.", key));
      }
    }
    #endregion

    #region Private Methods
    private static DhtResults ConvertToDhtResults(SimpleStorageDhtRetVal val) {
      var results = new DhtResults();
      foreach (var valString in val.values) {
        // We use base64 string to encode value bytes when we do puts.
        var entry = new DhtResultEntry(Convert.FromBase64String(valString));
        results.ResultEntries.Add(entry);
      }
      return results;
    } 
    #endregion
  }
}
