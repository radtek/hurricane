using System;
using System.Collections;

namespace Fushare {
  /// <summary>
  /// Operating System enumeration.
  /// </summary>
  public enum OS {
    Unknown,
    /// <summary>
    /// Unix and Linux
    /// </summary>
    Unix,
    Windows,
    OSX
  }

  public class Environment {
    /// <summary>
    /// Contains the current operating system.
    /// </summary>
    public static readonly OS OSVersion;

    static Environment() {
      // Determine system version.
      int p = (int)System.Environment.OSVersion.Platform;
      int v = (int)System.Environment.OSVersion.Version.Major;

      if (System.Environment.OSVersion.Platform.ToString() == "Win32NT")
        OSVersion = OS.Windows;
      else if ((p == 4) || (p == 128)) {
        if (v < 8)
          OSVersion = OS.Unix;
        else
          OSVersion = OS.OSX;
      } else {
        OSVersion = OS.Unknown;
      }
    }
  }
}