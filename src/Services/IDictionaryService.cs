using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare.Services {
  /**
   * An abstraction of services that allow
   * clients to store and retrieve data with Put and Get operation.
   * Note: Delete operation is not presented in all such services, thus 
   * it doesn't reside in the interface.
   */
  public interface IDictionaryService {
    void Put(object key, object value);
    object Get(object key);
  }
}
