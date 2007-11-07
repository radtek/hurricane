using System;
using System.Collections.Generic;
using System.Text;
using Brunet;
using System.Xml.Serialization;
using System.IO;
#if FUSE_NUNIT
using NUnit.Framework;
#endif

namespace FuseSolution.FuseDht {
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
      try {
        FuseDhtConfig cfg = FuseDhtConfigHandler.Read();
        FuseDhtConfig.Refresh(cfg);
      } catch (Exception) {
        /*
         * If read failed, just don't update FuseDhtConfig
         */
      }
    }
  }

  public class FuseDhtConfig {
    [XmlElement(ElementName = Constants.FILE_TTL)]
    public int ttl;
    [XmlElement(ElementName = Constants.FILE_LIFESPAN)]
    public int lifespan;
    [XmlElement(ElementName = Constants.FILE_PUT_MODE)]
    public string put_mode;
    [XmlElement(ElementName = Constants.FILE_INVALIDATE)]
    public bool invalidate;
    [XmlElement(ElementName = Constants.FILE_BLOCKING_RD)]
    public bool blocking_read;

    private static FuseDhtConfig _instance;
    
    public static FuseDhtConfig GetInstance() {
      if (_instance == null) {
        _instance = new FuseDhtConfig();
      }
      return _instance;
    }

    public FuseDhtConfig() {
      this.ttl = Constants.DEFAULT_TTL;
      this.lifespan = Constants.DEFAULT_LIFESPAN;
      this.put_mode = Constants.DEFAULT_PUT_MODE.ToString();
      this.invalidate = Constants.DEFAULT_INVALIDATE;
      this.blocking_read = Constants.DEFAULT_BLOCKING_RD;
    }

    /**
     * Used by Handler to refresh the singleton instance from file 
     */
    internal static void Refresh(FuseDhtConfig c) {
      _instance = c;
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine(base.ToString());
      sb.AppendLine(string.Format("{0}={1}",Constants.FILE_TTL, ttl));
      sb.AppendLine(string.Format("{0}={1}",Constants.FILE_LIFESPAN, lifespan));
      sb.AppendLine(string.Format("{0}={1}", Constants.FILE_PUT_MODE, put_mode));
      sb.AppendLine(string.Format("{0}={1}", Constants.FILE_INVALIDATE, invalidate));
      sb.AppendLine(string.Format("{0}={1}", Constants.FILE_BLOCKING_RD, blocking_read));
      return sb.ToString();
    }
  }

#if FUSE_NUNIT
  [TestFixture]
  public class FuseDhtConfigTest {
    [Test]
    [Ignore]
    public void TestWriteAndRead() {
      FuseDhtConfig config = FuseDhtConfig.GetInstance();
      config.ttl = 5000;
      config.lifespan = 5000;
      config.put_mode = PutMode.Recreate.ToString();
      FuseDhtConfigHandler.Write("./config", config);
      FuseDhtConfig actual = FuseDhtConfigHandler.Read("./config");
      Assert.AreEqual(config.put_mode, actual.put_mode);
    }

    [Test]
    [Ignore]
    public void TestReadPutMode() {
      FuseDhtConfig actual = FuseDhtConfigHandler.Read("./config");
      Assert.AreEqual(PutMode.Recreate, actual.put_mode);
    }
  }
#endif
}
