using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare {
  /// <summary>
  /// Encapsulates the response of an Fushare operation.
  /// </summary>
  /// <typeparam name="T">The type of data object to return.</typeparam>
  public class FushareResponse<T> {

    public T ResponseData {
      get;
      set;
    }

  }
}
