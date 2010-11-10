using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTorrent.Common;
using System.Threading;
using System.IO;
using MonoTorrent.BEncoding;
using System.Net;

namespace Fushare.Services.BitTorrent {
  public class PieceLevelTorrentManager {
    #region Fields
    BitTorrentManager _manager;
    DhtProxy _dhtProxy;
    TorrentHelper _torrentHelper;
    readonly int _pieceInfoServerPort;
    IPieceInfoServer _pieceInfoServer;
    BitTorrentCache _bittorrentCache;
    static readonly IDictionary _log_props = Logger.PrepareLoggerProperties(typeof(PieceLevelTorrentManager)); 
    #endregion

    public PieceLevelTorrentManager(BitTorrentManager manager, BitTorrentCache bittorrentCache,
      DhtProxy dhtProxy, TorrentHelper torrentHelper, int pieceInfoServerPort) {
      _manager = manager;
      _dhtProxy = dhtProxy;
      _bittorrentCache = bittorrentCache;
      _torrentHelper = torrentHelper;
      _pieceInfoServerPort = pieceInfoServerPort;
      _pieceInfoServer = new HttpPieceInfoServer(_pieceInfoServerPort, this);
    }

    public void Start() {
      _pieceInfoServer.Start();
    }

    /// <summary>
    /// Serves the torrent piece.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <exception cref="ArgumentException">The torrent is not a single file torrent.
    /// </exception>
    public void ServePiece(string nameSpace, string name, int pieceIndex) {
      string pieceName = MakePieceDataName(name, pieceIndex);
      if (IOUtil.FileOrDirectoryExists(_bittorrentCache.GetTorrentFilePath(
        nameSpace, pieceName))) {
        // It is already being served.
        return;
      }

      var torrent = Torrent.Load(_bittorrentCache.GetTorrentFilePath(nameSpace, 
        name));
      if (torrent.Files.Length != 1) {
        throw new ArgumentException(
          "This name specifies a multi-file torrent but only single file torrent is allowed.",
          "name");
      }

      var offset = pieceIndex * torrent.PieceLength;

      byte[] pieceData = IOUtil.Read(_bittorrentCache.GetPathOfItemInDownloads(
        nameSpace, name), offset, torrent.PieceLength, torrent.Size);

      var piecePath = _bittorrentCache.GetPathOfItemInDownloads(nameSpace, 
        pieceName);
      File.WriteAllBytes(piecePath, pieceData);
      Logger.WriteLineIf(LogLevel.Info, _log_props,
        string.Format("Start to serve the piece {0}.", pieceName));
      _manager.UpdateData(nameSpace, pieceName);
    }

    /// <summary>
    /// Reads the torrent for this piece.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <returns></returns>
    public byte[] ReadPieceTorrent(string nameSpace, string name, int pieceIndex) {
      var pieceName = MakePieceDataName(name, pieceIndex);
      return File.ReadAllBytes(_bittorrentCache.GetTorrentFilePath(nameSpace, 
        pieceName));
    }

    /// <summary>
    /// Gets a block of data.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="bytesToRead">The bytes to read.</param>
    /// <param name="waitHandle">The wait handle.</param>
    /// <returns></returns>
    public byte[] GetDataBlock(string nameSpace, string name, long offset, 
      int bytesToRead) {
      // Download whole torrent.
      var wholeTorrentBytes = _torrentHelper.ReadOrDownloadTorrent(
        nameSpace, name, _dhtProxy);
      Torrent wholeTorrent = Torrent.Load(wholeTorrentBytes);

      // Find out the piece to download.
      int pieceIndex = (int)Math.Floor(offset / (double)wholeTorrent.PieceLength);
      var pieceName = MakePieceDataName(name, pieceIndex);

      int offsetInPiece = (int)(offset % wholeTorrent.PieceLength);

      // If the piece is already downloaded, we just return it.
      string piecePath = _bittorrentCache.GetPathOfItemInDownloads(
        nameSpace, pieceName);
      if (IOUtil.FileOrDirectoryExists(piecePath)) {
        return IOUtil.Read(piecePath, offsetInPiece, bytesToRead);
      }

      var pieceTorrentBytes = DownloadPieceTorrent(nameSpace, name, 
        wholeTorrent, pieceIndex);

      // Write it to filesys so that the ordinary torrent downloading can pick it up.
      var pieceTorrentPath = 
        _torrentHelper.WriteTorrentFile(nameSpace, pieceName, pieceTorrentBytes);
      Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
        "Piece torrent downloaded and written to path {0}", pieceTorrentPath));
      
      // Download as a regular torrent.
      string pieceDownloadPath;
      _manager.GetData(nameSpace, pieceName, out pieceDownloadPath);
      return IOUtil.Read(pieceDownloadPath, offsetInPiece, bytesToRead);
    }

    /// <summary>
    /// Downloads the torrent of the piece and write it to 
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name.</param>
    /// <param name="wholeTorrent">The whole torrent.</param>
    /// <param name="pieceIndex">Index of the piece.</param>
    byte[] DownloadPieceTorrent(string nameSpace, string name, Torrent wholeTorrent, 
      int pieceIndex) {
      var wholeTorrentDhtKeyStr = ServiceUtil.GetDhtKeyString(nameSpace, name);
      var pieceKeyStr = MakePieceTorrentKey(wholeTorrentDhtKeyStr, pieceIndex);
      var torrentFilePath = _bittorrentCache.GetTorrentFilePath(nameSpace, name);
      
      byte[] pieceTorrentBytes;
      bool succ = _torrentHelper.TryReadOrDownloadTorrent(
        nameSpace, name, _dhtProxy, out pieceTorrentBytes);
      if (succ) {
        // Somebody else already requested this piece.
        return pieceTorrentBytes;
      } else {
        // We need to request the seeder to make a new piece.
        IEnumerable<PeerEntry> peers = _dhtProxy.GetPeers(wholeTorrent.InfoHash);
        bool foundCompletePeer = false;

        // The list most likely has only one entry.
        var trackerIPs = new List<string>();
        foreach(var tiers in wholeTorrent.AnnounceUrls) {
          foreach(var trackerUrl in tiers) {
            // Given that we don't use domain names for hosts.
            trackerIPs.Add(new Uri(trackerUrl).Host);
          }
        }

        // Completed -> Downloader has the whole data.
        // PeerIP == Tracker -> Original Seeder(s).
        var peerIPs = (from peer in peers
                       where (peer.PeerState == TorrentEvent.Completed ||
                         trackerIPs.Contains(peer.PeerIP))
                       select peer.PeerIP).Distinct();
        Logger.WriteLineIf(LogLevel.Verbose, _log_props, string.Format(
          "{0} peers to try for piece info.", peerIPs.Count()));

        foreach (var peerIP in peerIPs) {
          foundCompletePeer = true;
          // The first complete peer should be able to serve the piece we want.
          bool succPieceInfo = TryDownloadPieceTorrentFromPeer(nameSpace,
            name, pieceIndex, peerIP, _pieceInfoServerPort,
            out pieceTorrentBytes);
          if (succPieceInfo) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("Piece info downloaded from {0}", peerIP));
            return pieceTorrentBytes;
          } else {
            // Try the next completePeer
            continue;
          }
        }

        if (!foundCompletePeer) {
          throw new ResourceNotFoundException("No peer is complete.");
        } else if (pieceTorrentBytes == null) {
          throw new ResourceNotFoundException(
            "No complete peer(s) can return the piece Info");
        } else {
          // This shouldn't happen
          return pieceTorrentBytes;
        }
      }
    }

    private bool TryDownloadPieceTorrentFromPeer(string nameSpace, 
      string name, int pieceIndex, string peerIP, int peerPort, 
      out byte[] pieceTorrentBytes) {
      var requestUrl = string.Format("http://{0}:{1}/{2}/{3}/{4}/{5}",
        peerIP,
        peerPort,
        HttpPieceInfoServer.ControllerSegment,
        nameSpace,
        name,
        pieceIndex);

      try {
        var req = WebRequest.Create(requestUrl) as HttpWebRequest;
        var resp = req.GetResponse() as HttpWebResponse;
        if (resp.StatusCode == HttpStatusCode.OK) {
          using (var reader = new BinaryReader(resp.GetResponseStream())) {
            pieceTorrentBytes = reader.ReadBytes((int)resp.ContentLength);
            if (pieceTorrentBytes.Length == 0) {
              System.Environment.FailFast(
                "PieceInfoServer shoudn't return empty result with status OK.");
            }
          }
        } else {
          Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
            "Failed to get piece info from peer. StatusCode: {0}", resp.StatusCode));
          pieceTorrentBytes = null;
          return false;
        }
      } catch (WebException ex) {
        Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
          "Exception thrown when communicating with peer HttpPieceInfoServer at {0}: {1}",
          requestUrl, ex));
        pieceTorrentBytes = null;
        return false;
      }
      return true;
    }

    static string MakePieceDataName(string wholeName, int pieceIndex) {
      return string.Format("{0}.p{1}", wholeName, pieceIndex);
    }

    static string MakePieceTorrentKey(string wholeTorrentKey, int pieceIndex) {
      return string.Format("{0}:p{1}", wholeTorrentKey, pieceIndex);
    }

  }
}
