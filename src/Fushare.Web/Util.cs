using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Fushare.Web {
  public class Util {
    public static void LogBeforeThrow(Exception toThrow, IDictionary logProperties) {
      Logger.WriteLineIf(LogLevel.Error, logProperties,
        string.Format("Log this exception before throwing to client. \n{0}", toThrow));
    }
  }
}
