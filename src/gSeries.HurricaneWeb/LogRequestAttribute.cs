// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections;
using System.Reflection;
using log4net;

namespace GSeries.Web {
    public class LogRequestAttribute : ActionFilterAttribute {
        /// <summary>
        /// Log before serving the request.
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext filterContext) {
            ILog logger = LogManager.GetLogger(filterContext.Controller.GetType());

            logger.DebugFormat("Received request ({2}) {0} from {1}",
              filterContext.HttpContext.Request.RawUrl,
              filterContext.HttpContext.Request.UserHostAddress,
              filterContext.HttpContext.Request.HttpMethod);
        }

        /// <summary>
        /// Log after serving the request.
        /// </summary>
        public override void OnActionExecuted(ActionExecutedContext filterContext) {
            ILog logger = LogManager.GetLogger(filterContext.Controller.GetType());

            logger.DebugFormat("Finished serving request ({2}) {0} from {1}",
              filterContext.HttpContext.Request.RawUrl,
              filterContext.HttpContext.Request.UserHostAddress,
              filterContext.HttpContext.Request.HttpMethod);
        }
    }
}