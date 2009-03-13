using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Practices.Unity;
using System.Web.Mvc;

namespace Fushare.Web {
  /// <summary>
  /// The Mvc controller factory that uses Unity
  /// </summary>
  public class UnityControllerFactory : DefaultControllerFactory {
    IUnityContainer container;

    public UnityControllerFactory(IUnityContainer container) {
      this.container = container;
    }

    protected override IController GetControllerInstance(Type controllerType) {
      if (controllerType == null)
        throw new ArgumentNullException("controllerType");

      if (!typeof(IController).IsAssignableFrom(controllerType))
        throw new ArgumentException(string.Format(
            "Type requested is not a controller: {0}",
            controllerType.Name),
            "controllerType");
      return container.Resolve(controllerType) as IController;
    }
  }
}
