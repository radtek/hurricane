using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Practices.Unity;
using Fushare.Filesystem;
using Fushare;
using System.Configuration;

namespace FushareApp {
  public class Bootstrapper {
    public static void ConfigureUnityContainer(IUnityContainer container, 
      string shadowDirPath) {

      container.RegisterType<FilesysEventDispatcher, UnityFilesysEventDispatcher>(
        new ContainerControlledLifetimeManager());

      // bittorrent: the name of the directory for the protocol.
      container.RegisterType<IFilesysEventHandler, BitTorrentFilesysEventHandler>(
        "bittorrent", new ContainerControlledLifetimeManager());

      // This ensures that event handlers are registered.
      container.Resolve<UnityFilesysEventDispatcher>();
    }
  }
}
