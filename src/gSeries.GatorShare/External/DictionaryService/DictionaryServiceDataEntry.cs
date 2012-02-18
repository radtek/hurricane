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
  public class DictionaryServiceDataEntry {
    readonly IDictionary<string, object> _metaInfo = 
      new Dictionary<string, object>();
    readonly byte[] _value;

    /// <summary>
    /// Gets the meta info for this entry.
    /// </summary>
    /// <value>The meta info.</value>
    public IDictionary<string, object> MetaInfo {
      get {
        return _metaInfo;
      }
    }

    public byte[] Value {
      get {
        return _value;
      }
    }

    public DictionaryServiceDataEntry(byte[] value) {
      _value = value;
    }

    public DictionaryServiceDataEntry(byte[] value, IDictionary<string, object> metaInfo)
      : this(value) {
      _metaInfo = metaInfo;
    }
  }
}
