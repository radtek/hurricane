using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Xml.Serialization;

#if FUSHARE_NUNIT
using NUnit.Framework; 
#endif

using GatorShare.Services;
using GatorShare.External.DictionaryService;

namespace GatorShare.Configuration {
  /// <summary>
  /// Reads and writes FushareConfig
  /// </summary>
  public class FushareConfigHandler {
    #region Fields
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FushareConfigHandler));
    private static FushareConfig _config; 
    #endregion

    /// <summary>
    /// The current config object instance in the system. 
    /// </summary>
    /// <remarks>
    /// Call this property after you read it from config file.
    /// </remarks>
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

    /// <summary>
    /// Reads the config object from file and updates the current config object
    /// when it is now or if reload == true
    /// </summary>
    /// <param name="reload">Enforces reload of config file</param>
    public static FushareConfig Read(string configFile, bool reload) {
      if (_config != null && reload == false) {
        return _config;
      }
      using (FileStream fs = new FileStream(configFile, FileMode.Open)) {
        return Read(fs);
      }
    }

    /// <summary>
    /// Read config from file and updateds the current config object.
    /// </summary>
    private static FushareConfig Read(Stream configStream) {
      //Register event handler
      ServiceConfigSection.ServiceHandlersSet += new EventHandler(OnServiceHandlersSet);
      XmlSerializer serializer = new XmlSerializer(typeof(FushareConfig));
      FushareConfig config = (FushareConfig)serializer.Deserialize(configStream);
      _config = config;
      return config;
    }

    /// <summary>
    /// Updates the current config and writes it to the file.
    /// </summary>
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

    /// <summary>
    /// Registers ServiceConfigSection.ServiceHandlersSet event
    /// </summary>
    static void OnServiceHandlersSet(object sender, EventArgs e) {
      ServiceConfigSection config = (ServiceConfigSection)sender;
      foreach (ServiceHandlerMapping handler in config.serviceHandlers) {
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
