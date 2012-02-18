using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonExSerializer;

namespace GSeries {
  /// <summary>
  /// Utility class that converts objects to and from JSON strings.
  /// </summary>
  public class JsonUtil {
    /// <summary>
    /// Converts from json string.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="jsonString">The json string.</param>
    /// <returns></returns>
    public static T ConvertFromJsonString<T>(string jsonString) {
      Serializer serializer = new Serializer(typeof(T));
      T results;
      results = (T)serializer.Deserialize(jsonString);
      return results;
    }

    /// <summary>
    /// Converts to json string.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj">The object.</param>
    /// <returns></returns>
    public static string ConvertToJsonString<T>(T obj) {
      Serializer serializer = new Serializer(typeof(T));
      return serializer.Serialize(obj);
    }
  }
}
