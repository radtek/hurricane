using System;
using System.Collections;
using System.Text;
using System.IO;
using Brunet.DistributedServices;
using Brunet;
#if FUSE_NUNIT
using NUnit.Framework;
#endif

namespace Fushare.Filesystem {
  /**
   * This class represents the file that should be generated from DhtGetResult.
   * It encapsulates the way to generate the filename, the way to give DhtGetResult
   * information via file contents and attributes
   */
  public class DhtDataFile {
    public const int DEFAULT_FN_LENGTH = 20;

    #region Fields

    /**
     * shadow path
     */
    string _parent_dir_path;
    int _age;
    int _ttl;
    byte[] _content;

    string _filename;
    string _real_filename;  //in case there are multiple values with the same _filename
    /*
     * For FuseDht inserted values
     * keys: filename, value
     */
    IDictionary _fuse_value;

    #endregion

    public DhtDataFile(string parentDirPath, byte[] serializedDdf, int age, int ttl) {
      this._parent_dir_path = parentDirPath;
      this._age = age;
      this._ttl = ttl;
      IDictionary val = FuseDhtUtil.ParseDhtValue(serializedDdf);
      if (val != null) {
        _fuse_value = val;
        _filename = (string)_fuse_value[Constants.DHT_VALUE_ATTR_FN];
        _real_filename = _filename;
        //_content = _fuse_value[Constants.DHT_VALUE_ATTR_VAL] as byte[];
        _content = _fuse_value[Constants.DHT_VALUE_ATTR_VAL] as byte[];
      } else {
        //Cannot be parsed as IDictionary
        _content = serializedDdf;
        this._filename = GenFileName();
        _real_filename = _filename;
      }
    }

    public DhtDataFile(string parentDirPath, DhtGetResult dgr)
        : this(parentDirPath, dgr.value, dgr.age, dgr.ttl) { }

    public int Age {
      get { return _age; }
    }

    public int TTL {
      get { return _ttl; }
    }

    public string Name {
      get { return _filename; }
    }

    public string FullName {
      get { return Path.Combine(_parent_dir_path, _filename); }
    }

    /**
     * @deprecated
     */
    private string GenFileName() {
      return _age + "," + _ttl + "," + GenFilenameFromContent(_content, DEFAULT_FN_LENGTH);
    }

    public void WriteToFile() {
      string s_path = Path.Combine(_parent_dir_path, _filename);
      if (File.Exists(s_path)) {
        string[] ss = s_path.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        int int_suffix;
        bool succ = Int32.TryParse(ss[ss.Length - 1], out int_suffix);
        if (succ) {
          //This file has an numeric extension (name after the last dot)
          int_suffix++;
          s_path = s_path.Substring(0, s_path.Length - ss[ss.Length - 1].Length)
                 + "." + int_suffix.ToString();
        } else {
          s_path += ".1";
        }
      }
      _real_filename = new FileInfo(s_path).Name;
      File.WriteAllBytes(s_path, _content);
    }

    private string GenFilenameFromContent(byte[] content, int fnLength) {
      string ret;
      string scontent = Encoding.UTF8.GetString(content);
      char[] ccontent = scontent.ToCharArray();
      char[] invalidfn = Path.GetInvalidFileNameChars();	//{ '\x00', '/' } for non-windows
      int contentLength = ccontent.Length;

      int ckLength = (fnLength > 0 ? fnLength : contentLength);
      bool valid = true;
      for (int i = 0; i < ckLength & i < contentLength; i++) {
        char c = ccontent[i];
        if (!(Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c) || Char.IsPunctuation(c) || Char.IsSymbol(c))) {
          valid = false;
          break;
        }
      }
      if (valid) {
        int cplength = (ckLength <= ccontent.Length ? ckLength : ccontent.Length);
        char[] cfilename = new char[cplength];
        Array.Copy(ccontent, cfilename, cplength);

        //ensure correctness
        for (int i = 0; i < cfilename.Length; i++) {
          if (Array.IndexOf(invalidfn, cfilename[i]) >= 0) {
            //invalid file name
            cfilename[i] = '_';
          }
        }
        ret = new string(cfilename);
      } else {
        //use base32
        string fname = Base32.Encode(content);
        if (fnLength < 0) {
          ret = fname;
        } else {
          ret = fname.Length <= ckLength ? fname : fname.Substring(0, ckLength);
        }
      }
      return ret;
    }
  }

#if FUSE_NUNIT
  [TestFixture]
  public class DhtDataFileTest {
    [Test]
    [Ignore]
    public void TestConstructAndWriteToFile() {
      string parent = "/tmp";
      byte[] value = new byte[50];
      Random rnd = new Random();
      rnd.NextBytes(value);
      DhtGetResult dgr = new DhtGetResult(value, 8000, 50000);
      DhtDataFile ddf = new DhtDataFile(parent, dgr);
      ddf.WriteToFile();
      DateTime expected = DateTime.UtcNow - new TimeSpan(0, 0, 8000);
      Assert.AreEqual(expected, File.GetCreationTimeUtc(ddf.FullName));
    }
  }
#endif
}
