// Copyright (c) 2011 Jiangyan Xu <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Config;
using System.IO;

namespace GatorShare {
  /// <summary>
  /// Implements Logger using log4net.
  /// </summary>
  /// <remarks>
  /// Not supposed to be used directly by modules outside this assembly.
  /// </remarks>
  class Log4NetLogger : ILogger {

    private void Log4NetWriteLineIf(LogLevel level, ILog log, object message) {
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
    }

    #region ILogger Methods
    public void WriteLineIf(LogLevel level, IDictionary logProperties, object message) {
      ILog log = logProperties["logger"] as ILog;
      if (log == null) {
        throw new ArgumentException("Invalid property dictionary.", "logProperties");
      }
      Log4NetWriteLineIf(level, log, message);
    }

    public void ConfigureLogger() {
      BasicConfigurator.Configure();
    }

    public void ConfigureLogger(string configFile) {
      Uri result;
      if (Uri.TryCreate(configFile, UriKind.Absolute, out result)) {
        XmlConfigurator.Configure(result);
        return;
      }

      if (!File.Exists(configFile)) {
        throw new ArgumentException("Invalid file path.", "configFile");
      }
      XmlConfigurator.Configure(new System.IO.FileInfo(configFile));
    }
    /// <summary>
    /// Prepares the logger properties.
    /// This method should be called by every class who wants to be logged to 
    /// prepare the logging properties.
    /// </summary>
    /// <param name="objType">Type of the object.</param>
    /// <returns>A dictionary with the properties of the logger.</returns>
    public IDictionary PrepareLoggerProperties(Type objType) {
      IDictionary dict = new System.Collections.Specialized.ListDictionary();
      ILog log = LogManager.GetLogger(objType);
      dict.Add("logger", log);
      return dict;
    }

    /// <summary>
    /// Prepares properties for some of the loggers used in this system that aren't
    /// associated with a particular class but named with a <c>string</c> typed 
    /// name.
    /// </summary>
    /// <param name="loggerName"></param>
    /// <returns></returns>
    public IDictionary PrepareNamedLoggerProperties(string loggerName) {
      IDictionary dict = new System.Collections.Specialized.ListDictionary();
      ILog log = LogManager.GetLogger(loggerName);
      dict.Add("logger", log);
      return dict;
    }
    #endregion
  }
}
