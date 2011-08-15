using System;
using System.Collections;
using System.Text;
using System.IO;
using Brunet.Util;

namespace GatorShare {
  /**
   * Represents a class that is compatible with ISerializer and adaptes AdrConverter
   * methods.
   */
  public class AdrSerializer : ISerializer {
    #region ISerializer Members

    public byte[] Serialize(object o) {
      using (MemoryStream ms = new MemoryStream()) {
        AdrConverter.Serialize(o, ms);
        return ms.ToArray();
      }
    }

    public object Deserialize(byte[] b) {
      return AdrConverter.Deserialize(b);
    }

    public void Serialize(object obj, Stream stream) {
      AdrConverter.Serialize(obj, stream);
    }

    public object Deserialize(Stream stream) {
      return AdrConverter.Deserialize(stream);
    }

    #endregion
  }
}
