using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace GatorShare.Services.BitTorrent {
  [TestFixture]
  public class DataMetaInfoTest {
    [Test]
    public void Test() {
      DataMetaInfo info = new DataMetaInfo();
      info.DataUri = new Uri("file:///C:/aaa/bbb");
      info.Files = new List<Uri> {
        new Uri(@"ccc/ddd.txt", UriKind.Relative),
        new Uri(@"eee/ddd.txt", UriKind.Relative)
      };
      var xmlString = XmlUtil.ToXml<DataMetaInfo>(info);
      DataMetaInfo info1 = XmlUtil.FromXml<DataMetaInfo>(xmlString);

      Assert.AreEqual(info.DataUriString, info1.DataUriString);
      Assert.AreEqual(info.Files, info1.Files);
    }
  }
}
