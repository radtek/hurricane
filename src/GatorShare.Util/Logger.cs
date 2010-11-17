using System;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Collections.Specialized;
#if LOG4NET
using log4net;
using log4net.Config;
#endif

namespace GatorShare {
  /// <summary>
  /// Log level used by multiple logging systems
  /// </summary>
  public enum LogLevel {
    /// <summary>
    /// No logs
    /// </summary>
    Off, 
    /// <summary>
    /// Fatal in log4net and Fail in <c>System.Diagnostics</c>
    /// </summary>
    Fatal, 
    /// <summary>
    /// Non-fatal errors
    /// </summary>
    Error, 
    /// <summary>
    /// Warning in both
    /// </summary>
    Warning, 
    /// <summary>
    /// Useful information
    /// </summary>
    Info, 
    /// <summary>
    /// Debug in log4net and verbose in <c>System.Diagnostics</c>
    /// </summary>
    Verbose, 
    /// <summary>
    /// All, not used in <c>System.Diagnostics</c>
    /// </summary>
    All
  }

  public class Logger {
    #region Switches used by Systen.Diagnostics
    public static readonly TraceSwitch TrackerLog = new TraceSwitch("trackerLog", "Logs in tracker");
    public static readonly BooleanSwitch FilesysLog = new BooleanSwitch("filesysLog", "Logs in FUSE File system"); 
    #endregion

    /// <summary>
    /// Types of logging systems
    /// </summary>
    public enum TraceType {
      Trace, Debug, Log4Net
    }

    /// <summary>
    /// Loads the config file from ./l4n.config
    /// </summary>
    public static void LoadConfig() {
      LoadConfig("l4n.config");
    }

    public static void LoadConfig(string configFile) {
      LoadConfig(configFile, new NameValueCollection());
    }

    /// <summary>
    /// Loads config file for the logger.
    /// </summary>
    /// <remarks>
    /// Currently only for log4net. 
    /// </remarks>
    public static void LoadConfig(string configFile, NameValueCollection properties) {
#if LOG4NET
      XmlConfigurator.Configure(new System.IO.FileInfo(configFile));
#endif
    }

    /**
     * Should be called by every class who wants to be logged to prepare the 
     * logging properties.
     * If LOG4NET, Logger of the specific type is attached
     * If DEBUG || TRACE, TraceSwitch is added
     */
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

    /// <summary>
    /// Prepares properties for some of the loggers used in this system that aren't
    /// associated with a particular class but named with a <c>string</c> typed 
    /// name.
    /// </summary>
    /// <param name="loggerName"></param>
    /// <returns></returns>
    public static IDictionary PrepareNamedLoggerProperties(string loggerName) {
      IDictionary dict = new System.Collections.Specialized.ListDictionary();
#if LOG4NET
      ILog log = LogManager.GetLogger(loggerName);
      dict.Add("logger", log); 
#endif
      return dict;
    }

    #region Log Methods

    /// <summary>
    /// Main method for multi-tool logging.
    /// </summary>
    /// <remarks>
    /// Which logging tool to use depends on the MACROS defined.
    /// </remarks>
    /// <param name="level">level of logging</param>
    /// <param name="props">properties of logging</param>
    /// <param name="args">
    /// <list type="table">
    /// <item>
    /// <term>
    /// DEBUG || TRACE
    /// </term>
    /// <description>
    /// args[0] object to log; args[1]: (optional) string category
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// LOG4NET
    /// </term>
    /// <description>
    /// args[0]: object message; args[1]: (optional) Exception
    /// </description>
    /// </item>
    /// </list>
    /// </param>
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
    #endregion
  }
}
