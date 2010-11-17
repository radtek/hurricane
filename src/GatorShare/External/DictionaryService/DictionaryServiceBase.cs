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

namespace GatorShare.External.DictionaryService {
  /// <summary>
  /// Implements the IDict interface using dictionary service.
  /// </summary>
  /// <remarks>To access a distributed dictionary service the data goes to the 
  /// wire so a set of byte array interfaces are added.</remarks>
  public abstract class DictionaryServiceBase : IDict {

    #region IDict Members

    /// <summary>
    /// Puts a key,value pair to the dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <remarks>Duplicated key doesn't throw exception.</remarks>
    public virtual void Put(object key, object value) {
      if(!(key is byte[] && value is byte[])) {
        throw  new ArgumentException("Key/value should be byte[]");
      }
      Put(key as byte[], value as byte[], DefaultTtl);
    }

    /// <summary>
    /// Gets the value by the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>the value object.</returns>
    public virtual object Get(object key) {
      if (!(key is byte[])) {
        throw new ArgumentException("Key should be byte[]");
      }
      var dhtResult = Get(key as byte[]);
      return dhtResult.Value;
    }
    /// <summary>
    /// Creates a key,value pair to the dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <remarks>Duplicated key throws an exception.</remarks>
    public virtual void Create(object key, object value) {
      if (!(key is byte[] && value is byte[])) {
        throw new ArgumentException("Key/value should be byte[]");
      }
      Create(key as byte[], value as byte[], DefaultTtl);
    }
    #endregion

    #region Abstracts

    /// <summary>
    /// Gets or sets the default TTL.
    /// </summary>
    /// <value>The default TTL.</value>
    public abstract int DefaultTtl { get; set; }
    /// <summary>
    /// Dht Put.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="ttl">The TTL.</param>
    public abstract void Put(byte[] key, byte[] value, int ttl);
    public abstract void Create(byte[] key, byte[] value, int ttl);
    /// <summary>
    /// Gets the value by the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The wrapper of returned value.</returns>
    /// <exception cref="DhtException">WebException caught.</exception>
    /// <exception cref="DictionaryKeyNotFoundException">Such key doesn't exist.
    /// </exception>
    public abstract DictionaryServiceData Get(byte[] key); 
    #endregion
  }
}
