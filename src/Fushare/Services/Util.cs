using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace Fushare.Services {
  public static class Util {
    public static byte[] GetDhtKeyBytes(string nameSpace, string name) {
      string keyStr = GetDhtKeyString(nameSpace, name);
      return Encoding.UTF8.GetBytes(keyStr);
    }

    public static string GetDhtKeyString(string nameSpace, string name) {
      string keyStr = nameSpace + ":" + name;
      return keyStr;
    }

    public static string GetFileOrDirectoryName(string path) {
      return GetFileOrDirectoryName(path, true);
    }

    public static string GetFileOrDirectoryName(string path, bool checkPath) {
      if (checkPath) {
        return GetFileOrDirectoryInfo(path).Name;
      } else {
        string[] segments = path.Split(new char[] { Path.DirectorySeparatorChar }, 
          StringSplitOptions.RemoveEmptyEntries);
        return segments[segments.Length - 1];
      }
    }

    public static DirectoryInfo GetParent(string path, bool checkPath) {
      if (checkPath) {
        var filesysInfo = GetFileOrDirectoryInfo(path);
        string parent;
        if (filesysInfo is DirectoryInfo) {
          parent = (filesysInfo as DirectoryInfo).Parent.FullName;
        } else {
          parent = (filesysInfo as FileInfo).Directory.FullName;
        }
        return new DirectoryInfo(parent);
      } else {
        // Treat it as directory. The class doesn't check existence.
        return new DirectoryInfo(path).Parent;
      }
    }

    /// <summary>
    /// Gets the file or directory info.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <exception cref="ArgumentException">If the file or directory doesn't exist.
    /// </exception>
    public static FileSystemInfo GetFileOrDirectoryInfo(string path) {
      if (Directory.Exists(path)) {
        return new DirectoryInfo(path);
      } else if (File.Exists(path)) {
        return new FileInfo(path);
      } else {
        throw new ArgumentException("Neither a file nor a directory exists.", "path");
      }
    }

    public static void CheckPathRooted(string path, string paramName) {
      if (!Path.IsPathRooted(path)) {
        throw new ArgumentException("The path should be rooted", paramName); 
      }
    }

    public static void CheckPathRooted(string path) {
      if (!Path.IsPathRooted(path)) {
        throw new ArgumentException("The path should be rooted");
      }
    }

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
  }
}
