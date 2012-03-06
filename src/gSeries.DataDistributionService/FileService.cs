// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DataDistributionService {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Web;
    using System.Text;
    using CuttingEdge.Conditions;
    using GSeries.ProvisionSupport;
    using log4net;
    using MonoTorrent.Client;
    using MonoTorrent.Common;
    using Ninject;
    using System.Threading;

    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class FileService : IFileService {
        static readonly ILog logger = LogManager.GetLogger(
                        MethodBase.GetCurrentMethod().DeclaringType);
        readonly string _baseDir;
        DeduplicationService _dedupService;
        DistributedDiskManager _dDiskManager;

        public FileService(DistributedDiskManager dDiskManager,
            DeduplicationService dedupService,
            string baseDir) {
                _dDiskManager = dDiskManager;
            _baseDir = baseDir;
            _dedupService = dedupService;
        }

        #region IFileService Members
        public PathStatusDto GetPathStatus(string path) {
            logger.DebugFormat("Instance {1} received GetPathStatus request for path {0}", path, this.GetHashCode());

            string realPath = Path.Combine(_baseDir, path);

            if (Directory.Exists(realPath)) {
                return new PathStatusDto {
                    PathType = PathStatusDto.PathTypeEnum.Directory,
                    FileSize = -1
                };
            }

            ManagedFile mf;
            try {
                mf = _dedupService.GetManagedFileInfo(realPath);
            } catch (FileNotFoundInDbException ex) {
                throw new FaultException<DataDistributionServiceException>(
                    new DataDistributionServiceException("File cannot be found.", ex));
            }

            return new PathStatusDto {
                FileSize = mf.Size,
                PathType = PathStatusDto.PathTypeEnum.File
            };
        }

        public byte[] Read(string path, long offset, int count) {
            logger.DebugFormat("Instance {1} received Read request for path {0}", path, this.GetHashCode());
            string realPath = Path.Combine(_baseDir, path);

            try {
                // Translate offset, count based on the (virtual) file indices to
                // file parts based on chunk indices.
                List<Tuple<long, int>> chunkList =
                    _dedupService.MapFileIndicesToChunkIndices(path, offset, count);

                logger.DebugFormat("To serve request for virtual file segment " +
                    "(offset, count)=({0}, {1}), the following real file " +
                    "segments are needed: [{2}]", offset, count, string.Join(", ", 
                    chunkList.ConvertAll(x => string.Format("({0}, {1})", 
                        x.Item1, x.Item2)).ToArray()));

                return _dDiskManager.ReadFile(realPath, chunkList);
            } catch (Exception ex) {
                // TODO: Add more catches.
                throw new FaultException<DataDistributionServiceException>(
                    new DataDistributionServiceException(
                        "Error occurred while reading the file.", ex));
            }
        }

        public string Echo(string message) {
            return message;
            //throw new FaultException<ArgumentException>(new ArgumentException("test"), "test reason");
        }
        #endregion
    }
}
