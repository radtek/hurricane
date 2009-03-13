using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Fushare.Services;

namespace Fushare.Configuration {
  /// <summary>
  /// The configuration of external services used by Fushare.
  /// </summary>
  public class ServiceConfigSection {
    private ServiceHandlerMapping[] _service_handler_mappings;
    private PathHandlerMapping[] _path_handler_mappings;

    [XmlArrayItem(ElementName = "handler")]
    public PathHandlerMapping[] pathHandlers {
      get {
        return _path_handler_mappings;
      }
      set {
        _path_handler_mappings = value;
      }
    }

    [XmlArrayItem(ElementName = "handler")]
    public ServiceHandlerMapping[] serviceHandlers {
      get {
        return _service_handler_mappings;
      }
      set {
        _service_handler_mappings = value;
        if (ServiceHandlersSet != null)
          ServiceHandlersSet(this, null);
      }
    }

    /// <summary>
    /// Occurs when the XmlSerializer sets serviceshandlers.
    /// </summary>
    public static event EventHandler ServiceHandlersSet;
  }

  /// <summary>
  /// Maps Service handler classes to the URIs where the services are hosted.
  /// </summary>
  public class ServiceHandlerMapping {
    public string type;
    public string uri;
  }

  /// <summary>
  /// Decides which operation on which path should be dispatched to which handler (class)
  /// </summary>
  /// <remarks>
  /// The type filed should match the type field in one of the ServiceHandlers.
  /// </remarks>
  public class PathHandlerMapping {
    /// <summary>
    /// The path attribute can contain either a single URL path or a simple wildcard string (for example, *.aspx).
    /// </summary>
    public string path;
    /// <summary>
    /// Comma Seperated enum value of FuseMethod, or *.
    /// </summary>
    public string verb;
    /// <summary>
    /// Specifies a comma-separated class/assembly combination. 
    /// </summary>
    public string type;
  }
}
