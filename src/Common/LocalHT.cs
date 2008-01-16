using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Brunet.Dht;
using Brunet;
using System.Security.Cryptography;
#if FUSE_NUNIT
using NUnit.Framework;
#endif
using System.Diagnostics;
using Ipop;

namespace Fushare.Common {
  /**
   * LocalHT only uses TableServer to read and store data locally but provide the same interface
   * as IDht for testing purpose.
   */
  public class LocalHT : Ipop.IDht {
    #region Fields
    private TableServer _ts;
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(LocalHT));
    public const int MAX_BYTES = 1000; 
    #endregion

    private MemBlock MapToBrunetAddress(byte[] key) {
      HashAlgorithm hashAlgo = HashAlgorithm.Create();
      byte[] hash = hashAlgo.ComputeHash(key);
      Address.SetClass(hash, AHAddress._class);
      return MemBlock.Reference(hash);
    }

    private Node _node;

    public LocalHT() {
      AHAddress addr = new AHAddress(new RNGCryptoServiceProvider());
      Node brunetNode = new StructuredNode(addr);
      RpcManager rpc = RpcManager.GetInstance(brunetNode);
      this._ts = new TableServer(brunetNode, rpc);
      this._node = brunetNode;

#if FUSE_DEBUG
      //Having some init data isn't bad
      string key = FuseDhtUtil.GenDhtKey("testbasedir", "testkey1", "ipop_ns");
      this.Put(key, "testvalue1", 5000);
      this.Put(key, "testvalue2", 3000);
#endif
    }

    /**
     * We don't use password anymore
     */
    public bool Create(string key, string value, int ttl) {
      MemBlock mb_key = MapToBrunetAddress(Encoding.UTF8.GetBytes(key));
      return this._ts.PutHandler(mb_key, Encoding.UTF8.GetBytes(value), ttl, true);
    }

    public DhtGetResult[] Get(string key) {
      MemBlock mb_key = MapToBrunetAddress(Encoding.UTF8.GetBytes(key));

      IList result = this._ts.Get(mb_key, MAX_BYTES, null);
      IList values = result[0] as IList;
      List<DhtGetResult> ret = new List<DhtGetResult>();
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,string.Format("Count of Dht Get Results: {0}", values.Count));
      foreach (Hashtable ht in values) {
        ret.Add(new DhtGetResult(ht));
      }
      return ret.ToArray();
    }

    public bool Put(string key, string value, int ttl) {
      MemBlock mb_key = MapToBrunetAddress(Encoding.UTF8.GetBytes(key));
      return this._ts.PutHandler(mb_key, Encoding.UTF8.GetBytes(value), ttl, false);
    }

    public IDictionary GetDhtInfo() {
      Hashtable ht = new Hashtable();
      ht.Add("address", _node.Address.ToString());
      return ht;
    }

    public string BeginGet(string key) {
      throw new Exception("The method or operation is not implemented.");
    }

    public DhtGetResult ContinueGet(string token) {
      throw new Exception("The method or operation is not implemented.");
    }

    public void EndGet(string token) {
      throw new Exception("The method or operation is not implemented.");
    }
  }

#if FUSE_NUNIT
  [TestFixture]
  public class LocalHTTest {
    [Test]
    [Ignore]
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
#endif
}
