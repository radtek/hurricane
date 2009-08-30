using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Fushare {
  public static class UriUtil {

    /// <summary>
    /// Combines the URIs. It allows the base URI to include base path besides the 
    /// hostname.
    /// </summary>
    /// <param name="baseUri">The base URI.</param>
    /// <param name="relativeUri">The relative URI.</param>
    /// <returns></returns>
    public static Uri CombineUris(Uri baseUri, Uri relativeUri) {
      UriBuilder ub = new UriBuilder(baseUri);
      ub.Path = CombinePaths(ub.Path, relativeUri);
      return ub.Uri;
    }

    public static string CombinePaths(string baseFullPath, Uri relativeUri) {
      IOUtil.CheckPathRooted(baseFullPath, "baseFullPath");
      if (relativeUri.IsAbsoluteUri) {
        throw new ArgumentException("Uri should be relative", "relativeUri");
      }

      // Could be file:///aaa/bbb
      var absoluteUri = new Uri(new Uri(@"file:///"), relativeUri);
      // Could be C:\ccc\aaa/bbb (Windows) or /ccc/aaa/bbb
      // LocalPath always starts with slash.
      var fullPath = Path.Combine(baseFullPath, absoluteUri.LocalPath.TrimStart('/'));
      // Get a "canonical" representation.
      // Could be C:\ccc\aaa\bbb or /ccc/aaa/bbb
      return Path.GetFullPath(fullPath);
    }
  }
}
