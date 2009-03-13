using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare {
  /// <summary>
  /// Encapsulates the request to the Fushare operation.
  /// </summary>
  /// <typeparam name="T">The type of the data object in the request.
  /// </typeparam>
  public class FushareRequest<T> {

    #region Properties
    /// <summary>
    /// Note this string is protocol/technology independent. It could be URI.
    /// </summary>
    public string ParamString {
      get;
      set;
    }

    public T RequestData {
      get;
      set;
    }

    public FushareRequestMethod Method {
      get;
      set;
    }
    #endregion

  }
}
