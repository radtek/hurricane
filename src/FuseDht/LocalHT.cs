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

    private Node _node;

    public LocalHT() {
      AHAddress addr = new AHAddress(new RNGCryptoServiceProvider());
      Node brunetNode = new StructuredNode(addr);
      RpcManager rpc = RpcManager.GetInstance(brunetNode);
      this._ts = new TableServer(brunetNode, rpc);
      this._node = brunetNode;
    }

    /**
     * We don't use password anymore
     */
    public bool Create(string key, string value, int ttl) {
      return this._ts.PutHandler(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value), ttl, true);
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
      return this._ts.PutHandler(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value), ttl, false);
    }

    public IDictionary GetDhtInfo() {
      Hashtable ht = new Hashtable();
      ht.Add("address", _node.Address.ToString());
      return ht;
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
