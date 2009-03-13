using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fushare.Services {
  public class BigTableRetVal {
    public string column { get; set; }
    public string row { get; set; }
    public string table { get; set; }
    public string value { get; set; }
    public string timestamp { get; set; }

    public DateTime? Timestamp {
      get {
        if (timestamp == null) {
          return null;
        }
        return DateTime.Parse(timestamp);
      }
    }

    public byte[] Value {
      get {
        if (value == null) {
          return null;
        }
        return Convert.FromBase64String(value);
      }
    }
  }

  public class NullBigTableRetVal : BigTableRetVal { }
}
