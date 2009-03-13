using System;
using System.Collections;
using System.Text;
using System.Threading;
using Fushare;
using Fushare.Configuration;
using Fushare.Filesystem;
using System.Configuration;
using Microsoft.Practices.Unity;

namespace FushareApp {
  /// <summary>
  /// Program entry point.
  /// </summary>
  class FushareApp {
    #region Fields
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FushareApp));
    #endregion

    public static void Main(string[] args) {
      Logger.LoadConfig(ConfigurationManager.AppSettings["L4nConfigPath"]);
      AppDomain.CurrentDomain.UnhandledException +=
        new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
      IUnityContainer container = new UnityContainer();
      Bootstrapper.ConfigureUnityContainer(container);

      var filesys = container.Resolve<IFushareFilesys>();
      filesys.Start();
    }

    static void CurrentDomain_UnhandledException(object sender,
      UnhandledExceptionEventArgs e) {
      Logger.WriteLineIf(LogLevel.Error, _log_props,
        string.Format("Unhandled Exception: {0}, IsTerminating: {1}",
        e.ExceptionObject, e.IsTerminating));
    }
  }
}
