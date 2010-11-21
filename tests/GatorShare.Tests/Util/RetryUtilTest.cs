using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace GatorShare {
  [TestFixture]
  public class RetryUtilTest {
    [Test]
    public void TestRetry() {
      RetryUtil tu = new RetryUtil();
      tu.NumRetries = 3;
      try {
        tu.RetryFunc<object, object, Exception>(x => { throw new Exception("abc"); }, new object(), x => x.Message == "abc");
      } catch (Exception) {
        Assert.AreEqual(3, tu.RetryExecuted);
      }

      tu = new RetryUtil();
      tu.RetryDelay = new TimeSpan(0, 0, 1);
      try {
        tu.RetryFunc<object, object, Exception>(x => { throw new Exception("abc"); }, new object(), x => x.Message == "abc");
      } catch (Exception) {
        Assert.AreEqual(1, tu.RetryExecuted);
      }
    }
  }
}
