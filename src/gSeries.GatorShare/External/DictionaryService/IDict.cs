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
using System.Text;

namespace GSeries.External.DictionaryService {
  /// <summary>
  /// An abstraction of services that allow clients to store and retrieve data with Put
  /// and Get operation.
  /// </summary>
  /// <remarks>
  /// Delete operation is not presented in all such services, thus it doesn't reside in
  /// the interface.
  /// </remarks>
  public interface IDict {
    /// <summary>
    /// Puts a key,value pair to the dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <remarks>Duplicated key doesn't throw exception.</remarks>
    void Put(object key, object value);

    /// <summary>
    /// Creates a key,value pair to the dictionary. 
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <remarks>Duplicated key throws an exception.</remarks>
    void Create(object key, object value);

    /// <summary>
    /// Gets the value by the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>the value object.</returns>
    object Get(object key);
  }
}
