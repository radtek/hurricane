/*
Copyright (c) 2010 Jiangyan Xu <jiangyan@ufl.edu>, University of Florida

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Reflection;
using GSeries.External.DictionaryService;
#if FUSHARE_NUNIT
using NUnit.Framework; 
#endif

namespace GSeries.External.DictionaryService {
  /// <summary>
  /// Creates and maintains singleton instances of IDict.
  /// </summary>
  public class DictionaryServiceFactory {
    //<Type, Url>
    private static IDictionary<Type, Uri> _registered = new Dictionary<Type, Uri>();
    //<Type, IDict>
    private static IDictionary<Type, IDict> _loaded 
        = new Dictionary<Type, IDict>();

    /// <summary>
    /// Register the serivce type with its URI.
    /// </summary>
    /// <param name="type">Type in full name</param>
    /// <param name="uri">URI of the service</param>
    public static void RegisterServiceType(Type type, Uri uri) {
      _registered.Add(type, uri);
    }

    /// <summary>
    /// Returns the singleton instance of the type. The method loads all the 
    /// registered type the first time it's called.
    /// </summary>
    public static IDict GetServiceInstance(Type type) {
      IDict ds;
      if (!_loaded.TryGetValue(type, out ds)) {
        Load();
      }
      return (IDict)_loaded[type];
    }

    private static void Load() {
      foreach (KeyValuePair<Type, Uri> entry in _registered) {
        if (!_loaded.ContainsKey(entry.Key)) {
          //Initialize the instance using the service URI.
          _loaded.Add(entry.Key, (IDict)Activator.CreateInstance(
              entry.Key, new object[] { entry.Value }));
        }
      }
    }
  }

#if FUSHARE_NUNIT
  [TestFixture]
  public class DictionaryServiceFactoryTest {
    [Test]
    public void TestGetServiceInstance() {
      DictionaryServiceFactory.RegisterServiceType(typeof(BrunetDht), 
          new Uri(@"http://127.0.0.1:15151/xd.rem"));
      IDictionaryService svc = DictionaryServiceFactory.GetServiceInstance(
          typeof(BrunetDht));
    }
  } 
#endif
}
