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
  public interface IDictionaryService {
    void Put(object key, object value);
    object Get(object key);
  }
}
