using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Routing;
using Fushare;
using Fushare.Services.BitTorrent;
using Microsoft.Practices.Unity;

namespace Fushare.Web {
  // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
  // visit http://go.microsoft.com/?LinkId=9394801

  public class MvcApplication : System.Web.HttpApplication {
    static readonly IDictionary _log_props = 
      Logger.PrepareLoggerProperties(typeof(MvcApplication));

    public static void RegisterRoutes(RouteCollection routes) {
      routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

      // "if condition": workaround for Mono 1.9.1
      // Otherwise: HTTP 500 "A route named 'Default' is already in the route 
      // collection. Route names must be unique. Parameter name: name"
      if (routes["Default"] == null) {
        routes.MapRoute(
              "Default", // Route name
              "{controller}/{nameSpace}/{name}/{action}", // URL with parameters
              new {
                controller = "Home",
                action = "Index",
                nameSpace = "",
                name = ""
              }  // Parameter defaults
          ); 
      }
    }

    protected void Application_Start() {
      Logger.LoadConfig(WebConfigurationManager.AppSettings["L4nConfigPath"]);

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