using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare {
  /// <summary>
  /// Defines the contract that class mappers must implement to return the type of
  /// IFushareController.
  /// </summary>
  public interface IControllerTypeMapper {
    /// <summary>
    /// Returns the type of a class that implements IFushareController.
    /// </summary>
    /// <typeparam name="TReq">Type of request data.</typeparam>
    /// <typeparam name="TResp">Type of response data.</typeparam>
    /// <param name="context">An instance of FushareContext that provides
    /// references to objects that used to service the reuquests.</param>
    /// <returns>The type of the controller that can handle the request.</returns>
    Type GetFushareControllerType<TReq, TResp>(
      FushareContext<TReq, TResp> context);
  }
}
