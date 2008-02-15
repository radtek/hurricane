using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;

namespace Fushare {
  
  public enum DictionaryDataType {
    Undefined,
    Regular, //text or binary
    FragmentableData,
    FingerprintedData,
    FragmentationInfo
  }

  public class DictionaryDataUtil {
    /**
     * Maps client side types to DictionaryDataTypes
     */
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
      else {
        return DictionaryDataType.Undefined;
      }
    }

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
        default:
          return typeof(object);
      }
    }
  }
}
