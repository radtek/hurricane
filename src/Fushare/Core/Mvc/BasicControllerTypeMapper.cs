using System;
using System.Collections.Specialized;
using System.Text;
using System.ServiceModel.Web;

namespace Fushare {
  class BasicControllerTypeMapper : IControllerTypeMapper {

    private UriTemplate _uri_template = new UriTemplate("{controllerShortName}/"
      + "{namespace}/{resourceName}/{subResourceName}");

    private NameValueCollection _controller_name_table;

    private static BasicControllerTypeMapper _instance = 
      new BasicControllerTypeMapper();

    public static BasicControllerTypeMapper Instance {
      get {
        return _instance;
      }
    }

    private BasicControllerTypeMapper() {
      // @TODO move this to a configuration class.
      _controller_name_table.Add("bt", "Fushare.BitTorrent.BitTorrentController");
      _controller_name_table.Add("dht", "Fushare.Services.DhtController");
    }

    #region IControllerTypeMapper Members
 
    /// <returns>Null if no matched controller.</returns>
    public Type GetFushareControllerType<TReq, TResp>(
      FushareContext<TReq, TResp> context) {
      Type ret;
      string params_string = context.Request.ParamString;
      Uri params_uri = new Uri(params_string);
      UriTemplateMatch match = _uri_template.Match(new Uri("/"), params_uri);
      if (match != null) {
        string controller_short_name = match.BoundVariables["controllerShortName"];
        string controller_name = _controller_name_table[controller_short_name];
        ret = Type.GetType(controller_name);
      } else {
        ret = null;
      }
      return ret;
    }

    #endregion
  }
}
