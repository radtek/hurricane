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

    public static void ParseDhtKeyString(string keyString, out string nameSpace, 
      out string name) {
      string[] segements = keyString.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
      if (segements.Length != 2) {
        throw new ArgumentException(
          @"Key string should have and only have one ':' as the delimitor.");
      }
      nameSpace = segements[0];
      name = segements[1];
    }

    public static string GetDhtKeyString(byte[] keyBytes) {
      return Encoding.UTF8.GetString(keyBytes);
    }

    public static byte[] GetDhtKeyBytes(string keyStr) {
      return Encoding.UTF8.GetBytes(keyStr);
    }
  }
}
