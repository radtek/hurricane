using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GatorShare {
  /// <summary>
  /// Provides utilities for configuring Fushare project.
  /// </summary>
  public static class ConfigurationUtil {
    public static string GetFullPath(string configRoot, string pathInConfig) {
      if (!Path.IsPathRooted(pathInConfig)) {
        return Path.Combine(configRoot, pathInConfig);
      } else {
        return pathInConfig;
      }
    }
  }
}
