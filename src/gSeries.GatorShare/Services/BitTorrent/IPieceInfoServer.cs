using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTorrent.Common;

namespace GSeries.Services.BitTorrent {
  public interface IPieceInfoServer {
    byte[] GetPieceTorrent(string nameSpace, string name, int piece);
    void Start();
  }
}
