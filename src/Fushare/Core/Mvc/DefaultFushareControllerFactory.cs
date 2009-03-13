using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare {
  class DefaultFushareControllerFactory : IFushareControllerFactory {

    #region IFushareControllerFactory Members

    public IFushareController CreateController<TReq, TResp>(
      FushareContext<TReq, TResp> fushareContext) {
      IFushareController ret;
      Type controller_type = 
        BasicControllerTypeMapper.Instance.
        GetFushareControllerType<TReq, TResp>(fushareContext);
      // Make sure the returned type is indeed an IFushareController
      if (!typeof(IFushareController).IsAssignableFrom(controller_type)) {
        throw new ArgumentException("Returned type isn't an IFushareController");
      }
      try {
        ret = (IFushareController)Activator.CreateInstance(controller_type);
      } catch (Exception ex) {
        throw new InvalidOperationException(
          "Exception thrown when creating controller from type", ex);
      }
      return ret;
    }

    public virtual void ReleaseController(IFushareController controller) {
      IDisposable disposable = controller as IDisposable;
      if (disposable != null) {
        disposable.Dispose();
      }
    }
    #endregion
  }
}
