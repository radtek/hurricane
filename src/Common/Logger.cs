using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using log4net;

namespace FuseSolution.Common {
  public enum LogLevel {
    Off, Fatal, Error, Warning, Info, Verbose, All
  }
  
  class Logger {
    public static TraceSwitch TrackerLog = new TraceSwitch("trackerLog", "Logs in tracker");

    public enum TraceType {
      Trace, Debug, Log4Net
    }

    #region Log Methods
    /**
     * Well, this really could be optimized in terms of speed, but let's just keep the simplicity
     */
    public static void WriteLineIf(LogLevel level, params object[] args) {
#if TRACE
      TraceWriteLineIf(level, args);
#endif
#if DEBUG
      DebugWriteLineIf(level, args);
#endif
#if LOG4NET
      Log4NetWriteLineIf(level, args);
#endif
    }

    /**
     * @param args args[0]: ILog instance; args[1]: object message; log[2]: (optional) Exception
     */
    private static void Log4NetWriteLineIf(LogLevel level, params object[] args) {
      ILog log = (ILog)args[0];
      if (args.Length == 2) {
        //void Fatal(object message);
        switch (level) {
          case LogLevel.Fatal:
            log.Fatal(args[1]);
            break;
          case LogLevel.Error:
            log.Error(args[1]);
            break;
          case LogLevel.Warning:
            log.Warn(args[1]);
            break;
          case LogLevel.Info:
            log.Info(args[1]);
            break;
          case LogLevel.Verbose:
          case LogLevel.All:
            log.Debug(args[1]);
            break;
          default:
            break;
        }
      } else if (args.Length == 3) {
        switch (level) {
          case LogLevel.Fatal:
            log.Fatal(args[1], args[2] as Exception);
            break;
          case LogLevel.Error:
            log.Error(args[1], args[2] as Exception);
            break;
          case LogLevel.Warning:
            log.Warn(args[1], args[2] as Exception);
            break;
          case LogLevel.Info:
            log.Info(args[1], args[2] as Exception);
            break;
          case LogLevel.Verbose:
          case LogLevel.All:
            log.Debug(args[1], args[2] as Exception);
            break;
          default:
            break;
        }
      }
    }


    /**
     * @param args args[0] TraceSwitch; args[1]: object value; args[2]: (optional) string category
     */
    private static void TraceWriteLineIf(LogLevel level, params object[] args) {
      //TraceSwitch
      TraceSwitch ts = (TraceSwitch)args[0];
      if (args.Length == 2) {
        switch (level) {
          case LogLevel.Error:
            Trace.WriteLineIf(ts.TraceError, args[1]);
            break;
          case LogLevel.Warning:
            Trace.WriteLineIf(ts.TraceWarning, args[1]);
            break;
          case LogLevel.Info:
            Trace.WriteLineIf(ts.TraceInfo, args[1]);
            break;
          case LogLevel.Verbose:
            Trace.WriteLineIf(ts.TraceVerbose, args[1]);
            break;
          default:
            break;
        }
      } else if (args.Length == 3) {
        switch (level) {
          case LogLevel.Error:
            Trace.WriteLineIf(ts.TraceError, args[1], args[2] as string);
            break;
          case LogLevel.Warning:
            Trace.WriteLineIf(ts.TraceWarning, args[1], args[2] as string);
            break;
          case LogLevel.Info:
            Trace.WriteLineIf(ts.TraceInfo, args[1], args[2] as string);
            break;
          case LogLevel.Verbose:
            Trace.WriteLineIf(ts.TraceVerbose, args[1], args[2] as string);
            break;
          default:
            break;
        }
      }
    }

    private static void DebugWriteLineIf(LogLevel level, params object[] args) {
      //TraceSwitch
      TraceSwitch ts = (TraceSwitch)args[0];
      if (args.Length == 2) {
        switch (level) {
          case LogLevel.Error:
            Debug.WriteLineIf(ts.TraceError, args[1]);
            break;
          case LogLevel.Warning:
            Debug.WriteLineIf(ts.TraceWarning, args[1]);
            break;
          case LogLevel.Info:
            Debug.WriteLineIf(ts.TraceInfo, args[1]);
            break;
          case LogLevel.Verbose:
            Debug.WriteLineIf(ts.TraceVerbose, args[1]);
            break;
          default:
            break;
        }
      } else if (args.Length == 3) {
        switch (level) {
          case LogLevel.Error:
            Trace.WriteLineIf(ts.TraceError, args[1], args[2] as string);
            break;
          case LogLevel.Warning:
            Trace.WriteLineIf(ts.TraceWarning, args[1], args[2] as string);
            break;
          case LogLevel.Info:
            Trace.WriteLineIf(ts.TraceInfo, args[1], args[2] as string);
            break;
          case LogLevel.Verbose:
            Trace.WriteLineIf(ts.TraceVerbose, args[1], args[2] as string);
            break;
          default:
            break;
        }
      }
    }
  } 
    #endregion
}
