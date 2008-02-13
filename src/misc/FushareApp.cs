using System;
using System.Collections;
using System.Text;
using Fushare.Filesystem;
using System.Threading;

namespace Fushare {
  class FushareApp {
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FushareApp));

    public static void Main(string[] args) {
      Logger.LoadConfig();
      try {
        using (FuseFS fs = new FuseFS()) {
          fs.InitAndStartFS(args);
        }
      } catch (System.Net.WebException) {
        Console.Error.WriteLine("Soap/XmlRpc Dht interface not started. Please start it first");
      } catch (Exception ex) {
        Console.Error.WriteLine("System cannot started");
        Logger.WriteLineIf(LogLevel.Fatal, _log_props, 
            ex);
        //if caught unhandled exception, terminates.
        Thread.CurrentThread.Abort();
      }
    }
  }
}
