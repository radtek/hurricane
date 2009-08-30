using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  public class DhtResults {
    readonly IDictionary<string, object> _metaInfo =
      new Dictionary<string, object>();

    readonly IList<DhtResultEntry> _entryList = new List<DhtResultEntry>();

    public IDictionary<string, object> MetaInfo {
      get {
        return _metaInfo;
      }
    }

    public IList<DhtResultEntry> ResultEntries {
      get {
        return _entryList;
      }
    }

    public IList<byte[]> Values {
      get {
        var retList = new List<byte[]>();
        foreach(DhtResultEntry entry in _entryList) {
          retList.Add(entry.Value);
        }
        return retList;
      }
    }

    public bool HasMultipleEntries {
      get {
        return _entryList.Count > 1 ? true : false;
      }
    }

    /// <summary>
    /// Gets the value. Null returned if ResultEntry is empty.
    /// </summary>
    /// <value>The value.</value>
    public byte[] Value {
      get {
        return ResultEntry != null ? ResultEntry.Value : null;
      }
    }

    public DhtResultEntry ResultEntry {
      get {
        return _entryList.Count > 0 ? _entryList[0] : null;
      }
    }
  }
}
