using Ipop;
using System;
using System.Collections;
using Brunet.Dht;
using Brunet;
using FuseSolution.Common;


namespace FuseSolution.FuseDht {

  public class FuseDhtHelperFactory {
    public enum HelperType {
      Local, Dht
    }

    public FuseDhtHelperFactory() {
    }

    /**
     * @param basedir Mounting point of shadow FS
     */
    public static FuseDhtHelper GetFuseDhtHelper(HelperType t, IDictionary options) {
      string shadow_dir = options["shadow_dir"] as string;
      int dht_port = (int)options["dht_port"];
      if (t == HelperType.Local) {
        IDht dht = new LocalHT();
        return new FuseDhtHelper(dht, shadow_dir);
      } else if (t == HelperType.Dht) {
        IDht dht = Ipop.DhtServiceClient.GetXmlRpcDhtClient(dht_port);
        return new FuseDhtHelper(dht, shadow_dir);
      } else {
        throw new ArgumentException("No Dht of specified type");
      }
    }
  }
}
