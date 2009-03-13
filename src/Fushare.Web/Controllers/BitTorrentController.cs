using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Fushare;
using Fushare.Services;
using Fushare.Services.BitTorrent;

namespace Fushare.Web.Controllers {
  public class BitTorrentController : Controller {

    static readonly IDictionary _log_props = 
      Logger.PrepareLoggerProperties(typeof(BitTorrentController));
    IBitTorrentService _service;

    public BitTorrentController(IBitTorrentService service) {
      _service = service;
    }

    public ActionResult Index(string nameSpace, string name) {
      var path = Request.Params["path"];
      if (!string.IsNullOrEmpty(path)) {
        path = path.Trim(new char[] { '"' });
        return Publish(path);
      }

      string verb = Request.HttpMethod;
      switch (verb.ToUpper()) {
        case "GET":
          return Get(nameSpace, name);
        case "POST":
          return Publish(nameSpace, name);
        default:
          return new EmptyResult();
      }
    }

    [AcceptVerbs("POST")]
    public ActionResult Publish(string nameSpace, string name) {
      _service.Publish(nameSpace, name);
      return new EmptyResult();
    }

    [AcceptVerbs("GET")]
    public ActionResult Get(string nameSpace, string name) {
      string downloadedPath;
      try {
        downloadedPath = _service.Get(nameSpace, name);
      } catch (ResourceNotFoundException ex) {
        var toThrow = new HttpException(HttpCodes.NotFound404, 
          "No file/directory available at this key.", ex);
        LogBeforeThrow(toThrow);
        throw toThrow;
      } catch (ResourceException ex) {
        var toThrow = new HttpException(HttpCodes.ServiceUnavailable503,
          "Unable to get.", ex);
        LogBeforeThrow(toThrow);
        throw toThrow;
      }
      return Content(downloadedPath);
    }

    private ActionResult Publish(string path) {
      try {
        _service.Publish(path);
      } catch (DuplicateResourceKeyException ex) {
        var toThrow = new HttpException(HttpCodes.BadRequest400, 
          "The same key already exists. Change the name.", ex);
        LogBeforeThrow(toThrow);
        throw toThrow;
      } catch (ResourceException ex) {
        var toThrow = new HttpException(HttpCodes.ServiceUnavailable503,
          "Unable to publish.", ex);
        LogBeforeThrow(toThrow);
        throw toThrow;
      }
      return new EmptyResult();
    }

    private static void LogBeforeThrow(Exception toThrow) {
      Logger.WriteLineIf(LogLevel.Error, _log_props,
        string.Format("Log this exception before throwing to client. \n{0}", toThrow));
    }
  }
}
