using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;

namespace Fushare {
  /**
   * Represents a wrapper of regular DictionaryData or another
   * DictionaryDataDecorator to add additional functionalities
   * 
   */
  abstract class DictionaryDataDecorator : DictionaryData {

    /**
     * Regular data or another decorator
     */
    protected DictionaryData _inner_data;

    protected const string InnerDataKey = "inner_data";

    public DictionaryDataDecorator() : base() { }

    public DictionaryDataDecorator(DictionaryData inner_data) {
      _inner_data = inner_data;
    }

    public DictionaryData InnerData {
      get {
        return _inner_data;
      }
    }

    /**
     * Makes the IDictionary representation with InnerData.
     * 
     * @return IDictionary with:
     *  1. fields of base class inserted
     *  2. inner data inserted
     * For derived classes, fields of the class itself should be added by its overridden
     * ToDictionary()
     */
    protected IDictionary InitIDictionaryWithInnerData() {
      IDictionary dict = InitIDictionaryFromType();
      dict.Add(InnerDataKey, _inner_data.ToDictionary());
      return dict;
    }

    /**
     * Fills inner_data with the IDictionary.
     * For derived classes, fields of the class itself should be added by its overrided
     * FromDictionary()
     */
    protected void FillInnerData(IDictionary dict) {
      IDictionary inner_dict = (IDictionary)dict[InnerDataKey];
      _inner_data = (DictionaryData)DictionaryData.CreateDictionaryData(inner_dict);
    }
  }
}
