// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php
using System.Collections;
using System.Web.Mvc;
using System.Web;
using log4net;
using System.Reflection;

namespace GSeries.Web {
    /// <summary>
    /// Handles (Logs) uncaught exceptions.
    /// </summary>
    public class ExceptionHandlerAttribute : ActionFilterAttribute {

        public override void OnActionExecuted(ActionExecutedContext filterContext) {
            ILog logger = LogManager.GetLogger(filterContext.Controller.GetType());
            if (filterContext.Exception != null && !(filterContext.Exception is HttpException)) {
                // Don't log HttpException because it should be thrown as a wrapper of another
                // exception which is already handled or logged.
                logger.ErrorFormat(
                  "Exception caught when serving request ({2}){0} from {1}: {3}",
                  filterContext.HttpContext.Request.RawUrl,
                  filterContext.HttpContext.Request.UserHostAddress,
                  filterContext.HttpContext.Request.HttpMethod,
                  filterContext.Exception);
            }
        }
    }
}
