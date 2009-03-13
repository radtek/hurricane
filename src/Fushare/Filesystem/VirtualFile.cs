using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Fushare.Filesystem {
  public class VirtualFile {
    #region Properties
    [XmlIgnore]
    public Uri PhysicalUri { get; set; } 
    #endregion

    #region Properties for XmlSerializer
    public string PhysicalUriString {
      get {
        return PhysicalUri.ToString();
      }
      set {
        PhysicalUri = new Uri(value);
      }
    }
    #endregion
  }
}
