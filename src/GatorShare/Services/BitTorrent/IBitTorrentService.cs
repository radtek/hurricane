using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * @namespace Fushare::BitTorrent
 * Contains classes that provide Fushare's BitTorrent functionality. 
 */
namespace GatorShare.Services.BitTorrent {
  /// <summary>
  /// Defines contract of a BitTorrent Service.
  /// </summary>
  public interface IBitTorrentService {
    /// <summary>
    /// Gets a file or directory.
    /// </summary>
    /// <param name="nameSpace">namespace of the name</param>
    /// <param name="name">name</param>
    /// <returns>The full path to the file or directory already downloaded.</returns>
    DataMetaInfo Get(string nameSpace, string name);

    /// <summary>
    /// Checks if the specified file exists without downloading it.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <returns>The meta information about the file.</returns>
    DataMetaInfo Peek(string nameSpace, string name);

    /// <summary>
    /// Gets part of a file downloaded via BitTorrent.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="bytesToRead">The number of bytes to read.</param>
    /// <returns></returns>
    byte[] Get(string nameSpace, string name, long offset, int bytesToRead);

    /// <summary>
    /// Gets the file or directory and save it to the specified path.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="saveDirPath">The full path to save the file or directory.</param>
    void Get(string nameSpace, string name, string saveDirPath);

    /// <summary>
    /// Publishes a file or directory.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <remarks>Uses the name of the directory of file as the publishing name. 
    /// Used when you want to publish file/directory already in the Cache folder.
    /// </remarks>
    void Publish(string nameSpace, string name);
    /// <summary>
    /// Same as Publish except that you don't get an exception when the key is 
    /// duplicated. You overwrite the existing key.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    void Update(string nameSpace, string name);

    /// <summary>
    /// Updates a file or directory by the specified path.
    /// </summary>
    /// <param name="path">The path to the file/directory to be published.
    /// </param>
    /// <remarks>Used when you have a specific path for the 
    /// file/directory.</remarks>
    void Publish(string path);

    /// <summary>
    /// Same as Publish except that you don't get an exception when the key is 
    /// duplicated. You overwrite the existing key.
    /// </summary>
    /// <param name="path">The path to the file/directory to be published.
    /// </param>
    void Update(string path);

    /// <summary>
    /// Gets the service info.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns></returns>
    BitTorrentServiceInfo GetServiceInfo(string filter);
  }
}
