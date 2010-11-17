using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using GatorShare.External.DictionaryService;

namespace GatorShare.Services.BitTorrent {
  [TestFixture]
  public class CacheRegistryTest {
    CacheRegistry _cacheRegistry;

    [SetUp]
    public void SetUp() {
      var registryFile = Path.Combine(Path.GetTempPath(), 
        Path.GetRandomFileName());
      var nsName = Path.GetRandomFileName();
      _cacheRegistry = new CacheRegistry(registryFile, nsName);
    }

    [TearDown]
    public void TearDown() {
      _cacheRegistry.DeleteRegistryFile();
    }

    [Test]
    public void Test() {
      string filename = Path.GetRandomFileName();
      string path = Path.Combine(Path.GetTempPath(), filename);
      _cacheRegistry.AddPathToRegistry(path, false);
      Assert.AreEqual(path, _cacheRegistry[filename]);
    }

    /// <summary>
    /// Tests whether the class can recognize paths outside and inside cache dirs 
    /// and make correct registrations.
    /// </summary>
    [Test]
    public void TestLoadCacheDirAndRegisterPath() {
      // Create cache dir
      string cacheDirPath = Path.Combine(Path.GetTempPath(),
        Path.GetRandomFileName());
      var cacheDirInfo = Directory.CreateDirectory(cacheDirPath);
      // Create several sub-directories
      var ns1DirInfo = cacheDirInfo.CreateSubdirectory("ns1");
      string ns1file1Path = Path.Combine(ns1DirInfo.FullName, "file1");
      File.WriteAllText(ns1file1Path, "content1");

      var registryFile = Path.Combine(Path.GetTempPath(), 
        Path.GetRandomFileName());
      var cacheRegistry = new CacheRegistry(registryFile, "myns");
      // Load...
      cacheRegistry.LoadCacheDir(cacheDirPath);

      var keyStr = ServiceUtil.GetDictKeyString("ns1", "file1");
      var actualVal = cacheRegistry[keyStr];
      Assert.AreEqual(ns1file1Path, actualVal);

      // Register a path inside cache dir
      var fileInsideCache = Path.Combine(ns1DirInfo.FullName, "file2");
      cacheRegistry.AddPathToRegistry(fileInsideCache, false);
      var actualVal1 = cacheRegistry[ServiceUtil.GetDictKeyString("ns1", "file2")];
      Assert.AreEqual(fileInsideCache, actualVal1);

      // Register one outside the cache dir
      var fileOusideCache = IOUtil.GetRandomTempPath();
      cacheRegistry.AddPathToRegistry(fileOusideCache, false);
      var actualVal2 = cacheRegistry[ServiceUtil.GetDictKeyString("myns", 
        IOUtil.GetFileOrDirectoryName(fileOusideCache, false))];
      Assert.AreEqual(fileOusideCache, actualVal2);
    }
  }
}
