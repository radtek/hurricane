using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  /// <summary>
  /// The data class for the results returned by DHT.
  /// </summary>
  public class DhtResults {
    #region Fields
    readonly IDictionary<string, object> _metaInfo = 
      new Dictionary<string, object>();
    readonly IList<DhtResultEntry> _entryList = new List<DhtResultEntry>(); 
    #endregion

    #region Properties
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
        foreach (DhtResultEntry entry in _entryList) {
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
    /// Gets the (first) value. Null returned if ResultEntry is empty.
    /// </summary>
    /// <value>The value.</value>
    public byte[] Value {
      get {
        return ResultEntry != null ? ResultEntry.Value : null;
      }
    }

    /// <summary>
    /// Gets the first result entry.
    /// </summary>
    /// <value>The result entry.</value>
    public DhtResultEntry ResultEntry {
      get {
        return _entryList.Count > 0 ? _entryList[0] : null;
      }
    } 
    #endregion
  }
}
