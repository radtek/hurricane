using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services.Dht {
  public class DhtService : IDhtService {

    DhtBase _dht;

    public DhtService(DhtBase dht) {
      _dht = dht;
    }

    #region IDhtService Members

    public void Put(string nameSpace, string name, byte[] value) {
      var keyStr = Util.GetDhtKeyBytes(nameSpace, name);
      _dht.Put(keyStr, value);
    }

    public void Create(string nameSpace, string name, byte[] value) {
      var keyStr = Util.GetDhtKeyBytes(nameSpace, name);
      _dht.Create(keyStr, value);
    }

    public byte[] Get(string nameSpace, string name) {
      var keyStr = Util.GetDhtKeyBytes(nameSpace, name);
      return _dht.Get(keyStr).Value;
    }

    #endregion
  }
}
