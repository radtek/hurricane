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
using System.Net;
using GSeries.External.DictionaryService;

namespace GSeries.External.DictionaryService {
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
      byte[] resultBytes;
      try {
        // Give the server another chance if it's busy.
        resultBytes = _serverProxy.GetWithRetries(relativeUri, 1);
      } catch (WebException) {
        
        throw;
      }
      string resultString = Encoding.UTF8.GetString(resultBytes);
      var data = ConvertFromJsonString<SimpleStorageDictionaryData>(resultString);
      if (data == null || data.DataEntries.Length == 0) {
        throw new DictionaryKeyNotFoundException() {
          DictionaryKey = Encoding.UTF8.GetBytes(key)
        };
      }
      return data;
    }

    public override void Put(string key, byte[] value) {
      string relativeUri = string.Format("/{0}/{1}", _controller, key);
      try {
        _serverProxy.Post(relativeUri, TextUtil.ToUTF8Base64(value));
      } catch (WebException ex) {
        throw new DictionaryServiceException("Cannot do put.", ex) { 
          DictionaryKey = Encoding.UTF8.GetBytes(key) 
        };
      }
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Put or Create successfully by the URL: {0}", relativeUri));
    }

    public override void Create(string key, byte[] value) {
      var results = GetMultiple(key, 1);
      if (results.FirstValue == null) {
        Put(key, value);
      } else {
        throw new DictionaryKeyException(
          "The same key alreay exists in current table.") { 
          DictionaryKey = Encoding.UTF8.GetBytes(key) 
        };
      }
    }

    public override void Put(byte[] key, byte[] value) {
      Put(Encoding.UTF8.GetString(key), value);
    }

    public override void Create(byte[] key, byte[] value) {
      Create(Encoding.UTF8.GetString(key), value);
    }

    #endregion
  }
}
