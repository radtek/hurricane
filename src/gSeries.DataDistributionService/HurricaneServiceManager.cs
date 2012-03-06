// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DataDistributionService {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.ServiceModel.Web;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using log4net;
    using System.Reflection;
    using ProtoBuf.ServiceModel;
    using Ninject.Extensions.Wcf;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class HurricaneServiceManager {
        static readonly ILog logger = LogManager.GetLogger(
                        MethodBase.GetCurrentMethod().DeclaringType);
        public ServiceEndpoint WebHttpServiceEndpoint { get; private set; }
        public ServiceEndpoint NetNamedPipeServiceEndpoint { get; private set; }
        WebServiceHost _serviceHost;

        public HurricaneServiceManager() {
            _serviceHost = new NinjectWebServiceHost(typeof(FileService));
            ServiceEndpoint se = _serviceHost.AddServiceEndpoint(typeof(IFileService),
                new WebHttpBinding(), "http://localhost:18081/FileService/");
            ServiceEndpoint se1 = _serviceHost.AddServiceEndpoint(typeof(IFileService),
                new NetNamedPipeBinding(), "net.pipe://localhost/FileService/");
            se1.Behaviors.Add(new ProtoEndpointBehavior());
            WebHttpServiceEndpoint = se;
            NetNamedPipeServiceEndpoint = se1;
        }

        public void Start() {
            bool openSucceeded = false;
            try {
                _serviceHost.Open();
                openSucceeded = true;
                logger.Info("Service is started.");
            } catch (Exception ex) {
                logger.ErrorFormat("Error caught from service host. {0}", ex);
            } finally {
                if (!openSucceeded)
                    _serviceHost.Abort();
            }
        }
    }
}
