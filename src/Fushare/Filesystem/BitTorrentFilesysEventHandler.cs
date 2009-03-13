using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Fushare.Services.BitTorrent;
using System.IO;

namespace Fushare.Filesystem {
  public class BitTorrentFilesysEventHandler : FilesysEventHandlerBase {
    const string ServerControllerName = "BitTorrent";
    const string BasicTemplateString = "{handler}/{nameSpace}/{name}";

    public BitTorrentFilesysEventHandler(ServerProxy serverProxy) : 
      base(serverProxy) { }

    #region IFilesysEventHandler Members

    public override void HandleGettingPathStatus(IFushareFilesys sender, 
      GetPathStatusEventArgs args) {
      UriTemplateMatch match;
      var succ = TryMatchPath(BasicTemplateString, 
        args.VritualPath.PathString, out match);
      if (succ) {
        var reqUri = BasicPathMatch2ReqUri(match);
        var xmlString = _serverProxy.GetAsString(reqUri);
        CreateVirtualFiles(sender.ShadowDirPath, xmlString);
      }
    }

    private static Uri BasicPathMatch2ReqUri(UriTemplateMatch match) {
      // The path qualifies for this operation. Now get the real path.
      var nsStr = match.BoundVariables[1];
      var nameStr = match.BoundVariables[2];
      var reqUri = new Uri(string.Format("/{0}/{1}/{2}", ServerControllerName,
        nsStr, nameStr), UriKind.Relative);
      // Get the meta info for the path.
      return reqUri;
    }

    public static void CreateVirtualFiles(string localBaseDir, string xmlString) {
      var metaInfo = XmlUtil.FromXml<DataMetaInfo>(xmlString);
      // Create meta files
      foreach (var fileRelativeUri in metaInfo.Files) {
        var virtualFile = new VirtualFile();
        virtualFile.PhysicalUri = UriUtil.CombineUris(metaInfo.DataUri, fileRelativeUri);
        var virtualFilePath = UriUtil.CombinePaths(localBaseDir, fileRelativeUri);
        IOUtil.PrepareParentDiryForPath(virtualFilePath);
        XmlUtil.WriteXml<VirtualFile>(virtualFile, virtualFilePath);
      }
    }

    public override void HandleReleasedFile(IFushareFilesys sender, 
      ReleaseFileEventArgs args) {
      UriTemplateMatch match;
      var succ = TryMatchPath("{handler}/{name}", 
        args.VritualPath.PathString, out match);
      // Only files under /bt directory are supported.
      if (succ) {
        ShadowFullPath fullPath = new ShadowFullPath(sender.ShadowDirPath, 
          new VirtualPath(args.VritualPath));
        _serverProxy.Post(new Uri(string.Format("/{0}?path={1}", 
          ServerControllerName, fullPath.PathString), UriKind.Relative), null);
      }
    }

    public override void HandleReadingFile(IFushareFilesys sender, 
      ReadFileEventArgs args) {
      var fullPath = new ShadowFullPath(sender.ShadowDirPath,
        new VirtualPath(args.VritualPath));

    }

    #endregion
  }
}
