using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Ninject.Web.Mvc;
using Ninject;
using GSeries.ProvisionSupport;
using System.Web.Configuration;
using System.Reflection;
using System.IO;
using log4net.Config;

namespace GSeries.Web {
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class HurricaneWebApplication : NinjectHttpApplication {
        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected override void OnApplicationStarted() {
            base.OnApplicationStarted();
            XmlConfigurator.Configure(new FileInfo(WebConfigurationManager.AppSettings["Log4NetConfig"]));
            AreaRegistration.RegisterAllAreas();
            RegisterRoutes(RouteTable.Routes);
        }

        protected override Ninject.IKernel CreateKernel() {
            var kernel = new StandardKernel();
            kernel.Load(Assembly.GetExecutingAssembly());
            RegisterServices(kernel);
            return kernel;
        }

        private static void RegisterServices(IKernel kernel) {
            kernel.Bind<LocalFileService>().ToConstant(new LocalFileService(
                WebConfigurationManager.AppSettings["LocalFileService.BaseDir"]));
        }
    }
}