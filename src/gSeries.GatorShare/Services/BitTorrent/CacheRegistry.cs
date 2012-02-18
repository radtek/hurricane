using System.IO;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using GSeries.External.DictionaryService;

namespace GSeries.Services.BitTorrent {
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
    
    public string this[string key] {
      get {
        return _registry[key];
      }
    }

    /// <summary>
    /// Gets the registry.
    /// </summary>
    /// <value>The registry.</value>
    /// <remarks>@TODO: Unsafe internal API. Remove it later.</remarks>
    internal SerializableDictionary<string, string> Registry {
      get { return _registry; }
    }
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
        throw new ArgumentException(string.Format("Path should be rooted.", 
          registryFilePath), "cacheDirPath");
      }

      RegistryFilePath = registryFilePath;
      SelfNameSpace = selfNameSpace;
      if (loadAtStartup) {
        LoadRegistryFile(); 
      }
    }

    /// <summary>
    /// Loads the registry file and add the entries to the dictionary in memeory.
    /// </summary>
    /// <remarks>It checks whether the file/dir references exists.</remarks>
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

    #region Public Methods
    /// <summary>
    /// Loads the cache directory.
    /// </summary>
    /// <param name="dirPath">The dir path.</param>
    /// <remarks>The directory should have a two-level hierarchy, namespace 
    /// directory and file/directory</remarks>
    public void LoadCacheDir(string dirPath) {
      if (!Directory.Exists(dirPath)) {
        throw new ArgumentException(string.Format("Directory {0} doesn't exist",
          dirPath), "dirPath");
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
              AddToRegistry(nsDirInfo.Name, filesysInfo.Name, filesysInfo.FullName, 
                false);
            }
          }
        }
      }
    }

    /// <summary>
    /// Determines whether the specified path is in one of cache dirs.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>
    /// 	<c>true</c> if the specified path is in cache dir; otherwise, <c>false</c>.
    /// </returns>
    public bool IsInCacheDir(string path, bool checkPath) {
      var dirInfo = IOUtil.GetParent(path, checkPath).Parent;
      return _cacheDirs.Contains(dirInfo.FullName);
    }

    /// <summary>
    /// Determines whether the specified data is in cache registry.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <returns>
    /// 	<c>true</c> if it is in the cache registry; otherwise, <c>false</c>.
    /// </returns>
    public bool IsInCacheRegistry(string nameSpace, string name) {
      var key = ServiceUtil.GetDictKeyString(nameSpace, name);
      return _registry.ContainsKey(key);
    }

    public string GetRegisteredPath(string nameSpace, string name) {
      var key = ServiceUtil.GetDictKeyString(nameSpace, name);
      return this[key];
    }

    /// <summary>
    /// Adds the path to registry.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="checkPath">if set to <c>true</c> [check path].</param>
    public void AddPathToRegistry(string path, bool checkPath) {
      RegisterPath(path, checkPath, false);
    }

    /// <summary>
    /// Updates the path in registry. This means no exception thrown when the key 
    /// already exists.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="checkPath">if set to <c>true</c> [check path].</param>
    public void UpdatePathInRegistry(string path, bool checkPath) {
      RegisterPath(path, checkPath, true);
    }

    public void DeleteRegistryFile() {
      if (File.Exists(RegistryFilePath)) {
        File.Delete(RegistryFilePath);
      }
    } 
    #endregion

    #region Private Methods
    /// <summary>
    /// Registers a path to registry.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="checkPath">if set to <c>true</c> check wether path exists.
    /// </param>
    /// <param name="update">if set to <c>true</c>, no exception thrown if the 
    /// same key already exists.</param>
    private void RegisterPath(string path, bool checkPath, bool update) {
      var name = IOUtil.GetFileOrDirectoryName(path, checkPath);
      if (IsInCacheDir(path, checkPath)) {
        var nsDirName = IOUtil.GetParent(path, checkPath).Name;
        AddToRegistry(nsDirName, name, path, update);
      } else {
        AddToRegistry(SelfNameSpace, name, path, update);
      }
    }

    /// <summary>
    /// Adds entry to registry.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    /// <param name="update">if set to <c>true</c>, no exception thrown if the 
    /// same key already exists.</param>
    private void AddToRegistry(string nameSpace, string name, string value, bool update) {
      var key = ServiceUtil.GetDictKeyString(nameSpace, name);
      if (update) {
        _registry[key] = value;
      } else {
        _registry.Add(key, value); 
      }
      WriteToFile();
    }

    private void RemoveFromRegistry(string key) {
      _registry.Remove(key);
      WriteToFile();
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
    #endregion
  }
}
