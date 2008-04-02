using System;
using System.Collections;
using System.Text;
using Fushare.Filesystem;
using System.Threading;

namespace Fushare {
  /// <summary>
  /// Program entry point.
  /// </summary>
  class FushareApp {
    #region Fields
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FushareApp)); 
    #endregion

    public static void Main(string[] args) {
      Logger.LoadConfig();
      FushareConfigHandler.Read("fushare.config");
      try {
        using (FuseFS fs = new FuseFS()) {
          fs.InitAndStartFS(args);
        }
      } catch (System.Net.WebException) {
        Console.Error.WriteLine("Soap/XmlRpc Dht interface not started. Please start it first");
      } catch (Exception ex) {
        Console.Error.WriteLine("System cannot start. Aborting...");
        Logger.WriteLineIf(LogLevel.Fatal, _log_props, 
            ex);
        // If caught unhandled exception, terminates.
        Thread.CurrentThread.Abort();
      }
    }
  }
}
