using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Fushare.Services {
  [TestFixture]
  public class BigTableDhtTest {
    BigTableDht _bigTableDht;

    [SetUp]
    public void Initialize() {
      _bigTableDht = new BigTableDht("Dennis84225@gmail.com", "fushare");
    }

    [Test]
    public void TestPutThenGetString() {
      string valString = "Bigtable111";
      byte[] valToPut = Encoding.UTF8.GetBytes(valString);
      string keyString = "ns:key161.txt";
      _bigTableDht.Put(keyString, valToPut);
      byte[] valActual = _bigTableDht.GetMostRecentVal(keyString);
      string strActual = Encoding.UTF8.GetString(valActual);
      Assert.AreEqual(valString, strActual);
    }

    [Test]
    public void TestPutThenGetBytes() {
      Random rnd = new Random();
      byte[] bytesToTest = new byte[100000];
      rnd.NextBytes(bytesToTest);
      string keyString = "key333";
      _bigTableDht.Put(keyString, bytesToTest);
      byte[] valActual = _bigTableDht.GetMostRecentValAsOctetStream(keyString);
      Assert.IsTrue(Brunet.MemBlock.Reference(bytesToTest).Equals(
        Brunet.MemBlock.Reference(valActual)));
    }

    [Test]
    public void TestPutThenGetMultiple() {
      string keyString = "key1";
      _bigTableDht.GetMultiple(keyString, 100);
    }
  }
}
