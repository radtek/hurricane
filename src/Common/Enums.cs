using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare {
  public enum DhtType {
    Local,
    BrunetDht,
    OpenDht /* Maybe in the future */
  }

  public enum FSOpType {
    Read,
    Write
  }
}
