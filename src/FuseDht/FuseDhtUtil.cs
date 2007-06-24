using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Mono.Unix;

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
      string s_dirpath = Path.Combine(_s_dht_root, brunetAddr);
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
  }
}
