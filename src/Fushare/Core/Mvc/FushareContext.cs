using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare {
  /// <summary>
  /// Provides access to the execution context of a service method.
  /// </summary>
  public class FushareContext<TReq, TResp> {
    
    public FushareRequest<TReq> Request {
      get;
      protected set;
    }

    public FushareResponse<TResp> Response {
      get;
      protected set;
    }

    private IFushareController Handler {
      get;
      set;
    }

    public FushareContext(FushareRequest<TReq> request, 
      FushareResponse<TResp> response) {
      Request = request;
      Response = response;
    }

  }
}
