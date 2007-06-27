using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
#if FUSE_NUNIT
using NUnit.Framework;
#endif
using Mono.Unix;
using Brunet;
using Brunet.Dht;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace FuseDht {
  class FuseDhtUtil {
    string _s_dht_root;

    public FuseDhtUtil(string shadowdir) {
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
      FuseDhtConfigHandler.Write(Path.Combine(_s_dht_root, Constants.FILE_CONF), FuseDhtConfig.GetInstance());
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
      Debug.WriteLine(string.Format("Initializing keydir: {0}/{1}", basedirName, key));
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
      File.WriteAllText(Path.Combine(s_etcpath, Constants.FILE_PUT_MODE), config.putMode);
      File.WriteAllText(Path.Combine(s_etcpath, Constants.FILE_TTL), config.ttl.ToString());
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
            int int_invalidate;
            bool succ = Int32.TryParse(p, out int_invalidate);
            if (succ) {
              return Convert.ToBoolean(int_invalidate);
            } else {
              return Boolean.Parse(p);
            }
          case Constants.FILE_PUT_MODE:
            return Constants.GetPutMode(p);
          default:
            return null;
        }
      } catch (Exception e) {
        Debug.WriteLine(e);
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
        Debug.WriteLine("invalid regular expression");
        return false;
      }
      ret = reg.IsMatch(filename);

      Debug.WriteLine(string.Format("{0} IsValidName={1}", filename, ret));
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
      return Path.Combine(_s_dht_root, fusePath);
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
    public void TestIsValidFileName() {
      bool ret = FuseDhtUtil.IsValidMyFileName("ipaddress.txt");
      Assert.IsTrue(ret, "1st");
      ret = FuseDhtUtil.IsValidMyFileName("ipaddress.txt.swp");
      Assert.IsFalse(ret, "2nd");
    }
  }
#endif
}
