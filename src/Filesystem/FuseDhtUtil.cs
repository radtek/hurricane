using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.IO;
using System.Diagnostics;
#if FUSE_NUNIT
using NUnit.Framework;
#endif
using Mono.Unix;
using Brunet;
using Brunet.DistributedServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Fushare.Filesystem {
  class FuseDhtUtil {
    #region Fields
    readonly string _s_dht_root;
    readonly string _shadowdir;
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FuseDhtUtil)); 
    #endregion

    public FuseDhtUtil(string shadowdir) {
      _shadowdir = shadowdir;
      _s_dht_root = Path.Combine(shadowdir, Constants.DIR_DHT_ROOT);
    }

    /**
     * exception thrown when path error 
     * init files placed outside a key folder which defines overall behavior
     */
    public void InitDhtRootFileStructure() {
      if (Directory.Exists(_s_dht_root)) {
        Directory.Delete(_s_dht_root, true);
      }

      //dht root
      Directory.CreateDirectory(_s_dht_root);

      //KeyDirGenerator
      Directory.CreateDirectory(Path.Combine(_s_dht_root, Constants.DIR_KEY_DIR_GENERATOR));

      //conf file
      FuseDhtConfigHandler.cfgPath = Path.Combine(_s_dht_root, Constants.FILE_CONF);
      FuseDhtConfigHandler.Write(FuseDhtConfig.GetInstance());

      //meta
      string s_meta = Path.Combine(Path.Combine(_shadowdir, Constants.DIR_DHT_ROOT), Constants.DIR_META);
      Directory.CreateDirectory(s_meta);
      string s_log = Path.Combine(Path.Combine(_shadowdir, Constants.DIR_DHT_ROOT), Constants.DIR_LOG);
      Directory.CreateDirectory(s_log);
    }

    public void CreateSelfBaseDir(string brunetAddr) {
      //create dir
      string self_dir = brunetAddr;
      if (brunetAddr.StartsWith("brunet:node:")) {
        self_dir = brunetAddr.Remove(0, 12);
      }
      string s_dirpath = Path.Combine(_s_dht_root, self_dir);
      Directory.CreateDirectory(s_dirpath);
      //link to it
      UnixSymbolicLinkInfo ln = new UnixSymbolicLinkInfo(Path.Combine(_s_dht_root, Constants.LN_SELF_BASEDIR));
      ln.CreateSymbolicLinkTo(s_dirpath);
    }

    public static string[] ParsePath(string f_path) {
      string[] s = f_path.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
      return s;
    }

    public void InitKeyDirStructure(string basedirName, string key) {
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("Initializing keydir: {0}/{1}", basedirName, key));
      string s_key_path = Path.Combine(_s_dht_root, Path.Combine(basedirName, key));

      DirectoryInfo di = new DirectoryInfo(s_key_path);
      di.Create();

      di.CreateSubdirectory(Constants.DIR_ETC);
      di.CreateSubdirectory(Constants.DIR_CACHE);
      di.CreateSubdirectory(Constants.DIR_MY);
      string s_etcpath = Path.Combine(s_key_path, Constants.DIR_ETC);
      FuseDhtConfig config = FuseDhtConfig.GetInstance();
      File.WriteAllText(Path.Combine(s_etcpath, Constants.FILE_INVALIDATE), config.invalidate.ToString());
      File.WriteAllText(Path.Combine(s_etcpath, Constants.FILE_LIFESPAN), config.lifespan.ToString());
      File.WriteAllText(Path.Combine(s_etcpath, Constants.FILE_PUT_MODE), config.put_mode.ToString());
      File.WriteAllText(Path.Combine(s_etcpath, Constants.FILE_TTL), config.ttl.ToString());
      File.WriteAllText(Path.Combine(s_etcpath, Constants.FILE_BLOCKING_RD), config.blocking_read.ToString());
      string s_cachepath = Path.Combine(s_key_path, Constants.DIR_CACHE);
      File.WriteAllText(Path.Combine(s_cachepath, Constants.FILE_DONE), "1");
    }

    /**
     * Try to parse param value from param files. If sucessfully read, return the value;
     * otherwise return null.
     * Exceptions are handled locally
     */
    public object ReadParam(string baseDirName, string key, string paramFilename) {
      try {
        string s_param_path = _s_dht_root + Path.DirectorySeparatorChar
                            + baseDirName + Path.DirectorySeparatorChar
                            + key + Path.DirectorySeparatorChar
                            + Constants.DIR_ETC + Path.DirectorySeparatorChar
                            + paramFilename;
        string p = File.ReadAllText(s_param_path);
        switch (paramFilename) {
          case Constants.FILE_LIFESPAN:
          case Constants.FILE_TTL:
            return Int32.Parse(p);
          case Constants.FILE_INVALIDATE:
          case Constants.FILE_BLOCKING_RD:
            int int_invalidate;
            bool succ = Int32.TryParse(p, out int_invalidate);
            if (succ) {
              return Convert.ToBoolean(int_invalidate);
            } else {
              return Boolean.Parse(p);
            }
          case Constants.FILE_PUT_MODE:
            return Constants.GetPutMode(p);
          case Constants.FILE_REFRESH:
            //this returns local time
            return Convert.ToDateTime(p);
          default:
            return null;
        }
      } catch (Exception e) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,e);
        return null;
      }
    }

    /**
     * Write to the param file. Create the file if it doesn't exist already
     */
    public void WriteToParamFile(string baseDirName, string key, string paramFilename, string val) {
      string s_param_path = _s_dht_root + Path.DirectorySeparatorChar
                            + baseDirName + Path.DirectorySeparatorChar
                            + key + Path.DirectorySeparatorChar
                            + Constants.DIR_ETC + Path.DirectorySeparatorChar
                            + paramFilename;
      File.WriteAllText(s_param_path, val);
    }

    /**
     * Generate the real key to put in dht from the baseDir and keyDir
     */
    public static string GenDhtKey(string basedirName, string key, string ipopNS) {
      string ret;

      string brunetID;
      if (basedirName.StartsWith("brunet:node:")) {
        brunetID = basedirName.Remove(0, 12);
      } else {
        brunetID = basedirName;
      }

      try {
        MemBlock decoded = MemBlock.Reference(Base32.Decode(brunetID));
        //try to parse, not intended to be assigned to anyone
        AddressParser.Parse(decoded);
        /* 
         * Can be parsed as brunetID
         */
        ret = basedirName + ":" + key;
      } catch (Exception) {
        /*
         * Not a valid 160 bit BigInteger
         */
        ret = basedirName + ":" + key + ":" + ipopNS; //replace this with real ns
      }

      return ret;
    }


    public static bool IsValidMyFileName(string filename) {
      bool ret = false;

      Regex reg;
      try {
        //reg = new Regex(@"^[A-Za-z0-9]*\.*[A-Za-z]*$");
        reg = new Regex(@"^[^.~].*[^.~]$");
      } catch {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,"invalid regular expression");
        return false;
      }
      ret = reg.IsMatch(filename);

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("{0} IsValidName={1}", filename, ret));
      return ret;
    }

    public string GetShadowPath(string fusePath) {
      if (fusePath.StartsWith(Path.DirectorySeparatorChar.ToString())) {
        //rooted
        fusePath = fusePath.Remove(0, 1);
      }

      if(fusePath.StartsWith(Constants.DIR_DHT_ROOT)) {
        fusePath = fusePath.Remove(0, Constants.DIR_DHT_ROOT.Length);
      }

      if (fusePath.StartsWith(Path.DirectorySeparatorChar.ToString())) {
        //rooted
        fusePath = fusePath.Remove(0, 1);
      }

      return Path.Combine(_s_dht_root, fusePath);
    }

    public string GetFusePath(string shadowPath) {
      if (!Path.IsPathRooted(shadowPath)) {
        throw new ArgumentException("rooted shadow path expected");
      }
      string ret = shadowPath.Remove(0, _shadowdir.Length);
      return ret;
    }

    /**
     * @param data in string format. string is directly put in the dictionary.
     * Note: conversion from big byte[] to string throws exception in mono1.2.6
     */
    public static byte[] GenerateDhtValue(string fileName, string data) {
      IDictionary dic = new ListDictionary();
      dic[Constants.DHT_VALUE_ATTR_FN] = fileName;
      dic[Constants.DHT_VALUE_ATTR_VAL] = data;
      System.IO.MemoryStream ms = new System.IO.MemoryStream();
      AdrConverter.Serialize(dic, ms);
      return ms.ToArray();
    }

    /**
     * @return null if deserialization fails.
     */
    public static IDictionary ParseDhtValue(byte[] data) {
      object o;
      try {
        o = AdrConverter.Deserialize(data);
      } catch (Exception) {
        //data that not generated by FuseDht
        return null;
      }

      //successful
      if (o is IDictionary) {
        IDictionary dic = (IDictionary)o;
        return dic;
      } else {
        return null;
      }
    }

    public static bool IsIgnoredFilename(string filename) {
      bool ret = false;

      if (new List<string>((ICollection<string>)Constants.SPECIAL_PATHS).Contains(filename)) {
        return true;
      }

      Regex reg;
      try {
        //reg = new Regex(@"^[A-Za-z0-9]*\.*[A-Za-z]*$");
        reg = new Regex(@".*\.so[.]*.*");
      } catch {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,"invalid regular expression");
        return false;
      }
      ret = reg.IsMatch(filename);

      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("{0} IsIgnoredFilename={1}", filename, ret));
      return ret;
    }

    /**
     * Remove upload, offline, offline.number, etc
     */
    public static string TrimPathExtension(string s_file_path) {
      if (s_file_path.EndsWith(Constants.FILE_OFFLINE)) {
        //file.offline
        s_file_path = s_file_path.Remove(s_file_path.Length - Constants.FILE_OFFLINE.Length);
      } else if (s_file_path.EndsWith(Constants.FILE_UPLOADED)) {
        //file.uploaded
        s_file_path = s_file_path.Remove(s_file_path.Length - Constants.FILE_UPLOADED.Length);
      } else if (s_file_path.Remove(s_file_path.Length - 2).EndsWith(Constants.FILE_OFFLINE)) {
        //file.offline.1
        s_file_path = s_file_path.Remove(s_file_path.Length - 2 - Constants.FILE_OFFLINE.Length);
      }
      return s_file_path;
    }
  }

#if FUSE_NUNIT
  [TestFixture]
  public class FuseDhtUtilTest {
    [Test]
    public void TestGenDhtKey() {
      string s2 = "brunet:node:4S3VFIJBYEC2BAADOTYHMDFYNU4MO3UM";
      string dk2 = FuseDhtUtil.GenDhtKey(s2, "key", "ipop_ns");
      Assert.AreEqual("brunet:node:4S3VFIJBYEC2BAADOTYHMDFYNU4MO3UM:key", dk2);
      string s1 = "basedir";
      string dk1 = FuseDhtUtil.GenDhtKey(s1, "key", "ipop_ns");
      Assert.IsTrue(dk1.EndsWith("ipop_ns"), "no ipop_ns");
    }

    [Test]
    [Ignore]
    public void TestIsValidFileName() {
      bool ret = FuseDhtUtil.IsValidMyFileName("ipaddress.txt");
      Assert.IsTrue(ret, "1st");
      ret = FuseDhtUtil.IsValidMyFileName(".ipaddress.txt.swp");
      Assert.IsFalse(ret, "2nd");
    }

    [Test]
    [Ignore]
    public void TestValueSerializationAndDeserialiazation() {
      byte[] val = FuseDhtUtil.GenerateDhtValue("file1.txt", "testing value~~~~~");
      IDictionary dic = FuseDhtUtil.ParseDhtValue(val);
      Assert.AreEqual("testing value~~~~~", Encoding.UTF8.GetString((byte[])dic[Constants.DHT_VALUE_ATTR_VAL]));
      Assert.AreEqual("file1.txt", (string)dic[Constants.DHT_VALUE_ATTR_FN]);
    }

    [Test]
    public void TestTrimPath() {
      string s1 = FuseDhtUtil.TrimPathExtension("/tmp/file.offline");
      Assert.AreEqual("/tmp/file", s1, "1");
      string s2 = FuseDhtUtil.TrimPathExtension("/tmp/file.offline.1");
      Assert.AreEqual("/tmp/file", s2, "2");
      string s3 = FuseDhtUtil.TrimPathExtension("/tmp/file.uploaded");
      Assert.AreEqual("/tmp/file", s3, "3");
      string s4 = FuseDhtUtil.TrimPathExtension("/tmp/file");
      Assert.AreEqual("/tmp/file", s4, "4");
    }
  }
#endif
}
