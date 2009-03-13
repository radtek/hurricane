using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  public static class ServiceUtil {
    public static byte[] GetDhtKeyBytes(string nameSpace, string name) {
      string keyStr = GetDhtKeyString(nameSpace, name);
      return Encoding.UTF8.GetBytes(keyStr);
    }

    public static string GetDhtKeyString(string nameSpace, string name) {
      string keyStr = nameSpace + ":" + name;
      return keyStr;
    }
  }
}
