using System.IO;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Fushare.Services.BitTorrent {
  public class CacheRegistry {

    #region Fields
    /// <summary>
    /// (namepsace, name) -> full path.
    /// </summary>
    SerializableDictionary<string, string> _registry =
      new SerializableDictionary<string, string>();

    /// <summary>
    /// List of Cache directories.
    /// </summary>
    IList<string> _cacheDirs = new List<string>();

    XmlSerializer _serializer = new
      XmlSerializer(typeof(SerializableDictionary<string, string>)); 
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the path to the registry file.
    /// </summary>
    /// <value>The registry file path.</value>
    public string RegistryFilePath { get; private set; }
    /// <summary>
    /// Gets or sets the self name space.
    /// </summary>
    /// <value>The self name space.</value>
    /// <remarks>The value is used as the namespace for registered files/dirs 
    /// outside the cache dir</remarks>
    public string SelfNameSpace { get; private set; } 
    #endregion

    #region Constructor and Helper
    public CacheRegistry(string registryFilePath, string selfNameSpace) : 
      this(registryFilePath, selfNameSpace, true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheRegistry"/> class.
    /// </summary>
    /// <param name="registryFilePath">The registry file path.</param>
    /// <param name="selfNameSpace">The self name space.</param>
    /// <param name="loadAtStartup">if set to <c>true</c>, load the registry file 
    /// at startup.</param>
    public CacheRegistry(string registryFilePath, string selfNameSpace, 
      bool loadAtStartup) {
      if (!Path.IsPathRooted(registryFilePath)) {
        throw new ArgumentException("Path should be rooted.", "cacheDirPath");
      }

      RegistryFilePath = registryFilePath;
      SelfNameSpace = selfNameSpace;
      if (loadAtStartup) {
        LoadRegistryFile(); 
      }
    }

    private void LoadRegistryFile() {
      if (!File.Exists(RegistryFilePath)) {
        // Do nothing.
      } else {
        TryReadFromFile();
        foreach (var key in _registry.Keys) {
          var path = _registry[key];
          if (!(File.Exists(path) || Directory.Exists(path))) {
            RemoveFromRegistry(key);
          }
        }
      }
    } 
    #endregion

    /// <summary>
    /// Loads the cache directory.
    /// </summary>
    /// <param name="dirPath">The dir path.</param>
    /// <remarks>The directory should have a two-level hierarchy, namespace 
    /// directory and file/directory</remarks>
    public void LoadCacheDir(string dirPath) {
      if (!Directory.Exists(dirPath)) {
        throw new ArgumentException("Directory doesn't exist", "dirPath");
      }

      _cacheDirs.Add(dirPath);
      var cacheDirInfo = new DirectoryInfo(dirPath);
      foreach (var nsDirInfo in cacheDirInfo.GetDirectories()) {
        if (nsDirInfo.Name.StartsWith(".")) {
          // Ignore hidden.
          continue;
        }
        var filesysInfos = nsDirInfo.GetFileSystemInfos();
        foreach (var filesysInfo in filesysInfos) {
          if (!filesysInfo.Name.StartsWith(".")) {
            if (!_registry.ContainsValue(filesysInfo.FullName)) {
              AddToRegistry(nsDirInfo.Name, filesysInfo.Name, filesysInfo.FullName);
            }
          }
        }
      }
    }


    /// <summary>
    /// Determines whether the specified path is in cache dir.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>
    /// 	<c>true</c> if the specified path is in cache dir; otherwise, <c>false</c>.
    /// </returns>
    public bool IsInCacheDir(string path, bool checkPath) {
      var dirInfo = Util.GetParent(path, checkPath).Parent;
      return _cacheDirs.Contains(dirInfo.FullName);
    }
    
    public void RegisterPath(string path, bool checkPath) {
      var name = Util.GetFileOrDirectoryName(path, checkPath);
      if (IsInCacheDir(path, checkPath)) {
        var nsDirName = Util.GetParent(path, checkPath).Name;
        AddToRegistry(nsDirName, name, path);
      } else {
        AddToRegistry(SelfNameSpace, name, path);
      }
    }

    private void AddToRegistry(string nameSpace, string name, string value) {
      var key = Util.GetDhtKeyString(nameSpace, name);
      AddToRegistry(key, value);
    }

    private void AddToRegistry(string key, string value) {
      _registry.Add(key, value);
      WriteToFile();
    }

    private void RemoveFromRegistry(string key) {
      _registry.Remove(key);
      WriteToFile();
    }

    public void DeleteRegistryFile() {
      if (File.Exists(RegistryFilePath)) {
        File.Delete(RegistryFilePath);
      }
    }

    public string this[string key] {
      get {
        return _registry[key];
      }
    }

    private void WriteToFile() {
      using (var writer = new StreamWriter(RegistryFilePath)) {
        _serializer.Serialize(writer, _registry);
      }
    }


    /// <summary>
    /// Tries to read from file.
    /// </summary>
    /// <returns>True if successfully read.</returns>
    private bool TryReadFromFile() {
      using (var reader = new StreamReader(RegistryFilePath)) {
        try {
          _registry = _serializer.Deserialize(reader) as
        SerializableDictionary<string, string>;
          return true;
        } catch (Exception) {
          reader.Dispose();
          return false;
        }
      }
    }
  }
}
