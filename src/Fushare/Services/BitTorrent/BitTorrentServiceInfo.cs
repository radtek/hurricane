using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Fushare.Services.BitTorrent {
  public class BitTorrentServiceInfo {
    [XmlIgnore]
    public Uri ServerCacheUri;

    [XmlElement("ServerCacheUri")]
    public string ServerCacheUriString {
      get {
        return ServerCacheUri.ToString();
      }
      set {
        ServerCacheUri = new Uri(value, UriKind.Absolute);
      }
    }
  }
}
