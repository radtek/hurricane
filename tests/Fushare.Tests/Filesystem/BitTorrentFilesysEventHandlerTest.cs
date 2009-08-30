using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using Fushare.Services.BitTorrent;

namespace Fushare.Filesystem {
  [TestFixture]
  public class BitTorrentFilesysEventHandlerTest {
    [Test]
    public void TestCreateVirtualFiles() {
      var basePath = IOUtil.GetRandomTempPath();
      DataMetaInfo meta = new DataMetaInfo() {
        DataUri = new Uri(Path.GetTempPath()),
        Files = new List<Uri>() {
          new Uri("/aaa/bbb", UriKind.Relative),
          new Uri("ccc/ddd", UriKind.Relative)
        }
      };
      //BitTorrentFilesysEventHandler.CreateVirtualFiles(new ShadowMetaFullPath(,basePath), 
      //  XmlUtil.ToXml<DataMetaInfo>(meta));
    }
  }
}
