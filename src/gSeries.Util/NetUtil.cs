using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace GSeries {
  public static class NetUtil {
    [Obsolete("Doesn't work correctly on a Unbutu.")]
    public static IList<IPAddress> GetLocalIPByPrefix(string prefix) {
      string hostName = Dns.GetHostName();
      IPHostEntry entry = Dns.GetHostEntry(hostName);
      IPAddress[] list = entry.AddressList;
      var ret = new List<IPAddress>();
      foreach (IPAddress addr in list) {
        if (addr.ToString().StartsWith(prefix)) {
          ret.Add(addr);
        }
      }
      return ret;
    }

    public static IPAddress GetLocalIPByInterface(string iface) {
      IPAddresses ips = IPAddresses.GetIPAddresses(new string[] { iface });
      var it = ips.GetEnumerator();
      if (it.MoveNext()) {
        return it.Current as IPAddress;
      } else {
        throw new ArgumentException(string.Format(
          "No IP address detected for this interface: {0}.", iface), "iface");
      }
    }
  }
}
