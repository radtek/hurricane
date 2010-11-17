using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace GatorShare {
  /**
   * A decorator that enables the fragmentation functionality of data.
   */
  public class FragmentableData : DictionaryDataDecorator {
    private FragmentationInfo _info;
    private IList<byte[]> _fragments_in_bytes;

    #region Properties
    public FragmentationInfo FrgmtInfo {
      get {
        return _info;
      }
      set {
        _info = value;
      }
    }

    public bool Fragmented {
      get {
        return (_fragments_in_bytes != null);
      }
    }

    public IList<byte[]> FragmentedDataInBytes {
      get {
        return _fragments_in_bytes;
      }
    }
    #endregion

    #region Constructors
    public FragmentableData() : base() { }

    /**
     * Data that could be fragmented
     */
    public FragmentableData(DictionaryData data) : base(data) { } 
    #endregion

    public void Fragment() {
      _fragments_in_bytes = ToFragments();
      _info.PieceNum = _fragments_in_bytes.Count;
    }
    
    /**
     * Gives a list of byte arrays as the fragments. 
     * It doesn't know what the format of the inner data is and doesn't care 
     * how the output list of byte[] are used. The caller is 
     * supposed to handle the list and wrap them in data structures.
     */
    private IList<byte[]> ToFragments() {
      byte[] seriazlied_obj = _inner_data.SerializeTo();
      IList<byte[]> list = new List<byte[]>();
      int offset = 0;
      while(offset < seriazlied_obj.Length) {
        int fragment_length = (offset + _info.PieceLength > seriazlied_obj.Length) ?
          (seriazlied_obj.Length - offset) : _info.PieceLength;
        byte[] fragment = new byte[fragment_length];
        Buffer.BlockCopy(seriazlied_obj, offset, fragment, 0, fragment_length);
        list.Add(fragment);
        offset += fragment_length;
      }
      return list;
    }

    #region DictionaryData Methods
    public override IDictionary ToDictionary() {
      throw new NotImplementedException();
    }

    public override void FromDictionary(IDictionary dict) {
      throw new NotImplementedException();
    } 
    #endregion
  }
}
