using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare {
  /// <summary>
  /// Defines the contract that class factories must implement to create new 
  /// IFushareController objects.
  /// </summary>
  public interface IFushareControllerFactory {
    /// <summary>
    /// Returns an instance of a class that implements the IFushareController 
    /// interface.
    /// </summary>
    IFushareController CreateController<TReq, TResp>(
      FushareContext<TReq, TResp> fushareContext);
    /// <summary>
    /// Enables a factory to reuse an existing handler instance.
    /// </summary>
    void ReleaseController(IFushareController controller);
  }
}
