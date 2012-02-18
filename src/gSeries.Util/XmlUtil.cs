using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace GSeries {
  /// <summary>
  /// Provides utilities for Xml seriliazation.
  /// </summary>
  public class XmlUtil {
    /// <summary>
    /// Serializes object to XML string
    /// </summary>
    /// <param name="data">The data.</param>
    /// <returns>The XML string</returns>
    public static string ToXml<T>(T data) {
      XmlSerializer xs = new XmlSerializer(typeof(T));
      var sw = new StringWriter();
      using (var xw = new XmlTextWriter(sw)) {
        xs.Serialize(xw, data);
        return sw.ToString();
      }
    }

    /// <summary>
    /// Writes the object to a file as XML.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data">The data.</param>
    /// <param name="filePath">The file path.</param>
    public static void WriteXml<T>(T data, string filePath) {
      XmlSerializer xs = new XmlSerializer(typeof(T));
      using (var xw = new XmlTextWriter(filePath, Encoding.UTF8)) {
        xs.Serialize(xw, data);
      }
    }

    /// <summary>
    /// Reads the object from the XML string.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="xmlString">The XML string.</param>
    /// <returns>The result object.</returns>
    public static T FromXml<T>(string xmlString) {
      XmlSerializer xs = new XmlSerializer(typeof(T));
      using (var reader = new XmlTextReader(new StringReader(xmlString))) {
        if (!xs.CanDeserialize(reader)) {
          throw new ArgumentException("This object cannot be deserialized", 
            "xmlString");
        }
        return (T)xs.Deserialize(reader);
      }
    }

    /// <summary>
    /// Reads the object from a XML file.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="filePath">The file path.</param>
    /// <returns></returns>
    public static T ReadXml<T>(string filePath) {
      var xmlString = File.ReadAllText(filePath);
      return FromXml<T>(xmlString);
    }
  }
}
