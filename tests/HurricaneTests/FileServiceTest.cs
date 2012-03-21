// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using log4net.Config;
    using System.IO;
    using GSeries.DataDistributionService;
    using Ninject;
    using Ninject.Extensions.Wcf;
    using System.ServiceModel;
    using GSeries.ProvisionSupport;
    using GSeries.BitTorrent;
    using MonoTorrent.Client;
    using MonoTorrent.Client.Encryption;
    using System.Net;
    using MonoTorrent.Common;
    using log4net;
    using System.Reflection;
    using NUnit.Framework;
    using System.Threading;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FileServiceTest {
        static readonly ILog logger = LogManager.GetLogger(
                        MethodBase.GetCurrentMethod().DeclaringType);
        static FileServiceTest() {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("NHibernate.Debug.config.xml"));
        }

        public static void TestReadFile(string db, string torrentPath, string baseDir, string filePath, string origianlFile) {
            //System.Diagnostics.Debugger.Launch();
            var kernel = new StandardKernel();
            kernel.Load(new ServiceNinjectModule());
            
            kernel.Bind<FileInfoTable<TorrentManager>>().ToSelf().InSingletonScope();

            var dbs = new ChunkDbService(db, false);
            var ds = new DeduplicationService(dbs);
            kernel.Bind<DeduplicationService>().ToConstant(ds).InSingletonScope();

            var writer = new DedupDiskWriter(ds);

            var engineSettings = new EngineSettings();
            engineSettings.PreferEncryption = false;
            engineSettings.AllowedEncryption = EncryptionTypes.All;
            int port = 33123;
            var ip = NetUtil.GetLocalIPByInterface("Local Area Connection");
            engineSettings.ReportedAddress = new IPEndPoint(ip, port);
            var engine = new ClientEngine(engineSettings, new DedupDiskWriter(ds));
            
            kernel.Bind<DiskManager>().ToConstant(engine.DiskManager).InSingletonScope();
            kernel.Bind<ClientEngine>().ToConstant(engine).InSingletonScope();

            kernel.Bind<DistributedDiskManager>().ToSelf();
            kernel.Bind<FileService>().ToSelf().WithConstructorArgument("baseDir", baseDir);

            kernel.Bind<VirtualDiskDownloadService>().ToSelf();
            var vd = kernel.Get<VirtualDiskDownloadService>();

            var torrent = Torrent.Load(torrentPath);
            logger.DebugFormat("Loaded torrent file: {0}, piece length: {1}.",
                torrent.Name, torrent.PieceLength);
            vd.StartDownloadingFile(torrent, baseDir);

            KernelContainer.Kernel = kernel;

            var m = new HurricaneServiceManager();
            m.Start();

            var url = string.Format("http://{0}:18081/FileService/", ip.ToString());
            logger.DebugFormat("Contacting service at {0}", url);
            var client = new ManualFileServiceClient(url);
            string tstmsg = "tstmsg";
            var resp = client.Echo(tstmsg);
            Assert.AreEqual(resp, tstmsg);
            
            byte[] resultData = null;

            try {
                var pathStatus = client.GetPathStatus(filePath);
                logger.DebugFormat("File size: {0}", pathStatus.FileSize);
            } catch (Exception ex) {
                logger.Error(ex);
            }

            try {
                // can only read 49352.
                resultData = client.Read(filePath, 0, 49253);
            } catch (Exception ex) {
                logger.Error(ex);
            }

            var actualData = IOUtil.Read(origianlFile, 0, 49252);

            Assert.IsTrue(actualData.SequenceEqual(resultData),
                "File part should match.");
            logger.Debug("Read succeeded.");

            Console.Read();
        }


        public static void TestStartHurricaneServiceManager() {
            System.Diagnostics.Debugger.Launch();
            var m = new HurricaneServiceManager();
            m.Start();
            var p = new WcfProxy<IFileService>(m.NetNamedPipeServiceEndpoint);
            try {
                var pathStatus = p.Service.Echo("test");
                //Console.WriteLine(pathStatus.PathType);
            } catch (FaultException<ArgumentException> ex) {
                Console.WriteLine(ex);
            }
            Console.Read();
        }
    }
}
