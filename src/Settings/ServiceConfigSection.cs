using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Fushare.Services;

namespace Fushare {
  /// <summary>
  /// Section which incharges 
  /// </summary>
  public class ServiceConfigSection {
    private ServiceHandler[] _service_handlers;
    public ServiceMapping[] _service_mappings;

    [XmlArrayItem(ElementName = "mapping")]
    public ServiceMapping[] serviceMappings {
      get {
        return _service_mappings;
      }
      set {
        _service_mappings = value;
      }
    }

    [XmlArrayItem(ElementName = "handler")]
    public ServiceHandler[] serviceHandlers {
      get {
        return _service_handlers;
      }
      set {
        _service_handlers = value;
        if (ServiceHandlersSet != null)
          ServiceHandlersSet(this, null);
      }
    }

    /**
     * Occurs when the XmlSerializer sets serviceshandlers.
     * 
     */
    public static event EventHandler ServiceHandlersSet;
  }

  /// <summary>
  /// Maps Service handler classes to the URIs where the services are hosted.
  /// </summary>
  public class ServiceHandler {
    public string type;
    public string uri;
  }

  /// <summary>
  /// Decides which operation on which path should be dispatched to which service (class)
  /// </summary>
  /// <remarks>
  /// The type filed should match the type field in one of the ServiceHandlers.
  /// </remarks>
  public class ServiceMapping {
    public string path;
    public string operation;
    public string type;
  }
}
