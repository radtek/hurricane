// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DataDistributionService {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Net;
    using ProtoBuf;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using log4net;
    using System.Reflection;
    using System.Security.Cryptography;

    /// <summary>
    /// A Wcf web service client implemented using WebClient.
    /// </summary>
    public class ManualFileServiceClient : IFileService {
        static readonly ILog logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);
        string _baseUri;

        public ManualFileServiceClient(string baseUri) {
            _baseUri = baseUri.TrimEnd('/');
        }

        class CustomWebClient : WebClient {
            int _timeout;
            public CustomWebClient() {
                _timeout = 300000;
            }

            protected override WebRequest GetWebRequest(Uri address) {
                var result = base.GetWebRequest(address);
                result.Timeout = this._timeout;
                return result;
            }
        }

        public WebClient MakeWebClient() {
            return new CustomWebClient();
        }

        public PathStatusDto GetPathStatus(string path) {
            using (var wc = MakeWebClient()) {
                try {
                    byte[] data = wc.DownloadData(string.Format("{0}/PathStatus/{1}", _baseUri, path));
                    using (var stream = new MemoryStream(data)) {
                        var ser = new DataContractJsonSerializer(typeof(PathStatusDto));
                        var obj = ser.ReadObject(stream);
                        return obj as PathStatusDto;
                    }
                } catch (WebException ex) {
                    if (ex.Status == WebExceptionStatus.ProtocolError &&
                        ex.Response is HttpWebResponse &&
                        (ex.Response as HttpWebResponse).StatusCode ==
                        HttpStatusCode.NotFound) {
                        throw new FileNotFoundInServiceException("File not found.", ex);
                    } else {
                        throw;
                    }
                }
            }
        }

        public byte[] Read(string path, string offset, string count) {
            using (var wc = MakeWebClient()) {
                try {
                    byte[] data = wc.DownloadData(string.Format("{0}/File/{1}/{2}/{3}", _baseUri, path, offset, count));
                    using (var stream = new MemoryStream(data)) {
                        var ser = new DataContractJsonSerializer(typeof(byte[]));
                        var obj = ser.ReadObject(stream);
                        var content = obj as byte[];

                        return content;
                    }
                } catch (WebException ex) {
                    if (ex.Status == WebExceptionStatus.ProtocolError &&
                        ex.Response is HttpWebResponse &&
                        (ex.Response as HttpWebResponse).StatusCode ==
                        HttpStatusCode.NotFound) {
                        throw new FileNotFoundInServiceException("File not found.", ex);
                    } else {
                        throw;
                    }
                }
            }
        }

        public byte[] Read(string path, long offset, int count) {
            return Read(path, offset.ToString(), count.ToString());
        }

        public string Echo(string message) {
            using (var wc = MakeWebClient()) {
                try {
                    byte[] data = wc.DownloadData(string.Format("{0}/Echo/{1}", _baseUri, message));
                    using (var stream = new MemoryStream(data)) {
                        var ser = new DataContractJsonSerializer(typeof(string));
                        var obj = ser.ReadObject(stream);
                        return obj as string;
                    }
                } catch (WebException ex) {
                    throw;
                }
            }
        }

        public void Error() {
            using (var wc = MakeWebClient()) {
                wc.DownloadData(string.Format("{0}/Error", _baseUri));
            }
        }
    }
}
