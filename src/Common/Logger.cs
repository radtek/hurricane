using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace FuseSolution.Common {
  class Logger {
    public static TraceSwitch TrackerLog = new TraceSwitch("trackerLog", "Logs in tracker");
  }
}
