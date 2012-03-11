// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DedupFileSystem {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using GSeries.DataDistributionService;
    using System.ServiceModel.Description;
    using System.ServiceModel;
    using NDesk.Options;
    using log4net.Config;
    using System.IO;
    using log4net;
    using System.Reflection;

    /// <summary>
    /// The entry point for the FUSE client app.
    /// </summary>
    public class FuseMain {
        static readonly ILog logger = LogManager.GetLogger(
                        MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args) {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("NHibernate.Debug.config.xml"));
            string fileServiceHostAddr = null;
            var p = new OptionSet() {
                { "a|addr=", v => { fileServiceHostAddr = v; 
                    logger.InfoFormat("File service remote address: {0}", v); }}
            };
            List<string> extra = p.Parse(args);

            if (string.IsNullOrEmpty(fileServiceHostAddr)) {
                fileServiceHostAddr = "localhost";
            }

            //var proxy = new WcfProxy<IFileService>(
            //        new WebHttpBinding(),
            //        new EndpointAddress(string.Format(
            //            "http://{0}:18081/FileService/", 
            //            fileServiceHostAddr)));
            //proxy.Endpoint.Behaviors.Add(new WebHttpBehavior());
            //ChannelFactory<IFileService> cf = new ChannelFactory<IFileService>(new WebHttpBinding(), string.Format(
            //            "http://{0}:18081/FileService/",
            //            fileServiceHostAddr));
            //cf.Endpoint.Behaviors.Add(new WebHttpBehavior());
            //var proxy = cf.CreateChannel();

            var proxy = new ManualFileServiceClient(string.Format(
                        "http://{0}:18081/FileService/", fileServiceHostAddr));

            using (var fs = new FuseDedupFilesystem(proxy)) {
                string[] unhandled = fs.ParseFuseArguments(args);
                foreach (string key in fs.FuseOptions.Keys) {
                    logger.InfoFormat("FUSE Option: {0}={1}", key, fs.FuseOptions[key]);
                }
                string mountingPoint = unhandled[unhandled.Length - 1];
                logger.InfoFormat("Mounting Point: {0}", mountingPoint);
                fs.MountPoint = mountingPoint;
                fs.EnableFuseDebugOutput = true;
                fs.Start();
            }
        }
    }
}
