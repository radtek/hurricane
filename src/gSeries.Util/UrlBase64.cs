using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GSeries {
  /// <summary>
  /// Base64 for Url.
  /// </summary>
  /// <remarks>
  /// (+, /, =) => (-, _, ""). '=' isn't replaced with '.' because MS WebClient trims 
  /// trailing dots in a segment.</remarks>
  public static class UrlBase64 {
    public static string Encode(byte[] data) {
      var base64Str = Convert.ToBase64String(data);
      return base64Str.Replace('+', '-').Replace('/', '_').Replace("=", "");
    }

    public static byte[] Decode(string data) {
      // @TODO Solve padding issue.
      throw new NotImplementedException();
    }
  }
}
