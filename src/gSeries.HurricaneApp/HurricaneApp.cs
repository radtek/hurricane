using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GSeries.DataDistributionService;
using Ninject;
using Ninject.Extensions.Wcf;
using System.Reflection;

namespace GSeries.HurricaneApp {
    public class HurricaneApp {
        public static void Main(string[] args) {
            var kernel = new StandardKernel();
            kernel.Load(new ServiceNinjectModule());
            KernelContainer.Kernel = kernel;

            new HurricaneServiceManager().Start();
            Console.Read();
        }

    }
}
