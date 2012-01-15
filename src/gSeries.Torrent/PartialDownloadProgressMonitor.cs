// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTorrent.Client;
using MonoTorrent.Common;
using System.Diagnostics;

namespace GSeries.BitTorrent {
  /// <summary>
  /// This class monitors the completion of one or more files in the downloading 
  /// process and triggers an event when a file is downloaded.
  /// </summary>
  /// <remarks>
  /// Usually there are two adjacent files that have the piece if the piece 
  /// happens to be on the boundary of the two. There could be more, though, 
  /// if there are files smaller than a piece.
  /// </remarks>
  public class PartialDownloadProgressMonitor {
    public event EventHandler<FileDownloadedEventArgs> FileDownloaded;

    TorrentManager _torrentManager;
    TorrentFile[] _torrentFiles;

    public PartialDownloadProgressMonitor(TorrentManager torrentManager) {
      _torrentManager = torrentManager;
      _torrentFiles = torrentManager.Torrent.Files;
      _torrentManager.PieceHashed += new EventHandler<PieceHashedEventArgs>(
        _torrentManager_PieceHashed);
    }

    void _torrentManager_PieceHashed(object sender, PieceHashedEventArgs e) {
      TorrentManager manager = e.TorrentManager;
      bool hashPassed = e.HashPassed;
      int pieceIndex = e.PieceIndex;

      if (hashPassed) {
        if (manager.State != TorrentState.Downloading) {
          Debug.WriteLine(string.Format(
            "Piece {0} hash passed but the torrent isn't in the Downloading state.",
            pieceIndex));
          return;
        } else {
          Debug.WriteLine(string.Format("Piece {0} downloaded.", pieceIndex));
        }
      } else {
        // hash not passed. No need for further processing.
        return;
      }

      // Returns the collection of files that have the piece.
      IEnumerable<TorrentFile> files = _torrentFiles.Where<TorrentFile>(
        delegate(TorrentFile file) {
          if (file.EndPieceIndex < pieceIndex) {
            return false;
          } else if (file.StartPieceIndex > pieceIndex) {
            return false;
          } else {
            return true;
          }
      });

      foreach (TorrentFile file in files) {
        if (file.BitField.PercentComplete == 100) {
          RaiseFileDownloaded(new FileDownloadedEventArgs(_torrentManager, file));
        } else {
          // This file is not downloaded completely.
        }
      }
    }

    void RaiseFileDownloaded(FileDownloadedEventArgs args) {
      Toolbox.RaiseAsyncEvent<FileDownloadedEventArgs>(this.FileDownloaded, this, args);
    }

  }
}
