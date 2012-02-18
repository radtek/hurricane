using System;
using System.Collections;
using System.Text;

namespace GatorShare {
  /**
   * Base class of all the test fixtures in fushare.
   * 
   */
  public class FushareTestBase {
    protected static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(FushareTestBase));

    static FushareTestBase() {
#if LOG4NET
      Logger.LoadConfig(); 
#endif
    }
  }
}
