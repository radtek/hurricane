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
using System.Collections.Generic;
using System;
using System.Text;

namespace GatorShare.External.DictionaryService {
  /// <summary>
  /// The value object returned by the GET operation of 
  /// <see cref="SimpleStorageDictionary"/>
  /// </summary>
  public class SimpleStorageDictionaryData : DictionaryServiceData {

    /// <summary>
    /// List of values stored as Base64 format strings.
    /// </summary>
    /// <remarks>
    /// This field is declared as public for JsonExSerializer.
    /// </remarks>
    public string[] values;

    readonly string _key;

    public SimpleStorageDictionaryData(string key, string[] values) {
      this.values = values;
      _key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleStorageDictionaryData"/> class.
    /// </summary>
    /// <remarks>The ctor for JsonExSerializer</remarks>
    public SimpleStorageDictionaryData() { }

    public override IDictionary<string, object> MetaInfo {
      get { return new Dictionary<string, object>(); }
    }

    public override DictionaryServiceDataEntry[] DataEntries {
      get {
        var list = new List<DictionaryServiceDataEntry>();
        Array.ForEach<string>(values,
          x => list.Add(new DictionaryServiceDataEntry(Convert.FromBase64String(x))));
        return list.ToArray();
      }
    }

    public override byte[] Key {
      get { return Encoding.UTF8.GetBytes(_key); }
    }
  }
}
