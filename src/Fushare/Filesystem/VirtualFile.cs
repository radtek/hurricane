using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Fushare.Filesystem {
  /// <summary>
  /// The placeholder and metadata for a real file.
  /// </summary>
  /// <remarks>It's serializable with XmlSerializer and thus needs to be mutable.</remarks>
  public class VirtualFile {
    #region Properties
    /// <summary>
    /// Gets or sets the URI to the physical file. It could be of any protocol.
    /// </summary>
    /// <value>The URI.</value>
    [XmlIgnore]
    public Uri PhysicalUri { get; set; }
    /// <summary>
    /// Gets or sets the size of the real file.
    /// </summary>
    /// <value>The size of the file.</value>
    public long FileSize { get; set; }
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
