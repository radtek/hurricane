using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace GatorShare {
  /// <summary>
  /// Utility class for text manipulation.
  /// </summary>
  public static class TextUtil {
    public static string MD5Sum(byte[] input) {
      byte[] output = MD5.Create().ComputeHash(input);
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < output.Length; i++) {
        sb.Append(output[i].ToString("X2"));
      }
      return sb.ToString();
    }

    public static byte[] ToUTF8Base64(byte[] input) {
      string str = Convert.ToBase64String(input);
      return Encoding.UTF8.GetBytes(str);
    }

    public static byte[] FromUTF8Base64(byte[] input) {
      var str = Encoding.UTF8.GetString(input);
      return Convert.FromBase64String(str);
    }
  }
}
