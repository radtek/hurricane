using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  public static class ServiceUtil {
    public static byte[] GetDhtKeyBytes(string nameSpace, string name) {
      string keyStr = GetDhtKeyString(nameSpace, name);
      return GetDhtKeyBytes(keyStr);
    }

    public static string GetDhtKeyString(string nameSpace, string name) {
      string keyStr = nameSpace + ":" + name;
      return keyStr;
    }

    public static string GetDhtKeyString(byte[] keyBytes) {
      return Encoding.UTF8.GetString(keyBytes);
    }

    public static byte[] GetDhtKeyBytes(string keyStr) {
      return Encoding.UTF8.GetBytes(keyStr);
    }
  }
}
