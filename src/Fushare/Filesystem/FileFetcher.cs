using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fushare.Filesystem {
  public class FileFetcher {
    bool IsFile(Uri uri) {
      if (uri.IsLoopback & uri.Scheme.Equals("file")) {
        return File.Exists(uri.LocalPath);
      } else {
        throw new NotImplementedException("Currently only Local Path acceptable");
      }
    }
  }
}
