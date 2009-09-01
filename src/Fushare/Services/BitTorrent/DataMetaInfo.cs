using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Fushare.Services.BitTorrent {
  /// <summary>
  /// Represents the meta information of the downloaded data.
  /// </summary>
  /// <remarks>
  /// See <see cref="http://www.bittorrent.org/beps/bep_0003.html#metainfo-files-are-bencoded-dictionaries-with-the-following-keys"/>
  /// for the metainfo keys defined by BitTorrent.
  /// </remarks>
  public class DataMetaInfo {

    #region Properties for Fushare classes
    /// <summary>
    /// Gets or sets the base URI, which is the Uri of the file or the root direcotry of 
    /// all files in this download.
    /// </summary>
    /// <value>The base URI.</value>
    [XmlIgnore]
    public Uri DataUri { get; set; }
    /// <summary>
    /// Gets or sets the files in this torrent. Uses the form of relative paths/Uris.
    /// </summary>
    /// <value>The files.</value>
    /// <remarks>
    /// Uses Uri instead of string to circumvent the OS directory separator difference.
    /// </remarks>
    [XmlIgnore]
    public IList<Uri> Files { get; set; }

    /// <summary>
    /// Gets a value indicating whether this instance is a single file.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is single file; otherwise, <c>false</c>.
    /// </value>
    public bool IsSingleFile {
      get {
        return (Files.Count == 0);
      }
    } 
    #endregion

    /// <summary>
    /// Gets or sets the torrent file in bytes.
    /// </summary>
    /// <value>The torrent.</value>
    public byte[] TorrentBytes { get; set; }

    #region Properties for XmlSerializer
    [XmlElement("DataUri")]
    public string DataUriString {
      get {
        return DataUri.ToString();
      }
      set {
        DataUri = new Uri(value);
      }
    }

    /// <summary>
    /// Gets or sets the files path strings. 
    /// </summary>
    /// <value>The files path strings.</value>
    /// <remarks>It doesn't work with ArrayList or List(string), why? </remarks>
    [XmlArray("Files")]
    [XmlArrayItem("File")]
    public string[] FilesPathStrings {
      get {
        var list = new List<string>();
        foreach (var file in Files) {
          list.Add(file.ToString());
        }
        return list.ToArray();
      }
      set {
        foreach (string file in value) {
          Files.Add(new Uri(file, UriKind.Relative));
        }
      }
    } 
    #endregion

    #region Constructors
    public DataMetaInfo() {
      Files = new List<Uri>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMetaInfo"/> class. 
    /// Fushare code should be this contructor.
    /// </summary>
    /// <param name="baseUri">The base URI.</param>
    /// <param name="files">The files.</param>
    public DataMetaInfo(Uri baseUri, IList<Uri> files, byte[] torrent) : this() {
      DataUri = baseUri;
      Files = files;
      TorrentBytes = torrent;
    }
    #endregion

  }
}
