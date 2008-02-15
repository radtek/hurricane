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
    private string _base_key;
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
    public string BaseKey {
      get {
        return _base_key;
      }
      set {
        _base_key = value;
      }
    }

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

    public override IDictionary ToDictionary() {
      IDictionary dict = InitIDictionaryFromType();
      dict.Add("base_key", _base_key);
      dict.Add("piece_num", _piece_num);
      dict.Add("piece_length", _piece_length);
      return dict;
    }

    public override void FromDictionary(IDictionary dict) {
      DictionaryDataType t = (DictionaryDataType)dict[DataTypeKey];
      DictionaryDataType expected = DictionaryDataUtil.GetDictionaryDataType(this.GetType());
      if (t != expected) {
        throw new ArgumentException(string.Format("Wrong type of dictionary data. Expected: {0}, Was: {1}."
            , expected.ToString(), t.ToString()));
      }
      _base_key = (string)dict["base_key"];
      _piece_num = (int)dict["piece_num"];
      _piece_length = (int)dict["piece_length"];
    }
  }
}
