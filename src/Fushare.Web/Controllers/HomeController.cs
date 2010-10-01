using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Fushare.Web.Controllers {
  [HandleError]
  public class HomeController : Controller {
    public ActionResult Index1() {
      return Content("Welcome to GatorShare.");
    }

    public ActionResult Index() {
      return View("/Views/Home/Index.aspx");
    }
  }
}
