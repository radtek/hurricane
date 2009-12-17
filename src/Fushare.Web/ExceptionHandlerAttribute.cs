using System.Collections;
using System.Web.Mvc;
using System.Web;

namespace Fushare.Web {
  /// <summary>
  /// Handles (Logs) uncaught exceptions.
  /// </summary>
  public class ExceptionHandlerAttribute : ActionFilterAttribute {
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(ExceptionHandlerAttribute));

    #region ActionFilterAttribute Members
    public override void OnActionExecuted(ActionExecutedContext filterContext) {
      if (filterContext.Exception != null && ! (filterContext.Exception is HttpException)) {
        // Don't log HttpException because it should be thrown as a wrapper of another
        // exception which is already handled or logged.
        Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
          "Exception caught: {0}", filterContext.Exception));
      }
    }
    #endregion
  }
}
