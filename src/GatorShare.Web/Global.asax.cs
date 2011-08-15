using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Routing;
using GatorShare;
using GatorShare.Services.BitTorrent;
using Microsoft.Practices.Unity;

namespace GatorShare.Web {
  // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
  // visit http://go.microsoft.com/?LinkId=9394801

  public class MvcApplication : System.Web.HttpApplication {
    static readonly IDictionary _log_props = 
      Logger.PrepareLoggerProperties(typeof(MvcApplication));

    public static void RegisterRoutes(RouteCollection routes) {
      routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

      routes.MapRoute(
        "Default", // Route name
        "{controller}/{nameSpace}/{name}/{action}", // URL with parameters
        new {
          controller = "Home",
          action = "Index"//,
        }  // Parameter defaults
      );

      routes.MapRoute(
        "Info",
        "{controller}/{action}",
        new {
          controller = "Home",
          action = "Index"
        }
      );
    }

    protected void Application_Start() {
      Logger.ConfigureLogger(WebConfigurationManager.AppSettings["L4nConfigPath"]);

      AppDomain.CurrentDomain.UnhandledException +=
        new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

      RegisterRoutes(RouteTable.Routes);

      IUnityContainer container = new UnityContainer();
      // Configure for Dependecny Injection using Unity
      Bootstrapper.ConfigureUnityContainer(container);

      var btSerice = container.Resolve<BitTorrentService>();
      btSerice.Start();
    }

    void CurrentDomain_UnhandledException(object sender, 
      UnhandledExceptionEventArgs e) {
      Logger.WriteLineIf(LogLevel.Error, _log_props,
        string.Format("Unhandled Exception: {0}, IsTerminating: {1}", 
        e.ExceptionObject, e.IsTerminating));
      }
  }
}