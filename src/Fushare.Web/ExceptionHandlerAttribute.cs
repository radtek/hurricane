using System.Collections;
using System.Web.Mvc;

namespace Fushare.Web {
  public class ExceptionHandlerAttribute : ActionFilterAttribute {
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(ExceptionHandlerAttribute));

    #region ActionFilterAttribute Members
    public override void OnActionExecuted(ActionExecutedContext filterContext) {
      if (filterContext.Exception != null) {
        Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
          "Exception caught: {0}", filterContext.Exception));
      }
    }
    #endregion
  }
}
