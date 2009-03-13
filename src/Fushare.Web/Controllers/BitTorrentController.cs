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
using System.Collections.Specialized;

namespace Fushare.Web.Controllers {
  public class BitTorrentController : Controller {

    static readonly IDictionary _log_props = 
      Logger.PrepareLoggerProperties(typeof(BitTorrentController));
    IBitTorrentService _service;

    public BitTorrentController(IBitTorrentService service) {
      _service = service;
    }

    public ActionResult Index(string nameSpace, string name) {
      if (!string.IsNullOrEmpty(Request.Params["path"])) {
        // This is allowed for both GET and POST.
        return PublishInternal(nameSpace, name);
      }

      string verb = Request.HttpMethod;
      switch (verb.ToUpper()) {
        case "GET":
          return Get(nameSpace, name);
        case "POST":
          return PublishInternal(nameSpace, name);
        default:
          return new EmptyResult();
      }
    }

    [AcceptVerbs("POST")]
    public ActionResult Publish(string nameSpace, string name) {
      return PublishInternal(nameSpace, name);
    }

    [AcceptVerbs("GET")]
    public ActionResult Get(string nameSpace, string name) {
      DataMetaInfo meta;
      try {
        meta = _service.Get(nameSpace, name);
      } catch (ResourceNotFoundException ex) {
        var toThrow = new HttpException(HttpCodes.NotFound404, 
          "No file/directory available at this key.", ex);
        Util.LogBeforeThrow(toThrow, _log_props);
        throw toThrow;
      } catch (ResourceException ex) {
        var toThrow = new HttpException(HttpCodes.ServiceUnavailable503,
          "Unable to get.", ex);
        Util.LogBeforeThrow(toThrow, _log_props);
        throw toThrow;
      }

      // @TODO Check the location of the client and return the path correspondently.
      var xmlString = XmlUtil.ToXml<DataMetaInfo>(meta);
      return Content(xmlString);
    }

    private ActionResult PublishInternal(string nameSpace, string name) {
      try {
        var path = Request.Params["path"];
        var publisher = Publisher.CreatePublisher(Request.Params, this);
        if (!string.IsNullOrEmpty(path)) {
          // If the client quotes the path, we trim the quotation marks.
          path = path.Trim(new char[] { '"' });
          publisher.Execute(path);
        } else {
          publisher.Execute(nameSpace, name);
        }
      } catch (DuplicateResourceKeyException ex) {
        var toThrow = new HttpException(HttpCodes.BadRequest400, 
          "The same key already exists. Change the name.", ex);
        Util.LogBeforeThrow(toThrow, _log_props);
        throw toThrow;
      } catch (ResourceException ex) {
        var toThrow = new HttpException(HttpCodes.ServiceUnavailable503,
          "Unable to publish.", ex);
        Util.LogBeforeThrow(toThrow, _log_props);
        throw toThrow;
      }
      return new EmptyResult();
    }

    class Publisher {
      protected BitTorrentController _parent;

      internal static Publisher CreatePublisher(NameValueCollection parameters, 
        BitTorrentController parent) {
        if("update".Equals(parameters["action"], StringComparison.OrdinalIgnoreCase)) {
          return new Updater(parent);
        } else {
          return new Publisher(parent);
        }
      }

      protected internal Publisher(BitTorrentController parent) {
        _parent = parent;
      }

      internal protected virtual void Execute(string nameSpace, string name) {
        _parent._service.Publish(nameSpace, name);
      }

      internal protected virtual void Execute(string path) {
        _parent._service.Publish(path);
      }
    }

    class Updater : Publisher {
      protected internal Updater(BitTorrentController parent) : base(parent) { }

      internal protected override void Execute(string nameSpace, string name) {
        _parent._service.Update(nameSpace, name);
      }

      internal protected override void Execute(string path) {
        _parent._service.Update(path);
      }
    }
  }
}
