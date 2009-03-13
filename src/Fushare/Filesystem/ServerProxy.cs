using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Fushare.Filesystem {
  /// <summary>
  /// Encapsulates the WebClient instance and provides common operations.
  /// </summary>
  public class ServerProxy {
    internal string BaseAddress { get; private set; }

    public ServerProxy(string baseAddress) {
      BaseAddress = baseAddress;
    }

    public byte[] Get(Uri uri) {
      using (var webClient = MakeWebClient()) {
        return webClient.DownloadData(uri);
      }
    }

    public string GetAsString(Uri uri) {
      return Encoding.UTF8.GetString(Get(uri));
    }

    public string GetUTF8String(Uri uri) {
      return Encoding.UTF8.GetString(Get(uri));
    }

    public byte[] Post(Uri uri, byte[] data) {
      using (var webClient = MakeWebClient()) {
        return webClient.DownloadData(uri);
      }
    }

    WebClient MakeWebClient() {
      var webClient = new WebClient();
      webClient.BaseAddress = BaseAddress;
      return webClient;
    }
  }
}