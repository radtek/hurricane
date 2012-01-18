using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTorrent.Client;

namespace MonoTorrentExperiments {
  class CustomClientEngine : ClientEngine {
    public CustomClientEngine(EngineSettings settings) : base(settings) { }
  }
}
