using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Fushare.Services.BitTorrent;
using Microsoft.Practices.Unity;
using MonoTorrent.Client;
using Fushare.Services;
using MonoTorrent.Client.Encryption;
using System.Net;

namespace Fushare {
  public class Bootstrapper {
    public static void ConfigureUnityContainer(IUnityContainer container) {
      #region Common
      var dhtTrackerListenerPort =
        Int32.Parse(ConfigurationManager.AppSettings["DhtTrackerListeningPort"]); 
      var infoServerListeningPort = Int32.Parse(ConfigurationManager.AppSettings[
            "HttpPieceInfoServerListeningPort"]); // listeningPort
      #endregion

      #region TorrentSettings
      // Create the default settings which a torrent will have.
      // 4 Upload slots - a good ratio is one slot per 5kB of upload speed
      // 50 open connections - should never really need to be changed
      // Unlimited download speed - valid range from 0 -> int.Max
      // Unlimited upload speed - valid range from 0 -> int.Max
      TorrentSettings torrentDefaults = new TorrentSettings(4, 150, 0, 0);
      container.RegisterInstance<TorrentSettings>(torrentDefaults); 
      #endregion

      #region ClientEngine
      // DefaultListenPort = 52138;
      var engineSettings = new EngineSettings();
      engineSettings.PreferEncryption = false;
      engineSettings.AllowedEncryption = EncryptionTypes.All;
      var clientEngine = new ClientEngine(engineSettings);
      container.RegisterInstance<ClientEngine>(clientEngine); 
      #endregion

      #region DhtProxy
      var peerTtlSecs = 60 * 50;
      container.RegisterType<DhtProxy>(
        new InjectionConstructor(typeof(DhtBase),
          peerTtlSecs));
      #endregion

      #region DhtTracker
      // Singleton.
      container.RegisterType<DhtTracker>(new ContainerControlledLifetimeManager(),
        new InjectionConstructor(
          typeof(DhtProxy), 
          string.Format("http://*:{0}/", dhtTrackerListenerPort))); // listeningPrefix
      #endregion

      #region TorrentHelper
      // Singleton.
      var btManagerBaseDirPath =
        ConfigurationManager.AppSettings["BitTorrentManagerBaseDirPath"];
      var ip = NetUtil.GetLocalIPByInterface(
        ConfigurationManager.AppSettings["DhtTrackerIface"]);
      var torrentHelper = new TorrentHelper(
        BitTorrentManager.GetTorrentsDirPath(btManagerBaseDirPath), 
        string.Format("http://{0}:{1}/", ip.ToString(), dhtTrackerListenerPort));
      container.RegisterInstance<TorrentHelper>(torrentHelper); 
      #endregion

      #region BitTorrentManager
      // Singleton.
      container.RegisterType<BitTorrentManager>(
        new ContainerControlledLifetimeManager(),
        new InjectionConstructor(
          btManagerBaseDirPath,
          ConfigurationManager.AppSettings["BitTorrentManagerSelfNamespace"],
          typeof(DhtProxy), 
          typeof(DhtTracker), 
          typeof(ClientEngine),
          typeof(TorrentSettings), 
          typeof(TorrentHelper),
          Boolean.Parse(ConfigurationManager.AppSettings[
            "BitTorrentManagerStartSeedingAtStartup"])
          )); 
      #endregion

      #region IPieceInfoServer
      // Singleton.
      //container.RegisterType<IPieceInfoServer, HttpPieceInfoServer>(
      //    new ContainerControlledLifetimeManager(),
      //    new InjectionConstructor(
      //      infoServerListeningPort,
      //      typeof(PieceLevelTorrentManager))); 
      #endregion

      #region PieceLevelTorrentManager
      container.RegisterType<PieceLevelTorrentManager>(
        new ContainerControlledLifetimeManager(),
        new InjectionConstructor(
          typeof(BitTorrentManager),
          typeof(DhtProxy),
          typeof(TorrentHelper),
          infoServerListeningPort)); 
      #endregion
    }
  }
}
