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
using System.Collections;
using System.Web.Mvc;
using System.Web;

namespace GatorShare.Web {
  /// <summary>
  /// Handles (Logs) uncaught exceptions.
  /// </summary>
  public class ExceptionHandlerAttribute : ActionFilterAttribute {

    #region ActionFilterAttribute Members
    public override void OnActionExecuted(ActionExecutedContext filterContext) {
      if (filterContext.Exception != null && ! (filterContext.Exception is HttpException)) {
        IDictionary _log_props = Logger.PrepareLoggerProperties(
          filterContext.Controller.GetType());
        // Don't log HttpException because it should be thrown as a wrapper of another
        // exception which is already handled or logged.
        Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
          "Exception caught when serving request ({2}){0} from {1}: {3}",
          filterContext.HttpContext.Request.RawUrl,
          filterContext.HttpContext.Request.UserHostAddress,
          filterContext.HttpContext.Request.HttpMethod,
          filterContext.Exception));
      }
    }
    #endregion
  }
}
