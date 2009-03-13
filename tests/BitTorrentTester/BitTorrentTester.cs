using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;

using Brunet;
using Fushare.Configuration;

namespace Fushare.Services.BitTorrent {
  class BitTorrentTester {
    static void Main(string[] args) {
      Fushare.Logger.LoadConfig("l4n.trackerapp.config");
      FushareConfigHandler.Read("fushare.config");

      string filepath = args[0];

      Console.WriteLine(args[0]);

      string action = args[1];

      string hostName = Dns.GetHostName();
      IPHostEntry entry = Dns.GetHostEntry(hostName);
      IPAddress[] list = entry.AddressList;
      IPAddress chosen = null;
      foreach (IPAddress addr in list) {
        if (addr.ToString().StartsWith("10")) {
          chosen = addr;
          break;
        }
      }

      if (chosen == null) {
        throw new Exception("No suitable IP.");
      }

      BitTorrentManager manager = new BitTorrentManager(
        System.Environment.CurrentDirectory, 24133, 
        string.Format("http://{0}:24132/", chosen.ToString()));
      manager.Start();

      if (action.Equals("share")) {
        manager.ServeFile(filepath);
      } else {
        string key_base32 = args[2];
        byte[] dht_key = Base32.Decode(key_base32);
        manager.GetData(dht_key, "", filepath, null);
      }

      // Why the program doesn't work correctly when this line was added?
      Console.Read();
    }
  }
}
