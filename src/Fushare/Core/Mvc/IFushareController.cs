using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare {
  /// <summary>
  /// Defines the contract that a class implements to process requests from clients 
  /// of Fushare service.
  /// </summary>
  /// <remarks>
  /// Implementations of this interface should be stateless. Should they???
  /// </remarks>
  public interface IFushareController {
    /// <summary>
    /// Processes of a request.
    /// </summary>
    void Execute<TReq, TResp>(FushareContext<TReq, TResp> context);
  }
}
