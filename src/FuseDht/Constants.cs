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
    public const string DIR_ETC = "etc";
    public const string DIR_CACHE = "cache";
    public const string DIR_MY = "my";
    public const string DIR_KEY_DIR_GENERATOR = "KeyDirGenerator";
    public const string DIR_BIN_KEY = "binarykey";
    //some file names have prefix "dot" but no variable has. The visibility of files is controlled here.
    public const string FILE_OFFLINE = ".offline";
    public const string FILE_TTL = "ttl";
    public const string FILE_PASSWD = "passwd";
    public const string FILE_PASSWORDS = ".passwords.xml";
    public const string FILE_MAX_BYTES = "max_bytes";
    public const string FILE_DQ_WAIT = "dq_wait";
    public const string FILE_PUT_MODE = "put_mode";
    public const string FILE_CONF = ".conf.xml";
    public const string FILE_INVALIDATE = "invalidate";
    public const string FILE_SYNC = ".sync";
    public const string FILE_LIFESPAN = "lifespan";
    public const string FILE_DONE = ".done";
    //passwords xml file
    public const string ELEM_PASSWORDS = "passwords";
    public const string ELEM_RECORD = "record";
    public const string ELEM_FILE = "file";
    public const string ELEM_PASSWD = "passwd";
    //default values
    public const int DEFAULT_TTL = 300;	//seconds
    public const int DEFAULT_MAX_BYTES = 1000;	//bytes
    public const int DEFAULT_LIFESPAN = 300;
    public const PutMode DEFAULT_PUT_MODE = PutMode.Put;
    public const bool DEFAULT_INVALIDATE = true;
    public const int DEFAULT_DQ_WAIT = 5000; //millisec
    //conf file
    public const string ELEM_CONFROOT = "defaultSettings";
    //
    public const int DEFAULT_FN_LENGTH = 20;
    public const int DEFAULT_DN_LENGTH = 20;

    //level in directory structure
    public const int LVL_KEY_DIR = 0;
    public const int LVL_CONF_FILE = 0;
    public const int LVL_KEY_DIR_GENTR = 0;
    public const int LVL_KEY = 1;
    public const int LVL_CACHE = 2;
    public const int LVL_ETC = 2;
    public const int LVL_MY = 2;
    public const int LVL_FOLDER = 2;
    public const int LVL_OFFLINE = 2;
    public const int LVL_DATA_FILE = 3;
    public const int LVL_ARG_FILE = 3;

    /// <summary>
    /// Convert string to PutMode
    /// </summary>
    /// <param name="pm">Could be put/create/recreate. Case Insensitive</param>
    /// <exception cref="ArgumentException">Invalid argument</exception>
    public static PutMode GetPutMode(string pm) {
      PutMode putmode;
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
