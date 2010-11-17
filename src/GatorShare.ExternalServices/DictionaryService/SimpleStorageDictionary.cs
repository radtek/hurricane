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
  /// See http://simple-storage.appspot.com
  /// </summary>
  public class SimpleStorageDictionary : CloudDictionary {
    #region Fields
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(SimpleStorageDictionary));
    const string _controller = "hashtable"; 
    #endregion

    #region Ctors
    public SimpleStorageDictionary() : this("simple-storage.appspot.com", 80) { }

    SimpleStorageDictionary(string domain, int port) {
      _domain = domain;
      _port = port;
      _serverProxy = new ServerProxy(BaseUrl);
    }
    #endregion

    #region CloudDht Memebers
    public override DictionaryServiceData GetMultiple(string key, int count) {
      string relativeUri = string.Format("/{0}/{1}", _controller, key);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Getting by the URL: {0}", relativeUri));
      // Give the server another chance if it's busy.
      byte[] resultBytes = _serverProxy.GetWithRetries(relativeUri, 1);
      string resultString = Encoding.UTF8.GetString(resultBytes);
      var val = ConvertFromJsonString<SimpleStorageDictionaryData>(resultString);
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
        throw new DictionaryKeyException(
          string.Format("The same key {0} alreay exists in current table.", key));
      }
    }
    #endregion

    #region Private Methods
    private static DictionaryServiceData ConvertToDhtResults(SimpleStorageDictionaryData val) {
      var results = new DictionaryServiceData();
      foreach (var valString in val.values) {
        // We use base64 string to encode value bytes when we do puts.
        var entry = new DictionaryServiceDataEntry(Convert.FromBase64String(valString));
        results.ResultEntries.Add(entry);
      }
      return results;
    } 
    #endregion
  }
}
