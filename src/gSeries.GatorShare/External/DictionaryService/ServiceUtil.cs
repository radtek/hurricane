/*
Copyright (c) 2010 Jiangyan Xu <jiangyan@ufl.edu>, University of Florida

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GSeries.External.DictionaryService {
  /// <summary>
  /// Provides utility functions for dictionary services.
  /// </summary>
  public static class ServiceUtil {
    public static byte[] GetDictKeyBytes(string nameSpace, string name) {
      string keyStr = GetDictKeyString(nameSpace, name);
      return GetDictKeyBytes(keyStr);
    }

    public static string GetDictKeyString(string nameSpace, string name) {
      string keyStr = nameSpace + "|" + name;
      return keyStr;
    }

    public static void ParseDictKeyString(string keyString, out string nameSpace, 
      out string name) {
      string[] segements = keyString.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
      if (segements.Length != 2) {
        throw new ArgumentException(
          @"Key string should have and only have one ':' as the delimitor.");
      }
      nameSpace = segements[0];
      name = segements[1];
    }

    public static string GetDictKeyString(byte[] keyBytes) {
      return Encoding.UTF8.GetString(keyBytes);
    }

    /// <summary>
    /// Gets the DHT key bytes from string.
    /// </summary>
    /// <remarks>UTF8 is used.</remarks>
    public static byte[] GetDictKeyBytes(string keyStr) {
      return Encoding.UTF8.GetBytes(keyStr);
    }

    /// <summary>
    /// Gets the URL compatible bytes for the key.
    /// </summary>
    /// <param name="content">The key.</param>
    /// <returns></returns>
    public static byte[] GetUrlCompatibleBytes(byte[] content) {
      String keyStr = GetUrlCompatibleString(content);
      return Encoding.UTF8.GetBytes(keyStr);
    }

    public static String GetUrlCompatibleString(byte[] content) {
      return UrlBase64.Encode(content);
    }
  }
}
