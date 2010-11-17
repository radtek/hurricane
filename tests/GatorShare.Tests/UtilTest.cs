using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;

namespace GatorShare {
  [TestFixture]
  public class UtilTest {
    public void TestCombinePaths() {
      var systemDependentBaseDir = Path.GetTempPath();
      var path1 = UriUtil.CombinePaths(Path.Combine(systemDependentBaseDir, 
        "aaa"), new Uri("/bbb/ccc", UriKind.Relative));
      Assert.AreEqual(Path.Combine(systemDependentBaseDir, Path.Combine("aaa", 
        Path.Combine("bbb", "ccc"))), path1);
    }
  }
}
