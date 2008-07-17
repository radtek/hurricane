using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using System.IO;
#if FUSHARE_NUNIT
using NUnit.Framework; 
#endif
using Fushare.Services;

namespace Fushare {
  /// <summary>
  /// Main config file of fushare application.
  /// </summary>
  [XmlType("fushareConfig")]
  public class FushareConfig {
    public ServiceConfigSection serviceConfig;
    /// <summary>
    /// The prefix that DhtTracker passes to start System.Net.HttpListener
    /// </summary>
    public string dhtTrackerListeningPrefix;
  }

  /// <summary>
  /// Reads and writes FushareConfig
  /// </summary>
  public class FushareConfigHandler {
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FushareConfigHandler));
    private static FushareConfig _config;

    public static FushareConfig ConfigObject {
      get {
        if (_config == null) {
          throw new InvalidOperationException(
            "ConfigObject not set. Use Read method first.");
        } else {
          return _config;
        }
      }
    }

    public static FushareConfig Read(string configFile) {
      return Read(configFile, false);
    }

    /// <param name="reload">Enforces reload of config file</param>
    public static FushareConfig Read(string configFile, bool reload) {
      if (_config != null && reload == false) {
        return _config;
      }
      using (FileStream fs = new FileStream(configFile, FileMode.Open)) {
        return Read(fs);
      }
    }

    private static FushareConfig Read(Stream configStream) {
      //Register event handler
      ServiceConfigSection.ServiceHandlersSet += new EventHandler(OnServiceHandlersSet);
      XmlSerializer serializer = new XmlSerializer(typeof(FushareConfig));
      FushareConfig config = (FushareConfig)serializer.Deserialize(configStream);
      _config = config;
      return config;
    }

    public static void Write(string configFile,
      FushareConfig config) {
      _config = config;
      using (FileStream fs = new FileStream(configFile, FileMode.Create,
            FileAccess.Write)) {
        Write(fs, config);
      }
    }

    private static void Write(Stream configStream, FushareConfig config) {
      XmlSerializer serializer = new XmlSerializer(typeof(FushareConfig));
      serializer.Serialize(configStream, config);
    }

    static void OnServiceHandlersSet(object sender, EventArgs e) {
      ServiceConfigSection config = (ServiceConfigSection)sender;
      foreach (ServiceHandler handler in config.serviceHandlers) {
        Type type = Type.GetType(handler.type);
        Uri uri = new Uri(handler.uri);
        DictionaryServiceFactory.RegisterServiceType(type, uri);
        Logger.WriteLineIf(LogLevel.Info, _log_props, string.Format("{0} service at {1} registered", type.Name, uri.ToString()));
      }
    }
  }

#if FUSHARE_NUNIT
  [TestFixture]
  public class FushareConfigTest : FushareTestBase {
    [Test]
    public void TestSerializationAndDeserialization() {
      FushareConfig cfg = new FushareConfig();
      ServiceConfigSection svc = new ServiceConfigSection();
      ServiceHandler handler1 = new ServiceHandler();
      handler1.type = typeof(BrunetDht).FullName;
      handler1.uri = new Uri("http://localhost:15151/xd.rem").ToString();
      svc.serviceHandlers = new ServiceHandler[] { handler1 };
      cfg.serviceConfig = svc;
      MemoryStream ms = new MemoryStream();
      FushareConfigHandler.Write(ms, cfg);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props, Encoding.UTF8.GetString(ms.ToArray()));
      ms.Flush();
      ms.Position = 0;
      FushareConfig acutal = FushareConfigHandler.Read(ms);
      Assert.AreEqual("http://localhost:15151/xd.rem", acutal.serviceConfig.serviceHandlers[0].uri);
    }
  } 
#endif
}
