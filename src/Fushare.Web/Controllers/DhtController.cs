using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using Fushare.Services;
using Fushare.Services.Dht;
using System.Net;

namespace Fushare.Web.Controllers {
  public class DhtController : Controller {
    #region Fields

    static readonly IDictionary _log_props =
      Logger.PrepareLoggerProperties(typeof(DhtController));

    IDhtService _dhtService; 
    #endregion

    public DhtController(IDhtService dhtService) {
      _dhtService = dhtService;
    }

    public ActionResult Index(string nameSpace, string name) {
      switch (Request.HttpMethod.ToUpper()) {
        case "PUT":
          return Put(nameSpace, name);
        case "GET":
          // Allow a non-RESTful backdoor: specify value={val} to put values to Dht
          string value  = Request.QueryString["value"];
          if (!string.IsNullOrEmpty(value)) {
            var valueBytes = Encoding.UTF8.GetBytes(value);
            return PutInternal(nameSpace, name, valueBytes);
          } else {
            return Get(nameSpace, name);
          }
        default:
          return new EmptyResult();
      }
    }

    [AcceptVerbs("GET")]
    public ActionResult Get(string nameSpace, string name) {
      byte[] retBytes;
      try {
        retBytes = _dhtService.Get(nameSpace, name);
      } catch (ResourceNotFoundException ex) {
        var toThrow = new HttpException((int)HttpStatusCode.NotFound, 
          "No value associated with this key.", ex);
        Util.LogBeforeThrow(toThrow, _log_props);
        throw toThrow;
      }
      return File(retBytes, HttpUtil.OctetStreamContentType);
    }

    /// <summary>
    /// Puts content of the HTTP request to Dht
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <returns>EmptyResult</returns>
    /// <remarks>Param "action=create" triggers Dht Create instead of Put
    /// </remarks>
    [AcceptVerbs("PUT")]
    public ActionResult Put(string nameSpace, string name) {
      var inputBytes = Request.BinaryRead(Request.ContentLength);
      if (inputBytes == null) {
        var toThrow = new HttpException((int)HttpStatusCode.BadRequest,
          "The value should not be null.");
        Util.LogBeforeThrow(toThrow, _log_props);
        throw toThrow;
      }
      return PutInternal(nameSpace, name, inputBytes);
    }

    ActionResult PutInternal(string nameSpace, string name, byte[] value) {
      var actionStr = Request.Params["action"];
      if (string.Equals(actionStr, "create", StringComparison.OrdinalIgnoreCase)) {
        try {
          _dhtService.Create(nameSpace, name, value);
        } catch (DuplicateResourceKeyException ex) {
          var toThrow = new HttpException((int)HttpStatusCode.BadRequest, 
            ex.Message, ex);
          Util.LogBeforeThrow(toThrow, _log_props);
          throw toThrow;
        }
      } else {
        _dhtService.Put(nameSpace, name, value);
      }
      return new EmptyResult();
    }
  }
}
