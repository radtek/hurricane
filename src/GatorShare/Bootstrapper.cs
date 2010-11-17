using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using GatorShare.Services.BitTorrent;
using Microsoft.Practices.Unity;
using MonoTorrent.Client;
using GatorShare.Services;
using MonoTorrent.Client.Encryption;
using System.Net;
using GatorShare.External.DictionaryService;

namespace GatorShare {
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
      container.RegisterType<DictionaryServiceProxy>(
        new InjectionConstructor(typeof(DictionaryServiceBase),
          peerTtlSecs));
      #endregion

      #region DhtTracker
      // Singleton.
      container.RegisterType<DictionaryServiceTracker>(new ContainerControlledLifetimeManager(),
        new InjectionConstructor(
          typeof(DictionaryServiceProxy), 
          string.Format("http://*:{0}/", dhtTrackerListenerPort))); // listeningPrefix
      #endregion

      #region TorrentHelper
      // Singleton.
      var cacheBaseDirPath =
        ConfigurationManager.AppSettings["BitTorrentManagerBaseDirPath"];
      IPAddress ip = NetUtil.GetLocalIPByInterface(
        ConfigurationManager.AppSettings["DhtTrackerIface"]);
      int gsserverPort = Int32.Parse(ConfigurationManager.AppSettings["GSServerPort"]);
      var bittorrentCache = new BitTorrentCache(cacheBaseDirPath);
      container.RegisterInstance<BitTorrentCache>(bittorrentCache);
      var torrentHelper = new TorrentHelper(
        bittorrentCache, ip, dhtTrackerListenerPort, gsserverPort);
      container.RegisterInstance<TorrentHelper>(torrentHelper); 
      #endregion

      #region BitTorrentManager
      // Singleton.
      container.RegisterType<BitTorrentManager>(
        new ContainerControlledLifetimeManager(),
        new InjectionConstructor(
          typeof(BitTorrentCache),
          ConfigurationManager.AppSettings["BitTorrentManagerSelfNamespace"],
          typeof(DictionaryServiceProxy), 
          typeof(DictionaryServiceTracker), 
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
          typeof(BitTorrentCache),
          typeof(DictionaryServiceProxy),
          typeof(TorrentHelper),
          infoServerListeningPort)); 
      #endregion
    }
  }
}
