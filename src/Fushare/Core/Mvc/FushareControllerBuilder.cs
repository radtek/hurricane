using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare {
  /// <summary>
  /// A singleton class that builds the factory class for IFushareController.
  /// </summary>
  public class FushareControllerBuilder {

    #region Fields
    private static FushareControllerBuilder _instance = 
      new FushareControllerBuilder();
    private IFushareControllerFactory _factory; 
    #endregion

    #region Properties
    /// <summary>
    /// Singleton Instance.
    /// </summary>
    public static FushareControllerBuilder Instance {
      get {
        return _instance;
      }
    }

    public IFushareControllerFactory ControllerFactory {
      get {
        return _factory;
      }
    }
    #endregion

    /// <summary>
    /// Initializes FushareControllerBuilder with an instance of 
    /// DefaultFushareControllerFactory.
    /// </summary>
    public FushareControllerBuilder() {
      SetControllerFactory(new DefaultFushareControllerFactory());
    }

    public void SetControllerFactory(IFushareControllerFactory 
      controllerFactory) {
      if (controllerFactory == null) {
        throw new ArgumentNullException("controllerFactory");
      }
      _factory = controllerFactory;
    }


  }
}
