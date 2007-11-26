using System;
using System.Collections.Generic;
using System.Text;
using Ipop;
using Brunet.Dht;
using Brunet;
using System.Security.Cryptography;
using System.Net;

namespace FuseSolution.Common {
  /**
   * Soap URL: http://localhost:54541/sd.rem
   * XmlRpc URL: http://localhost:54541/xd.rem
   */
  public class LocalHTService {
    public const int SERVER_PORT = 54541;

    public static void Main(string[] args) {
      DhtServer server = new DhtServer(SERVER_PORT);
      LocalHTBrunetAdapter ht = new LocalHTBrunetAdapter();
      server.Update(ht);
      Console.WriteLine("Press Enter to stop");
      Console.ReadLine();
    }
  }

  public class LocalHTBrunetAdapter : Brunet.Dht.Dht {
    private LocalHT _ht = new LocalHT();

    public LocalHTBrunetAdapter() : 
        base(new StructuredNode(new AHAddress(new RNGCryptoServiceProvider()))) {
    }

    public new DhtGetResult[] Get(string key) {
      return _ht.Get(key);
    }

    public new bool Put(string key, string value, int ttl) {
      return _ht.Put(key, value, ttl);
    }

    public new bool Create(string key, string value, int ttl) {
      return _ht.Create(key, value, ttl);
    }
  }
}
