using System;
using System.Collections.Generic;
using System.Text;
using Brunet;
using System.Xml.Serialization;
using System.IO;
using NUnit.Framework;

namespace FuseDht {
  public class FuseDhtConfigHandler {
    public static string cfgPath;

    /// <summary>
    /// Set cfgPath before using this method
    /// </summary>    
    public static FuseDhtConfig Read() {
      if (string.IsNullOrEmpty(cfgPath)) {
        throw new Exception("You haven't set the config path yet");
      } else {
        return Read(cfgPath);
      }
    }
    
    /// <exception cref="">Could throw all kinds of xml parsing exceptions here</exception>
    public static FuseDhtConfig Read(string cfgPath) {
      XmlSerializer serializer = new XmlSerializer(typeof(FuseDhtConfig));
      FileStream fs = new FileStream(cfgPath, FileMode.Open);
      using (fs) {
        FuseDhtConfig config = (FuseDhtConfig)serializer.Deserialize(fs);
        fs.Close();
        return config;
      }
    }

    public static void Write(FuseDhtConfig config) {
      if (string.IsNullOrEmpty(cfgPath)) {
        throw new Exception("You haven't set the config path yet");
      } else {
        Write(cfgPath, config);
      }
    }
    
    public static void Write(string cfgPath, FuseDhtConfig config) {
      FileStream fs = new FileStream(cfgPath, FileMode.Create, FileAccess.Write);
      using (fs) {
        XmlSerializer serializer = new XmlSerializer(typeof(FuseDhtConfig));
        serializer.Serialize(fs, config);
        fs.Close(); 
      }
    }

    public static void Refresh() {
      FuseDhtConfig cfg = FuseDhtConfigHandler.Read();
      FuseDhtConfig.Refresh(cfg);
    }
  }

  public class FuseDhtConfig {
    [XmlElement(ElementName = Constants.FILE_TTL)]
    public int ttl;
    [XmlElement(ElementName = Constants.FILE_LIFESPAN)]
    public int lifespan;
    [XmlElement(ElementName = Constants.FILE_PUT_MODE)]
    public string putMode;
    [XmlElement(ElementName = Constants.FILE_INVALIDATE)]
    public bool invalidate;

    private static FuseDhtConfig _instance;
    
    public static FuseDhtConfig GetInstance() {
      if (_instance == null) {
        _instance = new FuseDhtConfig();
      }
      return _instance;
    }

    /// <summary>
    /// Used by Handler to refresh the singleton instance from file 
    /// </summary>
    internal static void Refresh(FuseDhtConfig c) {
      _instance = c;
    }
  }

  [TestFixture]
  public class FuseDhtConfigTest {
    [Test]
    [Ignore]
    public void TestWriteAndRead() {
      FuseDhtConfig config = new FuseDhtConfig();
      config.ttl = 5000;
      config.lifespan = 5000;
      config.putMode = PutMode.Recreate.ToString();
      FuseDhtConfigHandler.Write("./config", config);
      FuseDhtConfig actual = FuseDhtConfigHandler.Read("./config");
      Assert.AreEqual(config.putMode, actual.putMode);
    }

    [Test]
    [Ignore]
    public void TestReadPutMode() {
      FuseDhtConfig actual = FuseDhtConfigHandler.Read("./config");
      Assert.AreEqual(PutMode.Recreate, actual.putMode);
    }
  }
}
