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
using System.Collections;
using System.Collections.Specialized;

namespace GatorShare.External.DictionaryService {
  /// <summary>
  /// The class that implements the <see cref="DictionaryServiceData"/> for 
  /// BrunetDhtService.
  /// </summary>
  public class BrunetDhtServiceData : DictionaryServiceData {

    readonly Hashtable[] _data;
    readonly byte[] _key;

    public BrunetDhtServiceData(byte[] key, Hashtable[] data) {
      _data = data;
      _key = key;
    }

    public override IDictionary<string, object> MetaInfo {
      get { return new Dictionary<string, object>(); }
    }

    public override DictionaryServiceDataEntry[] DataEntries {
      get {
        var list = new List<DictionaryServiceDataEntry>();
        Array.ForEach<Hashtable>(_data, delegate(Hashtable item) {
          IDictionary<string, object> metaInfo = new Dictionary<string, object>();
          metaInfo["age"] = item["age"];
          metaInfo["ttl"] = item["ttl"];
          var entry = new DictionaryServiceDataEntry(item["value"] as byte[], metaInfo);
          list.Add(entry);
        });
        return list.ToArray();
      }
    }

    public override byte[] Key {
      get { return _key; }
    }
  }
}
