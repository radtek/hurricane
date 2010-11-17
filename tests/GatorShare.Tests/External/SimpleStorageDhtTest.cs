using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace GatorShare.External.DictionaryService {
  [TestFixture]
  public class SimpleStorageDhtTest {
    SimpleStorageDictionary _dht = new SimpleStorageDictionary();

    [SetUp]
    public void Initialize() {
      _dht = new SimpleStorageDictionary();
    }

    [Test]
    public void TestPutAndGet() {
      Random rnd = new Random();
      byte[] bytesToTest = new byte[100000];
      rnd.NextBytes(bytesToTest);
      byte[] keyBytes = new byte[20];
      rnd.NextBytes(keyBytes);
      string keyString = UrlBase64.Encode(keyBytes);
      _dht.Put(keyString, bytesToTest);
      byte[] valActual = _dht.GetMultiple(keyString, 1).Value;
      Assert.AreEqual(TextUtil.MD5Sum(bytesToTest), TextUtil.MD5Sum(valActual));
    }
  }
}
