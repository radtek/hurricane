// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.BitTorrent {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using MonoTorrent.Client;
  using System.Diagnostics;
  using MonoTorrent.Common;
  using log4net;

  /// <summary>
  /// The class that manages the sliding window for streaming.
  /// </summary>
  public class StreamBitTorrentManager {
    static readonly ILog logger = LogManager.GetLogger(typeof(StreamBitTorrentManager));

    TorrentManager _torrentManager;
    SlidingWindowPicker _slidingWindowPicker;
    object _syncRoot = new object();

    public StreamBitTorrentManager(TorrentManager torrentManager) {
      _torrentManager = torrentManager;
      _slidingWindowPicker = CreateSlidingWindowPicker();
      _slidingWindowPicker.HighPrioritySetSize = 5;
      _torrentManager.ChangePicker(_slidingWindowPicker);
      _torrentManager.PieceHashed += new EventHandler<PieceHashedEventArgs>(_torrentManager_PieceHashed);
    }

    void _torrentManager_PieceHashed(object sender, PieceHashedEventArgs e) {
      TorrentManager manager = e.TorrentManager;
      bool hashPassed = e.HashPassed;
      int pieceIndex = e.PieceIndex;

      if (hashPassed) {
        if (manager.State != TorrentState.Downloading) {
          logger.Debug(string.Format(
            "Piece {0} hash passed but the torrent isn't in the Downloading state.",
            pieceIndex));
          return;
        } else {
          logger.Debug(string.Format("Piece {0} downloaded.", pieceIndex));
        }
      } else {
        // hash not passed. No need for further processing.
        return;
      }

      lock (_syncRoot) {
        // We need to know whether all pieces (in the window) prior to this one 
        // are already downloaded.
        int highPriorityEndIndex = _slidingWindowPicker.HighPrioritySetStart +
          _slidingWindowPicker.HighPrioritySetSize;
        int checkEndIndex = pieceIndex < highPriorityEndIndex ? pieceIndex :
          highPriorityEndIndex;

        var isEveryPreviousHighPriorityPieceDownloaded = true;
        for (int i = _slidingWindowPicker.HighPrioritySetStart; i < checkEndIndex;
          i++) {
          if (!_torrentManager.Bitfield[i]) {
            isEveryPreviousHighPriorityPieceDownloaded = false;
            break;
          }
        }

        if (isEveryPreviousHighPriorityPieceDownloaded) {
          _slidingWindowPicker.HighPrioritySetStart = pieceIndex + 1;
          logger.Debug(string.Format("Moved sliding window to [{0}, {1}]",
            _slidingWindowPicker.HighPrioritySetStart,
            _slidingWindowPicker.HighPrioritySetStart +
            _slidingWindowPicker.HighPrioritySetSize));
        } else {
          // The window doesn't move.
        }
      }
    }

    /// <summary>
    /// Creates the <see cref="SlidingWindowPicker"/>
    /// </summary>
    public SlidingWindowPicker CreateSlidingWindowPicker() {
      PiecePicker picker;
      if (ClientEngine.SupportsEndgameMode)
        picker = new EndGameSwitcher(new StandardPicker(), new EndGamePicker(), 
          _torrentManager.Torrent.PieceLength / Piece.BlockSize, _torrentManager);
      else
        picker = new StandardPicker();
      picker = new RandomisedPicker(picker);
      picker = new RarestFirstPicker(picker);
      return new SlidingWindowPicker(picker);
    }
  }
}
