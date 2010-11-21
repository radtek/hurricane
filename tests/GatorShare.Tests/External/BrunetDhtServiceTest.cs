using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace GatorShare.External.DictionaryService {
  [TestFixture]
  public class BrunetDhtServiceTest {
    BrunetDhtService _dict = new BrunetDhtService("127.0.0.1", 10000, "xm.rem");

    [Test]
    public void TestPutAndGet() {
      Random rnd = new Random();
      byte[] bytesToTest = new byte[1000];
      rnd.NextBytes(bytesToTest);
      byte[] keyBytes = new byte[20];
      rnd.NextBytes(keyBytes);
      _dict.Put(keyBytes, bytesToTest);
      byte[] valActual = _dict.Get(keyBytes).FirstValue;
      Assert.AreEqual(TextUtil.MD5Sum(bytesToTest), TextUtil.MD5Sum(valActual));
    }
  }
}
