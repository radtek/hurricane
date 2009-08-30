using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  public class DhtResultEntry {
    readonly IDictionary<string, object> _metaInfo = 
      new Dictionary<string, object>();
    readonly byte[] _value;

    public IDictionary<string, object> MetaInfo {
      get {
        return _metaInfo;
      }
    }

    public byte[] Value {
      get {
        return _value;
      }
    }

    public DhtResultEntry(byte[] value) {
      _value = value;
    }

    public DhtResultEntry(byte[] value, IDictionary<string, object> metaInfo)
      : this(value) {
      _metaInfo = metaInfo;
    }
  }
}
