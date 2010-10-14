using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading;
using Fushare;
using Fushare.Configuration;
using Fushare.Filesystem;
using Microsoft.Practices.Unity;
using NDesk.Options;
using System.IO;
using System.Collections.Specialized;

namespace FushareApp {
  /// <summary>
  /// Program entry point.
  /// </summary>
  class FushareApp {
    #region Fields
    static readonly IDictionary _log_props;
    #endregion

    /// <summary>
    /// Initializes the <see cref="FushareApp"/> class and some static data structures
    /// of this program. It's executed before Main.
    /// </summary>
    static FushareApp() {
      var logconfig = ConfigurationManager.AppSettings["L4nConfigPath"];
      Logger.LoadConfig(logconfig);
      _log_props = Logger.PrepareLoggerProperties(typeof(FushareApp));
    }

    public static void Main(string[] args) {

      bool help = false;
      string mountPoint = null;
      string shadowDirPath = null;

      #region Argument parsing
      var options = new OptionSet()
      {
        { "m|mount-point=", "the path to mount the user level file system.",
          v => {
            mountPoint = v;  
            Logger.WriteLineIf(LogLevel.Info, _log_props, string.Format(
              "Mounting Point: {0}", v)); 
          }
        },
        { "S|shadow-path=", "the path to the shadow directory.",
          v => {
            shadowDirPath = v;
            Logger.WriteLineIf(LogLevel.Info, _log_props, string.Format(
              "Shadow Directory: {0}", v));
          }
        },
        { "h|help",  "show help information.", 
          v => help = v != null }
      };

      List<string> extra;
      try {
        extra = options.Parse(args);
      } catch (OptionException ex) {
        Console.WriteLine(ex.Message);
        PrintHelpAndExit(options);
        return;
      }

      if (help) {
        PrintHelpAndExit(options);
        return;
      }
      #endregion

      AppDomain.CurrentDomain.UnhandledException +=
        new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

      // Initiate virtual file system.
      IUnityContainer container = new UnityContainer();
      // register the arguments that FushareRedirectFSHelper depends on.
      container.RegisterInstance<ShadowDirPath>(new ShadowDirPath(shadowDirPath));
      container.RegisterType<FusharePathFactory>(new ContainerControlledLifetimeManager());
      container.RegisterType<FushareFileManager>(new ContainerControlledLifetimeManager());

      var proxy = new ServerProxy(ConfigurationManager.AppSettings[
        "FushareServerBaseAddress"]);
      container.RegisterInstance<ServerProxy>(proxy);

      // Register file system frontend.
      IFushareFilesys filesys;
      if (SysEnvironment.OSVersion == OS.Unix) {
        filesys = new FuseFilesys(mountPoint, shadowDirPath, 
          container.Resolve<FushareRedirectFSHelper>(), extra.ToArray());
        container.RegisterInstance<IFushareFilesys>(filesys);
      } else {
        throw new NotImplementedException("No Windows implemention for now.");
      }

      // Initiate other components.
      Bootstrapper.ConfigureUnityContainer(container, shadowDirPath);

      // Start.
      Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
        "Starting file system..."));

      // Need to be disposed.
      using (filesys as Mono.Fuse.FileSystem) {
        filesys.Start();
      }
    }

    static void CurrentDomain_UnhandledException(object sender,
      UnhandledExceptionEventArgs e) {
      Logger.WriteLineIf(LogLevel.Error, _log_props,
        string.Format("Unhandled Exception: {0}, IsTerminating: {1}",
        e.ExceptionObject, e.IsTerminating));
    }

    static void PrintHelpAndExit(OptionSet options) {
      Console.WriteLine("FushareApp options:");
      options.WriteOptionDescriptions(Console.Out);
      Environment.Exit(1);
    }
  }
}
