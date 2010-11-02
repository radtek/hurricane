using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Fushare.Services.BitTorrent;
using System.IO;
using System.Collections;
using MonoTorrent.Common;
using System.Collections.Specialized;

namespace Fushare.Filesystem {
  public class BitTorrentFilesysEventHandler : FilesysEventHandlerBase {
    #region Fields
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(BitTorrentFilesysEventHandler));
    const string ServerControllerName = "BitTorrent";
    const string BasicTemplateString = "{handler}/{nameSpace}/{name}";
    const string BytesToReadParamName = "bytesToRead";
    const string OffsetParamName = "offset";
    const string PeekParamName = "peek";
    const string OnDemandParamName = "od";
    #endregion

    #region Constructors
    public BitTorrentFilesysEventHandler(ServerProxy serverProxy,
      FushareFileManager fileManager, FusharePathFactory pathFactory, 
      FilesysContext filesysContext) :
      base(serverProxy, fileManager, pathFactory, filesysContext) { }
    #endregion

    #region IFilesysEventHandler Members

    public override void HandleGettingPathStatus(IFushareFilesys sender,
      GetPathStatusEventArgs args) {
      // First decide the path conforms to the pattern, then check its existence.
      UriTemplateMatch match;
      var succ = TryMatchPath(BasicTemplateString,
        args.VritualRawPath.PathString, out match);
      if (succ) {
        // Does it already exist?
        var shadowFullPath = _pathFactory.CreateShadowFullPath4Read(
          new VirtualPath(args.VritualRawPath));
        if (IOUtil.FileOrDirectoryExists(shadowFullPath.PathString)) {
          // @TODO return for now but the path could be stale.
          return;
        } else {
          String nameStr = match.BoundVariables[2];
          if (nameStr.StartsWith(".")) {
            // We don't deal with hidden files.
            return;
          }
        }

        // The path doesn't exsit locally. Check the server.
        Uri reqUri;
        reqUri = BasicPathMatch2ReqUri(match, new NameValueCollection() {
          { PeekParamName, "true" }
          });
        Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
          "Requesting meta info from Uri {0}", reqUri.ToString()));
        string xmlString;
        try {
          // This can be a while.
          xmlString = _serverProxy.GetUTF8String(reqUri,
            System.Threading.Timeout.Infinite);
        } catch (WebException ex) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "Exception thrown from server: {0}", ex));
          // Handle different types of error accodingly.
          if (ex is WebException) {
            if (((HttpWebResponse)((ex as WebException).Response)).StatusCode ==
              HttpStatusCode.NotFound) {
              // Normal. 
              Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
                "Server says {0} doesn't exist online.", args.VritualRawPath));
              return;
            } else {
              // Other protocol level errors.
              throw;
            }
          } else {
            // Non-procotol-level errors. 
            throw;
          }
        }

        // If everything is fine so far, we are good for subsequent read!
        CreateVirtualFiles(new ShadowMetaFullPath(sender.ShadowDirPath,
          new VirtualPath(args.VritualRawPath)), xmlString);
      }
    }

    /// <summary>
    /// Handles the opening file event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The <see cref="Fushare.Filesystem.OpenFileEventArgs"/> instance containing the event data.</param>
    public override void HandleOpeningFile(IFushareFilesys sender, OpenFileEventArgs args) {
      UriTemplateMatch match;
      var succ = TryMatchPath(BasicTemplateString,
        args.VritualRawPath.PathString, out match);
      if (succ && args.FileAccess == FileAccess.Read) {
        Uri reqUri;
        if (!string.IsNullOrEmpty(match.QueryParameters[OnDemandParamName])) {
          // It's on-demand read, downloading is postponed to read().
        } else {
          // Otherwise, we download the file now.
          reqUri = BasicPathMatch2ReqUri(match, null);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "Requesting meta info from Uri {0}", reqUri.ToString()));
          string xmlString;
          try {
            // This can be a while.
            xmlString = _serverProxy.GetUTF8String(reqUri,
              System.Threading.Timeout.Infinite);
          } catch (WebException ex) {
            Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
              "Exception thrown from server: {0}", ex));
            // Handle different types of error accodingly.
            if (ex is WebException) {
              if (((HttpWebResponse)((ex as WebException).Response)).StatusCode ==
                HttpStatusCode.NotFound) {
                // Normal. 
                Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                  string.Format("Server says requested file/directory doesn't exist."));
                return;
              } else {
                // Other protocol level errors.
                throw;
              }
            } else {
              // Non-procotol-level errors. 
              throw;
            }
          }
        }
      }
    }

    public override void HandleReleasedFile(IFushareFilesys sender,
      ReleaseFileEventArgs args) {
      UriTemplateMatch match;
      var succ = TryMatchPath(BasicTemplateString,
        args.VritualRawPath.PathString, out match);
      // Only files under /bt directory are supported.
      if (succ) {
        IntPtr handle = args.Handle;
        OpenFileInfo openFileInfo;
        if (_filesysContext.TryGetOpenFileInfo(handle, out openFileInfo) && 
          openFileInfo.FileAccess != FileAccess.Read) {
          // Stage in the file for publishing.
          _fileManager.CopyToServer(new VirtualPath(args.VritualRawPath));
          var uri = BasicPathMatch2ReqUri(match, null);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "POSTing to server to publish the file: {0}", uri));
          _serverProxy.Post(uri, new byte[] { });
        }
      }
    }

    public override void HandleReadingFile(IFushareFilesys sender,
      ReadFileEventArgs args) {
      // A read here should be preceded by a path status inquiry.
      OpenFileInfo ofi;
      VirtualFile vf;
      if (_filesysContext.TryGetOpenFileInfo(args.Handle, out ofi)) {
        vf = ofi.VirtualFile;
      } else {
        throw new InvalidOperationException();
      }

      if (vf.OnDemand) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Virtual file suggests on demand retrieval, requesting data..."));
        UriTemplateMatch match;
        var succ = TryMatchPath(BasicTemplateString,
          args.VritualRawPath.PathString, out match);
        if (succ) {
          var extraParams = new NameValueCollection();
          extraParams.Add(BytesToReadParamName, Convert.ToString(args.Buffer.Length));
          extraParams.Add(OffsetParamName, Convert.ToString(args.Offset));
          var uri = BasicPathMatch2ReqUri(match, extraParams);
          byte[] data = _serverProxy.Get(uri);
          // Size of data should be less than or equal to size of args.Buffer
          using (var ms = new MemoryStream(args.Buffer)) {
            ms.Write(data, 0, data.Length);
          }
          args.BytesRead = data.Length;
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Successfully read {0} bytes.", args.BytesRead));
        }
      } else {
        args.BytesRead =
          _fileManager.Read(vf, args.Buffer, args.Offset);
      }
    }

    #endregion

    /// <summary>
    /// Creates the virtual files for the metadata in a XML string form.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="xmlString">The XML string.</param>
    public static void CreateVirtualFiles(ShadowMetaFullPath basePath, string xmlString) {
      var metaInfo = XmlUtil.FromXml<DataMetaInfo>(xmlString);
      var torrent = Torrent.Load(metaInfo.TorrentBytes);
      if (metaInfo.IsSingleFile) {
        var virtualFile = new VirtualFile() {
          PhysicalUri = metaInfo.DataUri,
          FileSize = torrent.Files[0].Length,
          OnDemand = metaInfo.OnDemand
        };
        var virtualFilePath = basePath.PathString;
        Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "Creating virtual file at {0}", virtualFilePath));
        IOUtil.PrepareParentDirForPath(virtualFilePath);
        XmlUtil.WriteXml<VirtualFile>(virtualFile, virtualFilePath);
      } else {
        // Create a virtual file for each TorrentFile. 
        foreach (TorrentFile file in torrent.Files) {
          var pathUri = new Uri(file.Path, UriKind.Relative);
          var virtualFile = new VirtualFile() {
            PhysicalUri = UriUtil.CombineUris(metaInfo.DataUri, pathUri),
            FileSize = file.Length,
            OnDemand = metaInfo.OnDemand
          };
          var virtualFilePath = UriUtil.CombinePaths(basePath.PathString, pathUri);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "Creating virtual file at {0}", virtualFilePath));
          IOUtil.PrepareParentDirForPath(virtualFilePath);
          XmlUtil.WriteXml<VirtualFile>(virtualFile, virtualFilePath);
        }
      }
    }

    #region Private Methods
    /// <summary>
    /// Convert a matched basic path template (/Controller/Namespace/Name) to 
    /// request Uri
    /// </summary>
    /// <param name="match">The match.</param>
    /// <param name="extraParams">Extra parameters as query parameters.</param>
    /// <returns></returns>
    private static Uri BasicPathMatch2ReqUri(UriTemplateMatch match,
      NameValueCollection extraParams) {
      // The path qualifies for this operation. Now get the real path.
      var nsStr = match.BoundVariables[1];
      var nameStr = match.BoundVariables[2];
      match.QueryParameters.Remove(OnDemandParamName);

      string queryString = "";
      if (match.QueryParameters.Count > 0) {
        queryString = "?" + UriUtil.JoinNvcToQs(match.QueryParameters);
      }

      if (extraParams != null && extraParams.Count > 0) {
        string qs2 = UriUtil.JoinNvcToQs(extraParams);
        queryString += (string.IsNullOrEmpty(queryString) ? "?" + qs2 : "&" + qs2);
      }

      // BitTorrent/nameSpace/name
      var reqUri = new Uri(string.Format("/{0}/{1}/{2}", ServerControllerName,
        nsStr, nameStr) + queryString, UriKind.Relative);
      // Get the meta info for the path.
      return reqUri;
    }
    #endregion
  }
}
