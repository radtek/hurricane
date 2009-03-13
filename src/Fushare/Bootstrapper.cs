using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Fushare.Properties;
using Fushare.Services.BitTorrent;
using Microsoft.Practices.Unity;
using MonoTorrent.Client;
using Fushare.Services;

namespace Fushare {
  public class Bootstrapper {
    public static void ConfigureUnityContainer(IUnityContainer container) {
      
      // Create the default settings which a torrent will have.
      // 4 Upload slots - a good ratio is one slot per 5kB of upload speed
      // 50 open connections - should never really need to be changed
      // Unlimited download speed - valid range from 0 -> int.Max
      // Unlimited upload speed - valid range from 0 -> int.Max
      TorrentSettings torrentDefaults = new TorrentSettings(4, 150, 0, 0);
      container.RegisterInstance<TorrentSettings>(torrentDefaults);

      // ClientEngine.
      var engineSettings = new EngineSettings();
      var clientEngine = new ClientEngine(engineSettings);
      container.RegisterInstance<ClientEngine>(clientEngine);

      // DhtTracker. Singleton.
      container.RegisterType<DhtTracker>(new ContainerControlledLifetimeManager(), 
        new InjectionConstructor(typeof(DhtProxy), string.Format("http://*:{0}/", 
          Settings.Default.DhtTrackerListeningPort)));

      // TorrentHelper
      var btManagerBaseDirPath = Settings.Default.BitTorrentManagerBaseDirPath;
      var ipList = 
        Fushare.Services.Util.GetLocalIPByPrefix(Settings.Default.DhtTrackerIPPrefix);
      if (ipList.Count == 0) {
        throw new Exception("No suitable IP detected.");
      }
      var torrentHelper = new TorrentHelper(BitTorrentManager.GetTorrentsDirPath(btManagerBaseDirPath),
        string.Format("http://{0}:{1}/", ipList[0].ToString() ,Settings.Default.DhtTrackerListeningPort));
      container.RegisterInstance<TorrentHelper>(torrentHelper);

      container.RegisterType<DhtProxy>(
        new InjectionConstructor(typeof(DhtBase), 
          60 * 50));

      // BitTorrentManager.
      container.RegisterType<BitTorrentManager>(
        new ContainerControlledLifetimeManager(),
        new InjectionConstructor(Settings.Default.BitTorrentManagerBaseDirPath,
          Settings.Default.BitTorrentManagerSelfNamespace, typeof(DhtProxy),
          typeof(DhtTracker), typeof(ClientEngine), typeof(TorrentSettings), 
          typeof(TorrentHelper)));
    }
  }
}
