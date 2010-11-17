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

namespace GatorShare.External.DictionaryService {
  /// <summary>
  /// The data class for the results returned by DHT.
  /// </summary>
  public class DictionaryServiceData {
    #region Fields
    readonly IDictionary<string, object> _metaInfo = 
      new Dictionary<string, object>();
    readonly List<DictionaryServiceDataEntry> _entryList = new List<DictionaryServiceDataEntry>(); 
    #endregion

    #region Properties
    public IDictionary<string, object> MetaInfo {
      get {
        return _metaInfo;
      }
    }

    public IList<DictionaryServiceDataEntry> ResultEntries {
      get {
        return _entryList;
      }
    }

    public IList<byte[]> Values {
      get {
        var retList = new List<byte[]>();
        foreach (DictionaryServiceDataEntry entry in _entryList) {
          retList.Add(entry.Value);
        }
        return retList;
      }
    }

    public bool HasMultipleEntries {
      get {
        return _entryList.Count > 1 ? true : false;
      }
    }

    /// <summary>
    /// Gets the (first) value. Null returned if ResultEntry is empty.
    /// </summary>
    /// <value>The value.</value>
    public byte[] Value {
      get {
        return ResultEntry != null ? ResultEntry.Value : null;
      }
    }

    /// <summary>
    /// Gets the first result entry.
    /// </summary>
    /// <value>The result entry.</value>
    public DictionaryServiceDataEntry ResultEntry {
      get {
        return _entryList.Count > 0 ? _entryList[0] : null;
      }
    } 
    #endregion
  }
}
