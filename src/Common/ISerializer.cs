using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Fushare {
  /**
   * Serializes and deserializes objects into and from binary
   */
  public interface ISerializer {
    byte[] Serialize(object obj);
    void Serialize(object obj, Stream stream);
    object Deserialize(byte[] bytes);
    object Deserialize(Stream stream);
  }
}
