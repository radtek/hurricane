using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Fushare {
  /**
   * Meta info of the data pieces.
   */
  public class FragmentationInfo : DictionaryData {
    #region Fileds
    private byte[] _base_key;
    private int _piece_length = 1024;  //in bytes, used when serializing
    private int _piece_num;
    #endregion

    #region Properties
    /**
     * This base key might or might not be the same as the actual
     * "key" under which the header is stored. 
     * It is just a base name
     * that all data pieces have in common and can extend with.
     */
    public byte[] BaseKey {
      get {
        return _base_key;
      }
      set {
        _base_key = value;
      }
    }

    public string BaseKeyUTF8String {
      get {
        return Encoding.UTF8.GetString(_base_key);
      }
    }

    /**
     * Optional when constructed from client
     */
    public int PieceNum {
      get {
        return _piece_num;
      }
      set {
        _piece_num = value;
      }
    }

    /**
     * The piece length affects all the length of the pieces except the last
     * one, which is irregular.
     * Optional when parsed from incoming value.
     */
    public int PieceLength {
      get {
        return _piece_length;
      }
      set {
        _piece_length = value;
      }
    }
    #endregion

    /**
     * @param baseKey this field is always mandatory.
     */
    public FragmentationInfo(string baseKey)
      : this(Encoding.UTF8.GetBytes(baseKey)) {
    }

    public FragmentationInfo(byte[] baseKey) {
      _base_key = baseKey;
    }

    /**
     * Default ctor. Properties need to be set after object constructed.
     */
    public FragmentationInfo() { }
    
    public override IDictionary ToDictionary() {
      IDictionary dict = InitIDictionaryFromType();
      dict.Add("base_key", _base_key);
      dict.Add("piece_num", _piece_num);
      dict.Add("piece_length", _piece_length);
      return dict;
    }

    public override void FromDictionary(IDictionary dict) {
      _base_key = (byte[])dict["base_key"];
      _piece_num = (int)dict["piece_num"];
      _piece_length = (int)dict["piece_length"];
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      sb.Append("FragmentationInfo:\n");
      sb.Append("BaseKey: " + Encoding.UTF8.GetString(BaseKey) + ";\n");
      sb.Append("PieceNum: " + PieceNum + ";\n");
      sb.Append("PieceLength: " + PieceLength+ " (bytes);");
      return sb.ToString();
    }
  }
}
