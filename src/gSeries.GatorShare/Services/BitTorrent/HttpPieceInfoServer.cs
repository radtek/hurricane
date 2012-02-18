using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Net;
using MonoTorrent.Common;
using System.IO;

namespace GSeries.Services.BitTorrent {
  class HttpPieceInfoServer : IPieceInfoServer {
    HttpListener _httpListener = new HttpListener();
    PieceLevelTorrentManager _pieceTorrentManager;
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(HttpPieceInfoServer));
    public const string ControllerSegment = "PieceInfo";
    public int HttpListeningPort { get; private set; }

    HttpPieceInfoServer(string listeningPrefix, 
      PieceLevelTorrentManager pieceTorrentManager) {
      _httpListener.Prefixes.Add(listeningPrefix);
      _pieceTorrentManager = pieceTorrentManager;
    }

    public HttpPieceInfoServer(int listeningPort,
      PieceLevelTorrentManager pieceTorrentManager)
      : this(string.Format("http://*:{0}/", listeningPort), pieceTorrentManager) {
      HttpListeningPort = listeningPort;
    }

    public void Start() {
      _httpListener.Start();
      _httpListener.BeginGetContext(EndGetRequest, _httpListener);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("HttpPieceInfoServer started at: {0}", HttpListeningPort));
    }

    void EndGetRequest(IAsyncResult result) {
      HttpListenerContext context = null;
      HttpListener listener = (System.Net.HttpListener)result.AsyncState;

      try {
        context = listener.EndGetContext(result);
        HandleRequest(context);
      } catch (Exception ex) {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
          string.Format("Exception thrown while processing request: {0}", ex));
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
      } finally {
        // No matter what happens, we continue to serve the next request.
        if (context != null)
          context.Response.Close();

        if (_httpListener.IsListening)
          _httpListener.BeginGetContext(EndGetRequest, listener);
      }

    }

    void HandleRequest(HttpListenerContext context) {
      var url = context.Request.Url;
      Logger.WriteLineIf(LogLevel.Info, _log_props,
        string.Format("Received request for {0} from {1}", url, 
        context.Request.RemoteEndPoint));

      UriTemplate uriTemplate = new UriTemplate(ControllerSegment + 
        "/{nameSpace}/{name}/{piece}");
      // It doesn't matter which hostname is used here.
      UriTemplateMatch match = uriTemplate.Match(new Uri(string.Format(
        "http://{0}/", context.Request.UserHostAddress)), url);
      if (match != null) {
        try {
          var torrentBytes = GetPieceTorrent(match.BoundVariables[0],
                match.BoundVariables[1],
            // Let the exception be caught by the caller.
                Int32.Parse(match.BoundVariables[2]));

          context.Response.ContentType = "text/plain";
          context.Response.StatusCode = (int)HttpStatusCode.OK;
          context.Response.ContentLength64 = torrentBytes.Length;
          context.Response.OutputStream.Write(torrentBytes, 0, torrentBytes.Length);
          Logger.WriteLineIf(LogLevel.Info, _log_props,
            string.Format("Successfully handled request for {0} from {1}", url,
            context.Request.RemoteEndPoint));
        } catch (FileNotFoundException ex) {
          Logger.WriteLineIf(LogLevel.Error, _log_props,
            string.Format("Exception thrown when getting piece torrent: {0}", ex));
          context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }
      } else {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
          "Request URL ({0}) doesn't match the service. Returning 404 response.", 
          context.Request.Url));
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
    }

    #region IPieceInfoServer Members

    public byte[] GetPieceTorrent(string nameSpace, string name, int pieceIndex) {
      _pieceTorrentManager.ServePiece(nameSpace, name, pieceIndex);
      return _pieceTorrentManager.ReadPieceTorrent(nameSpace, name, pieceIndex);
    }

    #endregion
  }
}
