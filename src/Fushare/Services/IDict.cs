using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare.Services {
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
