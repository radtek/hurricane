using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare.Services {
  /**
   * Implements the IDictionaryService interface using Dht.
   */
  public abstract class DhtService : IDictionaryService {

    public DhtService() { }
    public DhtService(Uri uri) { }


    #region IDictionaryService Members

    public abstract void Put(object key, object value);

    public abstract object Get(object key);

    #endregion
  }
}
