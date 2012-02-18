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
  /// Thrown if there is some error while accessing the dictionary service.
  /// </summary>
  public class DictionaryServiceException : Exception {
    public DictionaryServiceException() : base() { }
    public DictionaryServiceException(string message) : base(message) { }
    public DictionaryServiceException(string message, Exception innerException) :
      base(message, innerException) { }

    /// <summary>
    /// Gets or sets the resource key.
    /// </summary>
    /// <value>The resource key.</value>
    /// <remarks>Could be a key, name, uri, etc.</remarks>
    public byte[] DictionaryKey {
      get {
        return _dictKey;
      }
      set {
        _dictKey = value;
      }
    }
    byte[] _dictKey;

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      sb.Append(base.ToString());
      sb.Append(System.Environment.NewLine);
      sb.Append(string.Format("Key: {0}", Encoding.UTF8.GetString(DictionaryKey)));
      return sb.ToString();
    }
  }
}
