using Ipop;
using System;
using System.Collections;
using Brunet.Dht;
using Brunet;


namespace Fushare.Filesystem {

  public class FuseDhtHelperFactory {
    public enum HelperType {
      Local, Dht
    }

    public FuseDhtHelperFactory() {
    }

    /**
     * @param basedir Mounting point of shadow FS
     */
    public static FuseDhtHelper GetFuseDhtHelper(IDictionary options) {
      HelperType t = (HelperType)options["helper_type"];
      string shadow_dir = options["shadow_dir"] as string;
      int dht_port = (int)options["dht_port"];
      int xmlrpc_port = (int)options["xmlrpc_port"];
      if (t == HelperType.Local) {
        IDht dht = new LocalHT();
        return new FuseDhtHelper(dht, xmlrpc_port, shadow_dir);
      } else if (t == HelperType.Dht) {
        IDht dht = Ipop.DhtServiceClient.GetXmlRpcDhtClient(dht_port);
        return new FuseDhtHelper(dht, xmlrpc_port, shadow_dir);
      } else {
        throw new ArgumentException("No Dht of specified type");
      }
    }
  }
}
