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

namespace GSeries.External.DictionaryService {
  /// <summary>
  /// The class that represents the data stored in the dictionary service
  /// from the client's perspective.
  /// </summary>
  public abstract class DictionaryServiceData {
    #region Properties
    public abstract IDictionary<string, object> MetaInfo { get; }
    public abstract DictionaryServiceDataEntry[] DataEntries { get; }
    public abstract byte[] Key { get; }

    /// <summary>
    /// A convenience property to get the values without meta info.
    /// </summary
    public byte[][] Values {
      get {
        var retList = new List<byte[]>();
        Array.ForEach<DictionaryServiceDataEntry>(DataEntries, 
          x => retList.Add(x.Value));
        return retList.ToArray();
      }
    }

    public bool HasMultipleEntries {
      get {
        return DataEntries.Length > 1 ? true : false;
      }
    }

    /// <summary>
    /// Gets the (first) value. Null returned if ResultEntry is empty.
    /// </summary>
    /// <value>The value.</value>
    public byte[] FirstValue {
      get {
        return FirstEntry != null ? FirstEntry.Value : null;
      }
    }

    /// <summary>
    /// Gets the first result entry.
    /// </summary>
    /// <value>The result entry.</value>
    public DictionaryServiceDataEntry FirstEntry {
      get {
        return DataEntries.Length > 0 ? DataEntries[0] : null;
      }
    }
    #endregion
  }
}
