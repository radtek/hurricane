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
    using System.Net;
    using System.Security.Cryptography;

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

        public string ToServerPath(string requestPath) {
            requestPath = requestPath.TrimStart('/');
            return Path.Combine(_baseDir, requestPath);
        }

        #region IFileService Members
        public PathStatusDto GetPathStatus(string path) {
            logger.DebugFormat("Instance {1} received GetPathStatus request for path {0}", path, this.GetHashCode());

            string serverPath = ToServerPath(path);

            if (Directory.Exists(serverPath)) {
                return new PathStatusDto {
                    PathType = PathStatusDto.PathTypeEnum.Directory,
                    FileSize = -1
                };
            }

            ManagedFile mf;
            try {
                mf = _dedupService.GetManagedFileInfo(serverPath);
            } catch (FileNotFoundInDbException ex) {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
                return null;
            }

            return new PathStatusDto {
                FileSize = mf.Size,
                PathType = PathStatusDto.PathTypeEnum.File
            };
        }

        public byte[] Read(string path, string offsetStr, string countStr) {
            long offset;
            int count;
            try {
                offset = long.Parse(offsetStr);
                count = int.Parse(countStr);
            } catch (Exception) {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = "Invalid offset or count";
                return null;
            }
            return Read(path, offset, count);
        }

        public byte[] Read(string path, long offset, int count) {
            logger.DebugFormat("Instance {1} received Read request for path {0}", path, this.GetHashCode());
            string serverPath = ToServerPath(path);

            try {
                // Translate offset, count based on the (virtual) file indices to
                // file parts based on chunk indices.
                List<Tuple<long, int>> chunkList =
                    _dedupService.MapFileIndicesToChunkIndices(serverPath, offset, count);

                logger.DebugFormat("To serve request for virtual file segment " +
                    "(offset, count)=({0}, {1}), the following real file " +
                    "segments are needed: [{2}]", offset, count, string.Join(", ",
                    chunkList.ConvertAll(x => string.Format("({0}, {1})",
                        x.Item1, x.Item2)).ToArray()));

                var content = _dDiskManager.ReadFile(serverPath, chunkList);
                return content;

            } catch (FileNotFoundInDbException ex) {
                logger.ErrorFormat("File not found.", ex);
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = "[FileService] File not found in service.";
                return null;
            } catch(ArgumentException ex) {
                logger.ErrorFormat("Some invalid argument within the request.", ex);
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = ex.Message;
                return null;
            } catch (Exception ex) {
                // TODO: Add more catches.
                logger.ErrorFormat("Error occurred while reading the file.", ex);
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = ex.Message;
                return null;
            }
        }

        public string Echo(string message) {
            return message;
        }

        public void Error() {
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotImplemented;
            WebOperationContext.Current.OutgoingResponse.StatusDescription = "The error you asked for.";
        }
        #endregion
    }
}
