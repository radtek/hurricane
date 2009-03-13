using System;
using System.Collections;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Threading;
using Mono.Unix;

using Fushare.Filesystem;
using MonoTorrent.Common;
using Brunet.Rpc;

namespace Fushare.Services.BitTorrent {
  /// <summary>
  /// Handles path using BitTorrent service.
  /// </summary>
  public class BitTorrentPathHandler : IPathHandler {
    private BitTorrentManager _manager;
    private static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(BitTorrentPathHandler));

    public BitTorrentPathHandler(string btBaseDir, int clientPort, int trackerPort) {
      string tracker_ip;
      IXmlRpcManager rpc = XmlRpcManagerClient.GetXmlRpcManager(10000);
      object rs = rpc.localproxy("Information.Info");
      IDictionary dic = (IDictionary)rs;
      tracker_ip = (string)dic["Virtual IP"];

      _manager = new BitTorrentManager(
        btBaseDir, clientPort,
        string.Format("http://{0}:{1}/", tracker_ip.ToString(), trackerPort));
      // Start listening threads.
      _manager.Start();
    }
    
    #region IPathHandler Members

    public void ProcessRequest(FuseContext context) {
      FuseRawPath fuse_raw_path = context.Request.FuseRawPath;
      FileSystemInfo shadow_full_info;
      NameValueCollection path_params;
      ShadowFullPath shadow_full_path =
        PathUtil.Instance.ParseFuseRawPath(
        fuse_raw_path, out shadow_full_info, out path_params);
      switch (context.Request.FuseMethod) {
        case FuseMethod.Read:
          // The filename should be like: {long Base32 string}.bt
          // And it's not in the shadow FS yet.
          string base32_dhtkey = Path.ChangeExtension(new FileInfo(
              shadow_full_path.PathString).Name, null);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Dhtkey in Base32: {0}", base32_dhtkey));
          byte[] torrent_dht_key = Brunet.Base32.Decode(base32_dhtkey);
          ManualResetEvent waitHandle = new ManualResetEvent(false);
          _manager.GetData(
            torrent_dht_key, "", _manager.DownloadsDirPath, waitHandle);
          // wait until downloading finishes
          waitHandle.WaitOne();
          if (Fushare.Environment.OSVersion == OS.Unix) {
            UnixSymbolicLinkInfo unique_to_downloads = 
              new UnixSymbolicLinkInfo(shadow_full_path.PathString);
            //unique_to_downloads.CreateSymbolicLinkTo(
            //  Path.Combine(_manager.DownloadsDirPath, torrent.Name));
          }
          break;
        case FuseMethod.Write:
          // In the current logic, we assume that real files all reside in 
          // Downloads folder.
          // Remove .bt suffix, which is regarded as the BitTorrent service 
          // indicator.
          string dest_shadow_full =
            Path.Combine(_manager.DownloadsDirPath,
            Path.ChangeExtension(shadow_full_info.Name, null));
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Src path: {0}, Dest path: {1}", 
            shadow_full_info.FullName, dest_shadow_full));
          if (!File.Exists(dest_shadow_full) && 
            !Directory.Exists(dest_shadow_full)) {
            // This move applies to file and directory
            Directory.Move(shadow_full_info.FullName, dest_shadow_full); 
          }
          
          byte[] dht_key = _manager.ServeFile(dest_shadow_full);
          
          // Link from the source to the dest.
          if (Fushare.Environment.OSVersion == OS.Unix) {
            string unique_name = Brunet.Base32.Encode(dht_key) + ".bt";
            string unique_full = Path.Combine(Directory.GetParent(
              shadow_full_info.FullName).FullName, unique_name);
            UnixSymbolicLinkInfo unique_to_downloads = 
              new UnixSymbolicLinkInfo(unique_full);
            unique_to_downloads.CreateSymbolicLinkTo(dest_shadow_full);
            UnixSymbolicLinkInfo orginal_to_unique = 
              new UnixSymbolicLinkInfo(shadow_full_info.FullName);
            orginal_to_unique.CreateSymbolicLinkTo(unique_full);
          }

          break;
      }
    }

    public bool IsReusable {
      get {
        return true;
      }
    }

    #endregion
  }
}
