using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using Fushare.BitTorrent;

namespace Fushare {
  
  public enum DictionaryDataType {
    Undefined,
    Regular, //text or binary
    FragmentableData,
    FingerprintedData,
    FragmentationInfo,
    BTPeerEntry
  }

  public class DictionaryDataUtil {
    /// <summary>
    /// Maps client side types to DictionaryDataTypes
    /// </summary>
    /// <remarks>The conversion makes it easy to use integers in the messages
    /// to represent types</remarks>
    public static DictionaryDataType GetDictionaryDataType(Type t) {
      if(t == typeof(RegularData)) {
        return DictionaryDataType.Regular;
      }
      else if(t == typeof(FragmentableData)) {
        return DictionaryDataType.FragmentableData;
      }
      else if(t == typeof(FingerprintedData)) {
        return DictionaryDataType.FingerprintedData;
      } 
      else if (t == typeof(FragmentationInfo)) {
        return DictionaryDataType.FragmentationInfo;
      } 
      else if (t == typeof(PeerEntry)) {
        return DictionaryDataType.BTPeerEntry;
      } 
      else {
        return DictionaryDataType.Undefined;
      }
    }

    /// <summary>
    /// Converts the enum value of a DictionaryData type to a class type.
    /// </summary>
    public static Type GetDataType(DictionaryDataType type) {
      switch(type) {
        case DictionaryDataType.FingerprintedData:
          return typeof(FingerprintedData);
        case DictionaryDataType.FragmentableData:
          return typeof(FragmentableData);
        case DictionaryDataType.Regular:
          return typeof(RegularData);
        case DictionaryDataType.FragmentationInfo:
          return typeof(FragmentationInfo);
        case DictionaryDataType.BTPeerEntry:
          return typeof(PeerEntry);
        default:
          return typeof(object);
      }
    }
  }
}
