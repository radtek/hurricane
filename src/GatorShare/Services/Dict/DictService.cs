using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GatorShare.External.DictionaryService;

namespace GatorShare.Services.Dict {
  public class DictService : IDictService {

    DictionaryServiceBase _dict;

    public DictService(DictionaryServiceBase dht) {
      _dict = dht;
    }

    #region IDictService Members

    public void Put(string nameSpace, string name, byte[] value) {
      var keyStr = ServiceUtil.GetDictKeyBytes(nameSpace, name);
      _dict.Put(keyStr, value);
    }

    public void Create(string nameSpace, string name, byte[] value) {
      var keyStr = ServiceUtil.GetDictKeyBytes(nameSpace, name);
      _dict.Create(keyStr, value);
    }

    public byte[] Get(string nameSpace, string name) {
      var keyStr = ServiceUtil.GetDictKeyBytes(nameSpace, name);
      return _dict.Get(keyStr).FirstValue;
    }

    #endregion
  }
}
