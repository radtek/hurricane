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
using System.Text;
using GatorShare.External.DictionaryService;

namespace GatorShare.External.DictionaryService {
  /// <summary>
  /// DhtBase Implementation using BigTable web service.
  /// </summary>
  /// <remarks>See http://bigtable.appspot.com </remarks>
  public class BigTableDictionary :  CloudDictionary {
    #region Fields
    static readonly IDictionary _log_props = 
      Logger.PrepareLoggerProperties(typeof(BigTableDictionary));
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
    public BigTableDictionary(string user, string secret)
      : this("bigtable.appspot.com", 80, user, secret, DefaultTableName) {
    }

    public BigTableDictionary(string domain, int port, string user, string secret,
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

    public BigTableDictionaryData GetMostRecent(string key) {
      string relativeUri = string.Format("/get/{0}/{1}/{2}", _table, key,
        DefaultColumnName);
      var resultString = _serverProxy.GetUTF8String(relativeUri);
      BigTableDictionaryData[] tuples = ConvertFromJsonString<BigTableDictionaryData[]>(resultString);
      if (tuples.Length == 0) {
        return new NullBigTableDictionaryData();
      } else {
        return tuples[0];
      }
    }
    #endregion

    #region CloudDht Members
    public override DictionaryServiceData GetMultiple(string key, int count) {
      string relativeUri = string.Format("/getVer/{0}/{1}/{2}/{3}", _table, key,
        DefaultColumnName, count);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Getting by the URL: {0}", relativeUri));
      var resultBytes = _serverProxy.Get(new Uri(relativeUri, UriKind.Relative));
      var resultString = Encoding.UTF8.GetString(resultBytes);
      var vals = ConvertFromJsonString<BigTableDictionaryData[]>(resultString);
      return ConvertToDictionaryServiceData(vals);
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
      BigTableDictionaryData result;
      result = GetMostRecent(key);

      if (result is NullBigTableDictionaryData) {
        Put(key, value);
      } else {
        throw new DictionaryKeyException(
          string.Format("The same key {0} alreay exists in current table.", key));
      }
    } 
    #endregion

    #region Private Methods

    private static DictionaryServiceData ConvertToDictionaryServiceData(BigTableDictionaryData[] vals) {
      var results = new DictionaryServiceData();
      foreach (var val in vals) {
        var entry = new DictionaryServiceDataEntry(val.Value);
        entry.MetaInfo["timestamp"] = val.Timestamp;
        results.ResultEntries.Add(entry);
      }
      return results;
    } 
    #endregion
  }
}
