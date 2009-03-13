using System;
using System.Collections.Generic;
using System.Text;

namespace Fushare {
  /**
   * Represents a DTO (Data Transfer Object) that can be serialized to and 
   * deserialized from its binary form
   */
  interface IDto {
    byte[] SerializeTo();
    byte[] SerializeTo(ISerializer serializer);
    /**
     * This method fills the fields of the DTO with serialized byte array
     */
    void DeserializeFrom(byte[] serializedObj);
    void DeserializeFrom(byte[] serializedObj, ISerializer serializer);
  }
}
