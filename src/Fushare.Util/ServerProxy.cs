/*
Copyright (C) 2009 Jiangyan Xu <jiangyan@ufl.edu>, University of Florida

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Collections;

namespace Fushare {
  /// <summary>
  /// Encapsulates the WebClient instance and provides common operations.
  /// </summary>
  public class ServerProxy {
    readonly string _baseAddress;
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(ServerProxy));

    internal string BaseAddress {
      get { return _baseAddress; }
    }

    public ServerProxy(string baseAddress) {
      if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute)) {
        throw new ArgumentException("Invalid address.", "baseAddress");
      }
      _baseAddress = baseAddress;
    }

    /// <summary>
    /// Downloads data from the specified URI through HTTP Get.
    /// </summary>
    /// <param name="relativeUri">The URI.</param>
    public byte[] Get(Uri relativeUri) {
      return DownloadData(new Uri(new Uri(_baseAddress), relativeUri), null);
    }

    public byte[] Get(Uri relativeUri, int timeout) {
      return DownloadData(new Uri(new Uri(_baseAddress), relativeUri), timeout);
    }

    public byte[] Get(string relativeUri) {
      return Get(new Uri(relativeUri, UriKind.Relative));
    }

    public string GetUTF8String(Uri uri, int timeout) {
      return Encoding.UTF8.GetString(Get(uri, timeout));
    }

    public string GetUTF8String(string uri) {
      return Encoding.UTF8.GetString(Get(uri));
    }

    /// <summary>
    /// Gets the with multiple attempts.
    /// </summary>
    /// <param name="relativeUri">The relative URI.</param>
    /// <param name="retries">The number of retries.</param>
    /// <remarks>Sometimes web services can fail due to load and other issues.
    /// </remarks>
    public byte[] GetWithRetries(string relativeUri, int retries) {
      for (; retries > 0; retries--) {
        try {
          return Get(relativeUri);
        } catch (WebException ex) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "Exception caught: {0}. Retrying...", ex));
        }
      }
      return Get(relativeUri);
    }

    /// <summary>
    /// Uploads data to the specified URI through HTTP POST.
    /// </summary>
    /// <param name="uri">The URI.</param>
    /// <param name="data">The data.</param>
    public byte[] Post(Uri uri, byte[] data) {
      using (var webClient = MakeWebClient()) {
        return webClient.UploadData(new Uri(new Uri(_baseAddress), uri), data);
      }
    }

    public byte[] Post(string uri, byte[] data) {
      using (var webClient = MakeWebClient()) {
        return webClient.UploadData(new Uri(new Uri(_baseAddress), uri), data);
      }
    }

    public byte[] Put(Uri uri, byte[] data) {
      using (var webClient = MakeWebClient()) {
        return webClient.UploadData(new Uri(new Uri(_baseAddress), uri), "PUT",
          data);
      }
    }

    public byte[] Put(string uri, byte[] data) {
      using (var webClient = MakeWebClient()) {
        return webClient.UploadData(new Uri(new Uri(_baseAddress), uri), "PUT",
          data);
      }
    }

    WebClient MakeWebClient() {
      var webClient = new WebClient();
      webClient.BaseAddress = _baseAddress;
      webClient.Headers[HttpRequestHeader.UserAgent] = "FushareClient";
      return webClient;
    }

    /// <summary>
    /// Downloads the data.
    /// </summary>
    /// <param name="uri">The URI.</param>
    /// <param name="timeout">The timeout. If null, use default timeout.</param>
    /// <returns></returns>
    byte[] DownloadData(Uri uri, int? timeout) {
      HttpWebRequest request = null;
      try {
        request = (HttpWebRequest)WebRequest.Create(uri);
        request.UserAgent = "FushareClient";
        if (timeout.HasValue) {
          request.Timeout = timeout.Value;
        }
        var response = request.GetResponse();
        Stream st = response.GetResponseStream();
        return ReadAll(st, (int)response.ContentLength);
      } catch (ThreadInterruptedException) {
        if (request != null)
          request.Abort();
        throw;
      } catch (WebException wexc) {
        throw;
      } catch (Exception ex) {
        throw new WebException("An error occurred while downloading data.", ex);
      }
    }

    byte[] ReadAll(Stream stream, int length) {
      MemoryStream ms = null;

      bool nolength = (length == -1);
      int size = ((nolength) ? 8192 : length);
      if (nolength)
        ms = new MemoryStream();

      //			long total = 0;
      int nread = 0;
      int offset = 0;
      byte[] buffer = new byte[size];
      while ((nread = stream.Read(buffer, offset, size)) != 0) {
        if (nolength) {
          ms.Write(buffer, 0, nread);
        } else {
          offset += nread;
          size -= nread;
        }
      }

      if (nolength)
        return ms.ToArray();

      return buffer;
    }
  }
}