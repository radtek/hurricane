using System;
using System.Collections;
using System.Text;
using Brunet.DistributedServices;
using Brunet.Rpc;

namespace Fushare.Services {
  /// <summary>
  /// Represents the method that Brunet DHT uses to do PUT and CREATE operation
  /// </summary>
  public delegate bool BrunetDhtPutOp(byte[] key, byte[] value, int ttl);
  /// <summary>
  /// Represents the methd that Brunet DHT uses to do GET operation
  /// </summary>
  public delegate DhtGetResult[] BrunetDhtGetOp(byte[] key);

  /// <summary>
  /// Provides a set of async operations that could be leveraged by BrunetDht
  /// </summary>
  class BrunetDhtClientOperations {
    private static IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(BrunetDhtClientOperations));
    private IDht _dht;

    public BrunetDhtClientOperations(IDht dht) {
      this._dht = dht;
    }

    public IAsyncResult BeginGetWithCallback(byte[] key, AsyncCallback acb, object state) {
      BrunetDhtGetOp op = new BrunetDhtGetOp(this._dht.Get);
      IAsyncResult ar = op.BeginInvoke(key, acb, state);
      return ar;
    }

    public IAsyncResult BeginPutWithCallback(byte[] key, byte[] value, int ttl, AsyncCallback acb, object state) {
      BrunetDhtPutOp op = new BrunetDhtPutOp(this._dht.Put);
      IAsyncResult ar = op.BeginInvoke(key, value, ttl, acb, state);
      return ar;
    }

    public IAsyncResult BeginCreateWithCallback(byte[] key, byte[] value, int ttl, AsyncCallback acb, object state) {
      BrunetDhtPutOp op = new BrunetDhtPutOp(this._dht.Create);
      IAsyncResult ar = op.BeginInvoke(key, value, ttl, acb, state);
      return ar;
    }
  }
}
