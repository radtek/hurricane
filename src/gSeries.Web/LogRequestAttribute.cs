/*
Copyright (c) 2010 Jiangyan Xu <jiangyan@ufl.edu>, University of Florida

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections;

namespace GSeries.Web {
  public class LogRequestAttribute : ActionFilterAttribute {

    /// <summary>
    /// Log before serving the request.
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext filterContext) {
      IDictionary _log_props = Logger.PrepareLoggerProperties(
        filterContext.Controller.GetType());

      Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
        "Received request ({2}){0} from {1}",
        filterContext.HttpContext.Request.RawUrl,
        filterContext.HttpContext.Request.UserHostAddress,
        filterContext.HttpContext.Request.HttpMethod));
    }

    /// <summary>
    /// Log after serving the request.
    /// </summary>
    public override void OnActionExecuted(ActionExecutedContext filterContext) {
      IDictionary _log_props = Logger.PrepareLoggerProperties(
        filterContext.Controller.GetType());

      Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
        "Finished serving request ({2}){0} from {1}",
        filterContext.HttpContext.Request.RawUrl,
        filterContext.HttpContext.Request.UserHostAddress,
        filterContext.HttpContext.Request.HttpMethod));
    }
  }
}