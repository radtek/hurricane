using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare.Services {
  /// <summary>
  /// Implements the IDict interface using Dht.
  /// </summary>
  /// <remarks>"Distributed" means data goes to the wire so a set of byte array 
  /// interfaces are added.</remarks>
  public abstract class DhtBase : IDict {

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
    /// <exception cref="ResourceNotFoundException">Such key doesn't exist.
    /// </exception>
    public abstract DhtResults Get(byte[] key); 
    #endregion
  }
}
