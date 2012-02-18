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
using System.Web.Mvc;
using GSeries.External.DictionaryService;
using GSeries.Services.BitTorrent;
using GSeries.Services.Dict;
using Microsoft.Practices.Unity;
using System.Configuration;
using System;

namespace GSeries.Web {
  public static class Bootstrapper {
    public static void ConfigureUnityContainer(IUnityContainer container) {
      // Registrations
      container.RegisterType<IDictService, DictService>(
        new HttpContextLifetimeManager<IDictService>());

      string dictSvcChoice = ConfigurationManager.AppSettings["DictionaryService"];
      if (dictSvcChoice.Equals("BrunetDht")) {
        string brunetDhtSvcHost =
          ConfigurationManager.AppSettings["BrunetDhtServiceHost"];
        int brunetDhtSvcPort = Int32.Parse(
          ConfigurationManager.AppSettings["BrunetDhtServicePort"]);
        string brunetDhtSvcPath =
          ConfigurationManager.AppSettings["BrunetDhtServicePath"];
        var brunetDhtSvc = new BrunetDhtService(
          brunetDhtSvcHost, brunetDhtSvcPort, brunetDhtSvcPath);
        container.RegisterInstance<DictionaryServiceBase>(brunetDhtSvc);
      } else {
        container.RegisterType<DictionaryServiceBase, SimpleStorageDictionary>(
          new ContainerControlledLifetimeManager());
      }

      container.RegisterType<IBitTorrentService, BitTorrentService>(
        new HttpContextLifetimeManager<IBitTorrentService>());

      // Set factory
      ControllerBuilder.Current.SetControllerFactory(
        new UnityControllerFactory(container));

      GSeries.Bootstrapper.ConfigureUnityContainer(container);
    }
  }
}
