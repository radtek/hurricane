using System;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Collections.Specialized;
#if UNIT_TEST
using NUnit.Framework; 
#endif

namespace GSeries {
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
    /// <summary>
    /// There is only one logger globally.
    /// </summary>
    static ILogger _logger;

    /// <summary>
    /// Initializes the <see cref="Logger"/> class.
    /// </summary>
    /// <remarks>
    /// To change to another logging tool, modify this method.
    /// </remarks>
    static Logger() {
      _logger = new Log4NetLogger();
    }

    public static void ConfigureLogger(string configFile) {
      _logger.ConfigureLogger(configFile);
    }

    public static void ConfigureLogger() {
      _logger.ConfigureLogger();
    }

    /// <summary>
    /// Prepares the logger properties.
    /// This method should be called by every class who wants to be logged to 
    /// prepare the logging properties.
    /// </summary>
    /// <param name="objType">Type of the object.</param>
    /// <returns>A dictionary with the properties of the logger.</returns>
    public static IDictionary PrepareLoggerProperties(Type objType) {
      return _logger.PrepareLoggerProperties(objType);
    }

    /// <summary>
    /// Prepares properties for some of the loggers used in this system that aren't
    /// associated with a particular class but named with a <c>string</c> typed 
    /// name.
    /// </summary>
    /// <param name="loggerName"></param>
    /// <returns></returns>
    public static IDictionary PrepareNamedLoggerProperties(string loggerName) {
      return _logger.PrepareNamedLoggerProperties(loggerName);
    }

    #region Log Methods

    public static void WriteLineIf(LogLevel level, IDictionary logProperties, 
      object message) {
      _logger.WriteLineIf(level, logProperties, message);
    }

    #endregion
  }

#if UNIT_TEST
  [TestFixture]
  public class LoggerTest {
    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void TestNonexistentConfigFile() {
      Logger.ConfigureLogger(@"C:\Temp\Nonexistent");
    }

    public void TestBasicConfig() {
      Logger.ConfigureLogger();
      var dict = Logger.PrepareNamedLoggerProperties("testLog");
      Logger.WriteLineIf(LogLevel.Verbose, dict, "test message");
    }
  }
#endif
}
