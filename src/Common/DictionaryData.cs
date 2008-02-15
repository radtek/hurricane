using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;

namespace Fushare {
  /**
   * Represents data in a Dictionary.
   */
  public abstract class DictionaryData : IDictionaryData, IDto {
    
    #region Fields
    /**
     * Key used byte the _data_type in the dictionary representaton of data.
     * It is defined here to enforce the key name used by inherited classes.
     */
    public const string DataTypeKey = "data_type";

    #endregion

    #region Constructors
    /**
     * Allow fields to be set after instantiated
     */
    public DictionaryData() { } 

    #endregion

    #region IDictionaryData Members
    
    /**
     * Make sure that the type information is inserted to the dictionary 
     */
    public abstract IDictionary ToDictionary();

    /**
     * Needs to be implemented
     */
    public abstract void FromDictionary(IDictionary dict);

    #endregion

    #region IDto Members

    /**
     * Choose AdrSerializer by default
     */
    public byte[] SerializeTo() {
      return SerializeTo(new AdrSerializer());
    }

    /**
     * Serializer has to be able to deal with IDictionary
     */
    public virtual byte[] SerializeTo(ISerializer serializer) {
      return serializer.Serialize(ToDictionary());
    }

    /**
     * Choose AdrSerializer by default
     */
    public void DeserializeFrom(byte[] serializedObj) {
      DeserializeFrom(serializedObj, new AdrSerializer());
    }

    public virtual void DeserializeFrom(byte[] serializedObj, ISerializer serializer) {
      IDictionary dict = (IDictionary)serializer.Deserialize(serializedObj);
      FromDictionary(dict);
    }

    #endregion

    #region Public Static Methods

    /**
     * Creates DictionaryData filled by entries in the IDictionary.
     */
    public static DictionaryData CreateDictionaryData(IDictionary dict) {
      //every dictionary must have type information
      //if not, exception should be thrown
      DictionaryDataType t = (DictionaryDataType)dict[DictionaryData.DataTypeKey];
      //This is supposed to be a derived class
      DictionaryData d = PrepareDictionaryData(t);
      d.FromDictionary(dict);
      return d;
    }

    /**
     * Creates instance and filled with serialized object.
     */
    public static DictionaryData CreateDictionaryData(byte[] serializedObj, ISerializer serializer) {
      IDictionary dict = (IDictionary)serializer.Deserialize(serializedObj);
      return CreateDictionaryData(dict);
    }

    public static DictionaryData CreateDictionaryData(byte[] serializedObj) {
      return CreateDictionaryData(serializedObj, new AdrSerializer());
    }

    #endregion

    #region Protected Static Helper Methods
    /**
     * Derived classes could use this method to initialize the IDictionary with datatype added
     * at the beginning of ToDictionary
     */
    protected IDictionary InitIDictionaryFromType() {
      IDictionary dict = new ListDictionary();
      dict.Add(DictionaryData.DataTypeKey, (int)DictionaryDataUtil.GetDictionaryDataType(this.GetType()));
      return dict;
    }


    /**
     * Creates an <b>empty</b> DictionaryData object with the datatype info.
     * @return A reference to the newly created object.
     */
    protected static DictionaryData PrepareDictionaryData(DictionaryDataType type) {
      Type t = DictionaryDataUtil.GetDataType(type);
      return (DictionaryData)Activator.CreateInstance(t);
    } 
    #endregion
  }
}
