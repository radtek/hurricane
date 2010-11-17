using System;
using System.Collections;
using System.Text;

namespace GatorShare {
  /**
   * Represents data with properties that can be expressed in a 
   * Key/Value pair form.
   */
  interface IDictionaryData {
    /**
     * Extracts fields and added to the IDictionary.
     */
    IDictionary ToDictionary();
    /**
     * Fill the object with entries in the IDictionary
     */
    void FromDictionary(IDictionary dict);
  }
}
