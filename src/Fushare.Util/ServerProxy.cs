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

namespace Fushare {
  /// <summary>
  /// Encapsulates the WebClient instance and provides common operations.
  /// </summary>
  public class ServerProxy {
    readonly string _baseAddress;

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
    /// <param name="uri">The URI.</param>
    public byte[] Get(Uri uri) {
      using (var webClient = MakeWebClient()) {
        // Mono has problem with BaseAddress usage so we make absolute address 
        // by ourselves.
        return webClient.DownloadData(new Uri(new Uri(_baseAddress), uri));
      }
    }

    public byte[] Get(string uri) {
      using (var webClient = MakeWebClient()) {
        // Mono has problem with BaseAddress usage so we make absolute address 
        // by ourselves.
        return webClient.DownloadData(new Uri(new Uri(_baseAddress), uri));
      }
    }

    public string GetUTF8String(Uri uri) {
      return Encoding.UTF8.GetString(Get(uri));
    }

    public string GetUTF8String(string uri) {
      return Encoding.UTF8.GetString(Get(uri));
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
  }
}