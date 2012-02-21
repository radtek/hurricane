// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.phpusing System;

using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GSeries.ProvisionSupport;
using log4net;
using System.Reflection;
using System.Net;
using System;
using System.IO;

namespace GSeries.Web.Controllers {
    [ExceptionHandler]
    [LogRequest]
    public class FileServerController : Controller {
        LocalFileService _localFileService;
        static readonly ILog logger = LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        public FileServerController(LocalFileService localFileService) {
            _localFileService = localFileService;
        }

        //
        // GET: /FileServer/
        public ActionResult Index() {
            return View();
        }


        public ActionResult GetFileRange(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new HttpException((int)HttpStatusCode.BadRequest, 
                    "File name is not provided.");
            }

            string rangeHeader = HttpContext.Request.Headers["Range"];
            logger.DebugFormat("Range header: {0}", rangeHeader);

            if (rangeHeader == null) {
                throw new HttpException((int)HttpStatusCode.BadRequest, 
                    "This service only accepts range requests.");
            }

            //Range header is specified in this format: bytes=startByte-{EndByte}
            string[] range = rangeHeader.Split(new char[] { '=', '-' });

            if (range.Length < 3) { 
                throw new HttpException((int)HttpStatusCode.BadRequest, 
                    "This service only accepts range requests with end byte specified.");
            }

            long startByte = Convert.ToInt64(range[1]);
            long endByte = Convert.ToInt64(range[2]);

            byte[] fileRange;

            try {
                fileRange = _localFileService.ReadFile(id, startByte, endByte);
            } catch (ArgumentOutOfRangeException ex) {
                throw new HttpException((int)HttpStatusCode.RequestedRangeNotSatisfiable, "Invalid file range.", ex);
            } catch (FileNotFoundException ex) {
                throw new HttpException((int)HttpStatusCode.NotFound, "File not found.", ex);
            }
            return File(fileRange, HttpUtil.OctetStreamContentType);
        }
    }
}
