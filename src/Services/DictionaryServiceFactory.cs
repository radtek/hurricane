using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Reflection;
#if FUSHARE_NUNIT
using NUnit.Framework; 
#endif

namespace Fushare.Services {
  public class DictionaryServiceFactory {
    //<Type, Url>
    private static IDictionary<Type, Uri> _registered = new Dictionary<Type, Uri>();
    //<Type, IDictionaryService>
    private static IDictionary<Type, IDictionaryService> _loaded = new Dictionary<Type, IDictionaryService>();

    public static void RegisterServiceType(Type type, Uri uri) {
      _registered.Add(type, uri);
    }

    public static IDictionaryService GetServiceInstance(Type type) {
      IDictionaryService ds;
      if (!_loaded.TryGetValue(type, out ds)) {
        Load();
      }
      return (IDictionaryService)_loaded[type];
    }

    private static void Load() {
      foreach (KeyValuePair<Type, Uri> entry in _registered) {
        if (!_loaded.ContainsKey(entry.Key)) {
          _loaded.Add(entry.Key, (IDictionaryService)Activator.CreateInstance(entry.Key, new object[] { entry.Value }));
        }
      }
    }
  }

#if FUSHARE_NUNIT
  [TestFixture]
  public class DictionaryServiceFactoryTest {
    [Test]
    public void TestGetServiceInstance() {
      DictionaryServiceFactory.RegisterServiceType(typeof(BrunetDht), new Uri(@"http://127.0.0.1:15151/xd.rem"));
      IDictionaryService svc = DictionaryServiceFactory.GetServiceInstance(typeof(BrunetDht));
    }
  } 
#endif
}
