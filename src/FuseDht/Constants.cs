using System;

namespace FuseDht {
  public enum PutMode {
    Put,
    Create,
    Recreate
  }

  /// <summary>
  /// Global Complie time constants
  /// </summary>
  public static class Constants {
    public const string DIR_DHT_ROOT = "dht";
    public const string DIR_ETC = "etc";
    public const string DIR_CACHE = "cache";
    public const string DIR_MY = "my";
    public const string DIR_KEY_DIR_GENERATOR = "KeyDirGenerator";
    
    //some file names have prefix "dot" but no variable has. The visibility of files is controlled here.
    public const string FILE_OFFLINE = ".offline";
    public const string FILE_UPLOADED = ".uploaded";
    public const string FILE_TTL = "ttl";
    public const string FILE_PUT_MODE = "put_mode";
    public const string FILE_CONF = "fusedht.conf";
    public const string FILE_INVALIDATE = "invalidate";
    public const string FILE_LIFESPAN = "lifespan";
    public const string FILE_DONE = ".done";
    //links
    public const string LN_SELF_BASEDIR = "myself";
    //default values
    public const int DEFAULT_TTL = 5000;	//seconds
    public const int DEFAULT_MAX_BYTES = 1000;	//bytes
    public const int DEFAULT_LIFESPAN = 1000;
    public const PutMode DEFAULT_PUT_MODE = PutMode.Put;
    public const bool DEFAULT_INVALIDATE = true;

    public const int DEFAULT_DN_LENGTH = 20;
    //
    public const int MAX_KEY_LENGTH = 150;  //keydir string length

    /**
     * level in directory structure
     * dht/basedir/keydir/etc/arg_files
     *                   /cache/data_files
     *                   /my/file_upload
     *    /fusedht.conf
     *    /KeyGenDir/basedir/bin_file
     */
    public const int LVL_DHT = 0;
    public const int LVL_CONF_FILE = 1;
    public const int LVL_BASE_DIR = 1;
    public const int LVL_KEY_DIR_GENTR = 1;
    public const int LVL_KEY_DIR = 2;
    public const int LVL_CACHE = 3;
    public const int LVL_ETC = 3;
    public const int LVL_MY = 3;
    public const int LVL_SUB_KEY_FOLDERS = 3;
    public const int LVL_BIN_KEY_FILE = 3;
    public const int LVL_DATA_FILE = 4;
    public const int LVL_ARG_FILE = 4;

    public const string DHT_VALUE_ATTR_FN = "filename";
    public const string DHT_VALUE_ATTR_VAL = "value";

    /// <summary>
    /// Convert string to PutMode
    /// </summary>
    /// <param name="pm">Could be put/create/recreate. Case Insensitive</param>
    /// <exception cref="ArgumentException">Invalid argument</exception>
    public static PutMode GetPutMode(string pm) {
      PutMode putmode;
      pm = pm.Trim();
      pm = pm.ToLower();
      switch (pm) {
        case "0":
        case "put":
        case "p":
          putmode = PutMode.Put;
          break;
        case "1":
        case "create":
        case "c":
          putmode = PutMode.Create;
          break;
        case "2":
        case "recreate":
        case "r":
          putmode = PutMode.Recreate;
          break;
        default:          
          throw new ArgumentException("No matched mode with the argument");
      }
      return putmode;
    }
  }

}
