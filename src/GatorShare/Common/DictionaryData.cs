using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;

namespace GatorShare {
  /// <summary>
  /// Represents data in a Dictionary.
  /// </summary>
  /// <remarks>
  /// <b>Callers:</b><br>
  /// Use DictionaryData CreateDictionaryData(byte[] serializedObj, ISerializer 
  /// serializer) to create DictionaryData from bytes; Use byte[] SerializeTo(
  /// ISerializer serializer) to convert object to bytes.<br>
  /// <b>Inheritors:</b><br>
  /// Implement ToDictionary and FromDictionary to control the behavior of 
  /// (object, IDictionary) mapping.
  /// Use InitIDictionaryFromType in ToDictionary.
  /// You also have to implement a default constructor.
  /// </remarks>
  public abstract class DictionaryData : IDictionaryData, IDto {
    
    #region Fields
  
    /// <summary>
    /// Key used byte the _data_type in the dictionary representation of data.
    /// </summary>
    /// <remarks>
    /// It is defined here to enforce the name name used by inherited classes.
    /// </remarks>
    public const string DataTypeKey = "data_type";

    #endregion

    #region Constructors
    /// <summary>
    /// Use CreateDictionaryData instead to construct an instance. 
    /// </summary>
    /// <remarks>
    /// Allows fields to be set after been instantiated.
    /// </remarks>
    internal DictionaryData() { } 

    #endregion

    #region IDictionaryData Members
    
    /// <summary>
    /// Converts this object to an IDictionary instance.
    /// </summary>
    /// <remarks>
    /// <b>Inheritors:</b><br>
    /// Make sure that the type information is inserted to the dictionary by
    /// InitIDictionaryFromType method.
    /// </remarks>
    public abstract IDictionary ToDictionary();

    /**
     * Needs to be implemented
     */
    public abstract void FromDictionary(IDictionary dict);

    #endregion

    #region IDto Members

    /// <summary>
    /// Serializes this object with AdrSerializer
    /// </summary>
    public byte[] SerializeTo() {
      return SerializeTo(new AdrSerializer());
    }

    /// <summary>
    /// Serializes this object
    /// </summary>
    /// <remarks>
    /// Its behavior is decided by ToDictionary method, depending on how it's
    /// implemented by inheritors.
    /// </remarks>
    public virtual byte[] SerializeTo(ISerializer serializer) {
      return serializer.Serialize(ToDictionary());
    }

    /// <summary>
    /// bytes -> IDictionary -> object instance.
    /// </summary>
    public void DeserializeFrom(byte[] serializedObj) {
      DeserializeFrom(serializedObj, new AdrSerializer());
    }

    public virtual void DeserializeFrom(byte[] serializedObj, ISerializer serializer) {
      IDictionary dict = (IDictionary)serializer.Deserialize(serializedObj);
      FromDictionary(dict);
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates DictionaryData filled by entries in the IDictionary.
    /// </summary>
    /// <remarks>
    /// Its behavior is decided by FromDictionary method, depending on how it's
    /// implemented by inheritors.
    /// </remarks>
    public static DictionaryData CreateDictionaryData(IDictionary dict) {
      //every dictionary must have type information
      //if not, exception should be thrown
      DictionaryDataType t = (DictionaryDataType)dict[DictionaryData.DataTypeKey];
      //This is supposed to be a derived class
      DictionaryData d = PrepareDictionaryData(t);
      d.FromDictionary(dict);
      return d;
    }

    /// <summary>
    /// Creates an DictionaryData instance and fill it with serialized object.
    /// </summary>
    public static DictionaryData CreateDictionaryData(byte[] serializedObj, ISerializer serializer) {
      IDictionary dict = (IDictionary)serializer.Deserialize(serializedObj);
      return CreateDictionaryData(dict);
    }

    /// <summary>
    /// Uses AdrSerializer deserialization.
    /// </summary>
    public static DictionaryData CreateDictionaryData(byte[] serializedObj) {
      return CreateDictionaryData(serializedObj, new AdrSerializer());
    }

    #endregion

    #region Protected Static Helper Methods

    /// <summary>
    /// Derived classes could use this method to initialize the IDictionary with datatype added
    /// at the beginning of ToDictionary
    /// </summary>
    /// <remarks>
    /// datatype is represented using int in the dictionary.
    /// </remarks>
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
