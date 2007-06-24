using Ipop;
using System;
using System.Collections;
using Brunet.Dht;
using Brunet;


namespace FuseDht {

  public class FuseDhtHelperFactory {
    public enum HelperType {
      Local, Dht
    }

    public FuseDhtHelperFactory() {
    }

    /// <param name="basedir">Mounting point of shadow FS</param>    
    public static FuseDhtHelper GetFuseDhtHelper(HelperType t, string basedir) {
      if (t == HelperType.Local) {
        IDht dht = new LocalHT();
        return new FuseDhtHelper(dht, basedir);
      } else if (t == HelperType.Dht) {
        IDht dht = Ipop.DhtServiceClient.GetSoapDhtClient();
        return new FuseDhtHelper(dht, basedir);
      } else {
        throw new ArgumentException("No Dht of specified type");
      }
    }
  }
}
