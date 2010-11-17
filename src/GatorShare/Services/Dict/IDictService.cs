using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GatorShare.Services.Dict {
  /// <summary>
  /// Defines the interface for Dht services provides by Fushare.
  /// </summary>
  /// <remarks>Dht doesn't have to accept keys as strings but this interface mainly 
  /// services requests coming from the web, so both nameSpace and name are 
  /// strings. Do not confuse with the Dht infrastrure.</remarks>
  public interface IDictService {
    void Put(string nameSpace, string name, byte[] value);
    void Create(string nameSpace, string name, byte[] value);
    byte[] Get(string nameSpace, string name);
  }
}
