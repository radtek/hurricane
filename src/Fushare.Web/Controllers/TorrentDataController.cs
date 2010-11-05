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
using Fushare.Services.BitTorrent;
using System.IO;

namespace Fushare.Web.Controllers {
  /// <summary>
  /// Controller for Torrent Data Service.
  /// </summary>
  [ExceptionHandler]
  public class TorrentDataController : Controller {
    readonly TorrentDataService _torrentDataService;

    public TorrentDataController(TorrentDataService torrentDataService) {
      _torrentDataService = torrentDataService;
    }

    public ActionResult Index() {
      return View();
    }

    /// <summary>
    /// Serves the torrent file downloads.
    /// </summary>
    [AcceptVerbs(HttpVerbs.Get)]
    public ActionResult TorrentFile(string nameSpace, string name) {
      string fileName;
      Stream stream = _torrentDataService.LoadTorrentFile(nameSpace, name, out fileName);
      return File(stream, HttpUtil.OctetStreamContentType, fileName);
    }
  }
}
