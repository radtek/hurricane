using System;
using System.Collections;
using System.Text;
using System.Diagnostics;
#if LOG4NET
using log4net;
using log4net.Config;
#endif

namespace FuseSolution.Common {
  public enum LogLevel {
    Off, Fatal, Error, Warning, Info, Verbose, All
  }

  public class Logger {
    public static readonly TraceSwitch TrackerLog = new TraceSwitch("trackerLog", "Logs in tracker");

    public enum TraceType {
      Trace, Debug, Log4Net
    }

    public static void LoadConfig() {
      LoadConfig("l4n.config");
    }

    public static void LoadConfig(string configFile) {
      XmlConfigurator.Configure(new System.IO.FileInfo(configFile));
    }

    public static IDictionary PrepareLoggerProperties(Type objType) {
      IDictionary dic = new System.Collections.Specialized.ListDictionary();
#if LOG4NET
      ILog log = LogManager.GetLogger(objType);
      dic.Add("logger", log);
#endif

#if (DEBUG || TRACE)
      TraceSwitch ts = null;
      if (objType.Namespace.EndsWith("Tracker")) {
        ts = Logger.TrackerLog;
      } else {
        //Is this the right thing to do?
        ts = new TraceSwitch("Undefined", "Undefined");
      }
      dic.Add("trace_switch", ts);
#endif
      return dic;
    }

    #region Log Methods
    /**
     * Well, this really could be optimized in terms of speed, but let's just keep the simplicity
     */
    public static void WriteLineIf(LogLevel level, IDictionary props, params object[] args) {
      if (args.Length == 0) {
        throw new ArgumentException("Object(s) to log needed.");
      }
#if TRACE
      TraceWriteLineIf(level, props["trace_switch"] as TraceSwitch, args);
#endif
#if DEBUG
      DebugWriteLineIf(level, props["trace_switch"] as TraceSwitch, args);
#endif
#if LOG4NET
      ILog log = props["logger"] as ILog;
      Log4NetWriteLineIf(level, log, args);
#endif
    }


#if LOG4NET
    /**
     * @param log ILog instance
     * @param args args[0]: ; args[1]: object message; log[2]: (optional) Exception
     */
    private static void Log4NetWriteLineIf(LogLevel level, ILog log, params object[] args) {
      if (args.Length == 1) {
        //void Fatal(object message);
        object message = args[0];
        switch (level) {
          case LogLevel.Fatal:
            log.Fatal(message);
            break;
          case LogLevel.Error:
            log.Error(message);
            break;
          case LogLevel.Warning:
            log.Warn(message);
            break;
          case LogLevel.Info:
            log.Info(message);
            break;
          case LogLevel.Verbose:
          case LogLevel.All:
            log.Debug(message);
            break;
          default:
            break;
        }
      } else if (args.Length == 2) {
        object message = args[0];
        Exception e = args[1] as Exception;
        switch (level) {
          case LogLevel.Fatal:
            log.Fatal(message, e);
            break;
          case LogLevel.Error:
            log.Error(message, e);
            break;
          case LogLevel.Warning:
            log.Warn(message, e);
            break;
          case LogLevel.Info:
            log.Info(message, e);
            break;
          case LogLevel.Verbose:
          case LogLevel.All:
            log.Debug(message, e);
            break;
          default:
            break;
        }
      }
    }
#endif

#if TRACE
    /**
     * @param ts TraceSwitch that defined the logging conditions
     * @param args args[0] TraceSwitch; args[1]: object value; args[2]: (optional) string category
     */
    private static void TraceWriteLineIf(LogLevel level, TraceSwitch ts, params object[] args) {
      //TraceSwitch
      if (args.Length == 1) {
        object val = args[0];
        switch (level) {
          case LogLevel.Error:
            Trace.WriteLineIf(ts.TraceError, val);
            break;
          case LogLevel.Warning:
            Trace.WriteLineIf(ts.TraceWarning, val);
            break;
          case LogLevel.Info:
            Trace.WriteLineIf(ts.TraceInfo, val);
            break;
          case LogLevel.Verbose:
            Trace.WriteLineIf(ts.TraceVerbose, val);
            break;
          case LogLevel.Fatal:
            Trace.Fail(val as string);
            break;
          default:
            break;
        }
      } else if (args.Length == 2) {
        object val = args[0];
        if (args[1] is string) {
          string cat = args[1] as string;
          switch (level) {
            case LogLevel.Error:
              Trace.WriteLineIf(ts.TraceError, val, cat);
              break;
            case LogLevel.Warning:
              Trace.WriteLineIf(ts.TraceWarning, val, cat);
              break;
            case LogLevel.Info:
              Trace.WriteLineIf(ts.TraceInfo, val, cat);
              break;
            case LogLevel.Verbose:
              Trace.WriteLineIf(ts.TraceVerbose, val, cat);
              break;
            case LogLevel.Fatal:
              string detail_message = cat;
              Trace.Fail(val as string, detail_message);
              break;
            default:
              break;
          }
        } else if (args[1] is Exception) {
          Exception e = args[1] as Exception;
          switch (level) {
            case LogLevel.Error:
              Trace.WriteLineIf(ts.TraceError, val);
              Trace.WriteLineIf(ts.TraceError, e);
              break;
            case LogLevel.Warning:
              Trace.WriteLineIf(ts.TraceWarning, val);
              Trace.WriteLineIf(ts.TraceWarning, e);
              break;
            case LogLevel.Info:
              Trace.WriteLineIf(ts.TraceInfo, val);
              Trace.WriteLineIf(ts.TraceInfo, e);
              break;
            case LogLevel.Verbose:
              Trace.WriteLineIf(ts.TraceVerbose, val);
              Trace.WriteLineIf(ts.TraceVerbose, e);
              break;
            default:
              break;
          }
        }
      }
    }
#endif

#if DEBUG
    private static void DebugWriteLineIf(LogLevel level, TraceSwitch ts, params object[] args) {
      if (args.Length == 1) {
        object val = args[0];
        switch (level) {
          case LogLevel.Error:
            Debug.WriteLineIf(ts.TraceError, val);
            break;
          case LogLevel.Warning:
            Debug.WriteLineIf(ts.TraceWarning, val);
            break;
          case LogLevel.Info:
            Debug.WriteLineIf(ts.TraceInfo, val);
            break;
          case LogLevel.Verbose:
            Debug.WriteLineIf(ts.TraceVerbose, val);
            break;
          case LogLevel.Fatal:
            Debug.Fail(val as string);
            break;
          default:
            break;
        }
      } else if (args.Length == 2) {
        object val = args[0];
        if (args[1] is string) {
          string cat = args[1] as string;
          switch (level) {
            case LogLevel.Error:
              Debug.WriteLineIf(ts.TraceError, val, cat);
              break;
            case LogLevel.Warning:
              Debug.WriteLineIf(ts.TraceWarning, val, cat);
              break;
            case LogLevel.Info:
              Debug.WriteLineIf(ts.TraceInfo, val, cat);
              break;
            case LogLevel.Verbose:
              Debug.WriteLineIf(ts.TraceVerbose, val, cat);
              break;
            case LogLevel.Fatal:
              string detail_message = cat;
              Debug.Fail(val as string, detail_message);
              break;
            default:
              break;
          }
        } else if (args[1] is Exception) {
          Exception e = args[1] as Exception;
          switch (level) {
            case LogLevel.Error:
              Debug.WriteLineIf(ts.TraceError, val);
              Debug.WriteLineIf(ts.TraceError, e);
              break;
            case LogLevel.Warning:
              Debug.WriteLineIf(ts.TraceWarning, val);
              Debug.WriteLineIf(ts.TraceWarning, e);
              break;
            case LogLevel.Info:
              Debug.WriteLineIf(ts.TraceInfo, val);
              Debug.WriteLineIf(ts.TraceInfo, e);
              break;
            case LogLevel.Verbose:
              Debug.WriteLineIf(ts.TraceVerbose, val);
              Debug.WriteLineIf(ts.TraceVerbose, e);
              break;
            default:
              break;
          }
        }
      }
    }
#endif
  }
    #endregion
}
