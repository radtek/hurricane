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

namespace GSeries.BitTorrent {
  /// <summary>
  /// Represents an event fired when a file in the torrent is downloaded.
  /// </summary>
  public class FileDownloadedEventArgs : TorrentEventArgs {
    public TorrentFile TorrentFile {
      get;
      private set;
    }

    public FileDownloadedEventArgs(TorrentManager torrentManager, 
      TorrentFile torrentFile)
      : base(torrentManager) {
        TorrentFile = torrentFile;
    }
  }
}
