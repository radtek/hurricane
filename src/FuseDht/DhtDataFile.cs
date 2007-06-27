using System;
using System.Collections;
using System.Text;
using System.IO;
using Brunet.Dht;
using Brunet;
#if FUSE_NUNIT
using NUnit.Framework;
#endif

namespace FuseDht {
  /**
   * This class represents the file that should be generated from DhtGetResult.
   * It encapsulates the way to generate the filename, the way to give DhtGetResult
   * information via file contents and attributes
   */
  class DhtDataFile {
    public const int DEFAULT_FN_LENGTH = 20;
    
    private string _parent_dir_path;
    private DhtGetResult _dgr;
    private string _filename;

    public DhtDataFile(string parentDirPath, DhtGetResult dgr) {
      this._parent_dir_path = parentDirPath;
      this._dgr = dgr;
      this._filename = GenFileName();
    }

    public string Name {
      get { return _filename; }
    }

    public string FullName {
      get { return Path.Combine(_parent_dir_path, _filename); }
    }

    private string GenFileName() {
      return _dgr.age + "," + _dgr.ttl + "," + GenFilenameFromContent(_dgr.value, DEFAULT_FN_LENGTH);
    }


    public void WriteToFile() {
      string s_path = Path.Combine(_parent_dir_path, _filename);
      File.WriteAllBytes(s_path, _dgr.value);
      TimeSpan ts = new TimeSpan(0, 0, _dgr.age);
      DateTime c_time = DateTime.UtcNow - ts;
      File.SetCreationTimeUtc(s_path, c_time);
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
