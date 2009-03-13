using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Fushare;
using Microsoft.Practices.Unity;
using Fushare.Services.BitTorrent;
using System.IO;
using Fushare.Web.Properties;

namespace Fushare.Web {
  // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
  // visit http://go.microsoft.com/?LinkId=9394801

  public class MvcApplication : System.Web.HttpApplication {
    public static void RegisterRoutes(RouteCollection routes) {
      routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

      routes.MapRoute(
          "Default", // Route name
          "{controller}/{nameSpace}/{name}/{action}", // URL with parameters
          new { 
            controller = "Home", action = "Index", nameSpace = "", name = "" 
          }  // Parameter defaults
      );

    }

    protected void Application_Start() {
      Logger.LoadConfig(Settings.Default.L4nConfigPath);

      RegisterRoutes(RouteTable.Routes);

      IUnityContainer container = new UnityContainer();
      // Configure for Dependecny Injection using Unity
      Bootstrapper.ConfigureUnityContainer(container);

      BitTorrentManager btManager = container.Resolve<BitTorrentManager>();
      btManager.Start();
    }
  }
}