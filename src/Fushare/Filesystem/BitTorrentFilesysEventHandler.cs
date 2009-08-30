using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Fushare.Services.BitTorrent;
using System.IO;
using System.Collections;

namespace Fushare.Filesystem {
  public class BitTorrentFilesysEventHandler : FilesysEventHandlerBase {
    #region Fields
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(BitTorrentFilesysEventHandler));
    const string ServerControllerName = "BitTorrent";
    const string BasicTemplateString = "{handler}/{nameSpace}/{name}"; 
    #endregion

    #region Constructors
    public BitTorrentFilesysEventHandler(ServerProxy serverProxy, 
      FushareFileManager fileManager, FusharePathFactory pathFactory) :
      base(serverProxy, fileManager, pathFactory) { } 
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
        }
        
        // else
        var reqUri = BasicPathMatch2ReqUri(match);
        Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
          "Requesting meta info from Uri {0}", reqUri.ToString()));
        string xmlString;
        try {
          xmlString = _serverProxy.GetUTF8String(reqUri);
        } catch (WebException ex) {
          Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
            "Exception thrown from server: {0}", ex));
          // Handle different types of error accodingly.
          var innerEx = ex.InnerException;
          if (innerEx != null && innerEx is WebException) {
            if ((ex.Response as HttpWebResponse).StatusCode ==
              HttpStatusCode.NotFound) {
              // Normal. Server says requested file/directory doesn't exist.
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

        CreateVirtualFiles(new ShadowMetaFullPath(sender.ShadowDirPath, 
          new VirtualPath(args.VritualRawPath)), xmlString);
      }
    }

    public override void HandleReleasedFile(IFushareFilesys sender, 
      ReleaseFileEventArgs args) {
      UriTemplateMatch match;
      var succ = TryMatchPath("{handler}/{name}", 
        args.VritualRawPath.PathString, out match);
      // Only files under /bt directory are supported.
      if (succ) {
        ShadowFullPath fullPath = new ShadowFullPath(sender.ShadowDirPath, 
          new VirtualPath(args.VritualRawPath));
        _serverProxy.Post(new Uri(string.Format("/{0}?path={1}", 
          ServerControllerName, fullPath.PathString), UriKind.Relative), null);
      }
    }

    public override void HandleReadingFile(IFushareFilesys sender, 
      ReadFileEventArgs args) {
      // A read here should be preceded by a path status inquiry.
      args.BytesRead = 
        _fileManager.Read(new VirtualPath(args.VritualRawPath), args.Buffer, 
        args.Offset);
    }

    #endregion

    /// <summary>
    /// Creates the virtual files for the metadata in a XML string form.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="xmlString">The XML string.</param>
    public static void CreateVirtualFiles(ShadowMetaFullPath basePath, string xmlString) {
      var metaInfo = XmlUtil.FromXml<DataMetaInfo>(xmlString);

      if (metaInfo.IsSingleFile) {
        var virtualFile = new VirtualFile() {
          PhysicalUri = metaInfo.DataUri
        };
        var virtualFilePath = basePath.PathString;
        Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "Creating virtual file at {0}", virtualFilePath));
        IOUtil.PrepareParentDiryForPath(virtualFilePath);
        XmlUtil.WriteXml<VirtualFile>(virtualFile, virtualFilePath);
      } else {
        // Create meta files
        foreach (var fileRelativeUri in metaInfo.Files) {
          var virtualFile = new VirtualFile() {
            PhysicalUri = UriUtil.CombineUris(metaInfo.DataUri, fileRelativeUri)
          };
          var virtualFilePath = UriUtil.CombinePaths(basePath.PathString, fileRelativeUri);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
            "Creating virtual file at {0}", virtualFilePath));
          IOUtil.PrepareParentDiryForPath(virtualFilePath);
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
    /// <returns></returns>
    private static Uri BasicPathMatch2ReqUri(UriTemplateMatch match) {
      // The path qualifies for this operation. Now get the real path.
      var nsStr = match.BoundVariables[1];
      var nameStr = match.BoundVariables[2];
      // BitTorrent/nameSpace/name
      var reqUri = new Uri(string.Format("/{0}/{1}/{2}", ServerControllerName,
        nsStr, nameStr), UriKind.Relative);
      // Get the meta info for the path.
      return reqUri;
    }
    #endregion
  }
}
