using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare {
  public static class UrlBase64 {
    public static string Encode(byte[] data) {
      var base64Str = Convert.ToBase64String(data);
      return base64Str.Replace('+', '-').Replace('/', '_').Replace('=', '.');
    }

    public static byte[] Decode(string data) {
      var urlbase64Str = data.Replace('-', '+').Replace('_', '/').Replace('.', '=');
      return Convert.FromBase64String(urlbase64Str);
    }
  }
}
