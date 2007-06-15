using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Brunet.Dht;
using Brunet;
using System.Security.Cryptography;
using NUnit.Framework;
using Ipop;

namespace FuseDht {
  /**
   * LocalHT only uses TableServer to read and store data locally but provide the same interface
   * as IDht for testing purpose.
   */
  class LocalHT : Ipop.IDht {
    private TableServer _ts;

    public LocalHT() {
      AHAddress addr = new AHAddress(new RNGCryptoServiceProvider());
      Node brunetNode = new StructuredNode(addr);
      EntryFactory factory = EntryFactory.GetInstance(brunetNode, EntryFactory.Media.Memory);
      this._ts = new TableServer(factory, brunetNode);
    }

    /**
     * We don't use password anymore
     */
    public bool Create(string key, string value, int ttl) {
      return this._ts.Create(Encoding.UTF8.GetBytes(key), ttl, Encoding.UTF8.GetBytes(value));
    }

    public DhtGetResult[] Get(string key) {
      IList result = this._ts.Get(Encoding.UTF8.GetBytes(key), 1000, null);
      IList values = result[0] as IList;
      List<DhtGetResult> ret = new List<DhtGetResult>();
      foreach (Hashtable ht in values) {
        ret.Add(new DhtGetResult(ht));
      }
      return ret.ToArray();
    }

    public bool Put(string key, string value, int ttl) {
      return this._ts.Put(Encoding.UTF8.GetBytes(key), ttl, Encoding.UTF8.GetBytes(value));
    }

    public string BeginGet(string key) {
      throw new Exception("The method or operation is not implemented.");
    }

    public DhtGetResult[] ContinueGet(string token) {
      throw new Exception("The method or operation is not implemented.");
    }

    public void EndGet(string token) {
      throw new Exception("The method or operation is not implemented.");
    }
  }

  [TestFixture]
  public class LocalHTTest {
    [Test]
    public void TestPutAndGet() {
      IDht dht = new LocalHT();
      dht.Put("key1", "value1", 1000);
      dht.Put("key1", "value2", 2000);
      IList expected = new ArrayList();
      expected.Add("value1");
      expected.Add("value2");
      DhtGetResult[] result = dht.Get("key1");
      foreach (DhtGetResult rs in result) {
        Assert.IsTrue(expected.Contains(rs.valueString));
      }
    }
  }
}
