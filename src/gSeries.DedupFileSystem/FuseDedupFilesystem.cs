// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DedupFileSystem {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Mono.Fuse;
    using Mono.Unix.Native;
    using GSeries.Filesystem;
    using GSeries.DataDistributionService;
    using System.ServiceModel;
    using GSeries.ProvisionSupport;
    using log4net;
    using System.Reflection;
    using System.Net;

    /// <summary>
    /// This class is responsible for "intercepting" FUSE syscalls and delegating
    /// de-duplication tasks to <see cref="DedupFilesystemHelper"/>.
    /// </summary>
    public class FuseDedupFilesystem : FileSystem {
        static readonly ILog logger = LogManager.GetLogger(
                        MethodBase.GetCurrentMethod().DeclaringType);
        IFileService _fileService;

        public FuseDedupFilesystem(IFileService fileService) {
            _fileService = fileService;
        }

        protected override Errno OnGetPathStatus(string path, out Stat stat) {
            logger.DebugFormat("OnGetPathStatus: {0}", path);
            PathStatusDto status;
            stat = new Stat();
            try {
                status = _fileService.GetPathStatus(path);

                if (status.PathType == PathStatusDto.PathTypeEnum.Directory) {
                    stat.st_mode = FilePermissions.S_IFDIR |
                        // Readonly directory
                        NativeConvert.FromOctalPermissionString("0555");
                    stat.st_nlink = 2;
                } else if (status.PathType == PathStatusDto.PathTypeEnum.File) {
                    stat.st_mode = FilePermissions.S_IFREG |
                        // Readonly file.
                        NativeConvert.FromOctalPermissionString("0444");
                    stat.st_nlink = 2;
                    stat.st_size = status.FileSize;
                }
                return 0;
            } catch (FileNotFoundInServiceException) {
                logger.DebugFormat("File not found in service.");
                return Errno.ENOENT;
            } catch (WebException ex) {
                logger.ErrorFormat("Caught WebException: {0} : {1}", ex.Status, ex);
                return Errno.ENONET;
            } catch (Exception ex) {
                logger.ErrorFormat("Exception caught while interacting with File Service : {0}", ex);
                // TODO: Other error more appropriate?
                return Errno.ENONET;
            }

        }

        protected override Errno OnOpenHandle(string file, OpenedPathInfo info) {
            if (info.OpenAccess != OpenFlags.O_RDONLY)
                return Errno.EACCES;
            return 0;
        }

        protected override Errno OnFlushHandle(string file, OpenedPathInfo info) {
            return 0;
        }

        protected override Errno OnReleaseHandle(string file, OpenedPathInfo info) {
            return base.OnReleaseHandle(file, info);
        }

        protected override Errno OnReadHandle(string file, OpenedPathInfo info, 
            byte[] buf, long offset, out int bytesRead) {
            logger.DebugFormat("OnReadHandle: {0}, {1}, {2}", file, offset, buf.Length);

            byte[] readData;
            try {
                readData = _fileService.Read(file, offset, buf.Length);
                Buffer.BlockCopy(readData, 0, buf, 0, readData.Length);
                bytesRead = readData.Length;
                return 0;
            } catch (FaultException<DataDistributionServiceException> ex) {
                logger.ErrorFormat("File reading failed: {0}", ex);
                bytesRead = 0;
                return Errno.EAGAIN;
            } catch (Exception ex) {
                logger.ErrorFormat("Some error occurred during reading handle: {0}", ex);
                bytesRead = 0;
                return Errno.EPERM;
            }

        }
    }
}
