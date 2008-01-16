using System;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using Mono.Unix.Native;
using Brunet;
#if FUSE_DEBUG
using NUnit.Framework;
#endif

namespace Fushare.Filesystem {
  public class DhtMetadataFile {
    [XmlElement(DataType = "dateTime")]
    public DateTime create_time;
    [XmlElement(DataType = "dateTime")]
    public DateTime end_time;
    public string s_data_file_path;
    /**
     * Used when you renew the data
     */
    public int ttl;
    public string _meta_filename;

    public DateTime CreateTimeUtc {
      get { return create_time.ToUniversalTime(); }
    }

    public DateTime EndTimeUtc {
      get { return end_time.ToUniversalTime(); }
    }

    public DhtMetadataFile() { }

    public DhtMetadataFile(long createTime, long endTime, string sDataFilePath) {
      create_time = DateTime.FromBinary(createTime).ToUniversalTime();
      end_time = DateTime.FromBinary(endTime).ToUniversalTime();
      s_data_file_path = sDataFilePath;
    }
    
    public DhtMetadataFile(int ttl, string dataFilePath) {
      /*
       * Local time is used here because of .NET issues with DateTime Xml Serialization.
       */
      create_time = DateTime.Now;
      this.ttl = ttl;
      end_time = create_time + new TimeSpan(0, 0, this.GetTTLForMetaFile(ttl));
      s_data_file_path = dataFilePath;
      Stat buf;
      int r = Syscall.lstat(dataFilePath, out buf);
      if (r != -1) {
        _meta_filename = Convert.ToString(buf.st_ino);
      } else {
        throw new FuseDhtStructureException("An error occurred when lstat this path", s_data_file_path);
      }
    }

    /**
     * Dht statistics: usually 1-2 second for a put
     */
    private int GetTTLForMetaFile(int dhtTTL) {
      if (dhtTTL < 1800) {
        //half an hour
        return Convert.ToInt32(dhtTTL * 0.5);
      } else if (dhtTTL < 10800) {
        //3 hours
        return Convert.ToInt32(dhtTTL * 0.7);
      } else {
        return Convert.ToInt32(dhtTTL * 0.9);
      }
    }
  }


  public class DhtMetadataFileHandler {
    public static void WriteAsXml(string sParentDirPath, DhtMetadataFile data) {
      string path = Path.Combine(sParentDirPath, data._meta_filename);
      FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
      using (fs) {
        XmlSerializer serializer = new XmlSerializer(typeof(DhtMetadataFile));
        serializer.Serialize(fs, data);
        fs.Close();
      }
    }

    public static DhtMetadataFile ReadFromXml(string sPath) {
      XmlSerializer serializer = new XmlSerializer(typeof(DhtMetadataFile));
      FileStream fs = new FileStream(sPath, FileMode.Open);
      using (fs) {
        DhtMetadataFile file = (DhtMetadataFile)serializer.Deserialize(fs);
        fs.Close();
        return file;
      }
    }

    public static void WriteAsAdr(DhtMetadataFile data, string sParentDirPath) {
      Hashtable ht = new Hashtable();
      ht.Add("create_time", data.create_time.ToBinary());
      ht.Add("end_time", data.end_time.ToBinary());
      ht.Add("s_data_file_path", data.s_data_file_path);
      string path = Path.Combine(sParentDirPath, data._meta_filename);
      FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
      using (fs) {
        AdrConverter.Serialize(ht, fs);
        fs.Close();
      }
    }

    public static DhtMetadataFile ReadFromAdr(string sPath) {
      FileStream fs = new FileStream(sPath, FileMode.Open);
      using (fs) {
        IDictionary dic = (IDictionary)AdrConverter.Deserialize(fs);
        fs.Close();
        DhtMetadataFile file = new DhtMetadataFile(
            (long)dic["create_time"], (long)dic["end_time"], dic["s_data_file_path"] as string);
        return file;
      }
    }
  }

#if FUSE_DEBUG
  [TestFixture]
  public class DhtMetadataFileTest {
    [TestFixtureSetUp]
    public void SetUp() {
      File.WriteAllText("/tmp/test.txt", "contents");
    }
    
    [Test]
    public void TestWRXml() {
      DhtMetadataFile file = new DhtMetadataFile(5000, "/tmp/test.txt");
      DhtMetadataFileHandler.WriteAsXml("/tmp", file);
      string p = Path.Combine("/tmp", file._meta_filename);
      DhtMetadataFile file1 = DhtMetadataFileHandler.ReadFromXml(p);
      Assert.AreEqual(file.create_time, file1.create_time);
      Assert.AreEqual(file.end_time, file1.end_time);
    }
  }
#endif
}
