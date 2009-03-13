using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Fushare.Services;
using Fushare.Services.BitTorrent;
using Fushare.Services.Dht;
using Microsoft.Practices.Unity;
using System.Configuration;

namespace Fushare.Web {
  public static class Bootstrapper {
    public static void ConfigureUnityContainer(IUnityContainer container) {
      // Registrations
      container.RegisterType<IDhtService, DhtService>(
        new HttpContextLifetimeManager<IDhtService>());
      BigTableDht bigTableDht = new BigTableDht("Dennis84225@gmail.com", 
        "fushare");
      container.RegisterInstance<DhtBase>(bigTableDht);

      container.RegisterType<IBitTorrentService, BitTorrentService>(
        new HttpContextLifetimeManager<IBitTorrentService>());

      // Set factory
      ControllerBuilder.Current.SetControllerFactory(
        new UnityControllerFactory(container));

      Fushare.Bootstrapper.ConfigureUnityContainer(container);
    }
  }
}
