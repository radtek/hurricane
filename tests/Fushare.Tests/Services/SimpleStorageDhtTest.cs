using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Fushare.Services {
  [TestFixture]
  public class SimpleStorageDhtTest {
    SimpleStorageDht _dht = new SimpleStorageDht();

    [SetUp]
    public void Initialize() {
      _dht = new SimpleStorageDht();
    }

    [Test]
    public void TestPutAndGet() {
      Random rnd = new Random();
      byte[] bytesToTest = new byte[100000];
      rnd.NextBytes(bytesToTest);
      string keyString = "key555";
      _dht.Put(keyString, bytesToTest);
      byte[] valActual = _dht.GetMultiple(keyString, 1).Value;
      Assert.AreEqual(TextUtil.MD5Sum(bytesToTest), TextUtil.MD5Sum(valActual));
    }
  }
}
