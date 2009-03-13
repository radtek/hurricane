using System;
using System.Collections;
using Brunet.DistributedServices;
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
        return new FuseDhtHelper(xmlrpc_port, shadow_dir);
      } else if (t == HelperType.Dht) {
        return new FuseDhtHelper(xmlrpc_port, shadow_dir);
      } else {
        throw new ArgumentException("No Dht of specified type");
      }
    }
  }
}
