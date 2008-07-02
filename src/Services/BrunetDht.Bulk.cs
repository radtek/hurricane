using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CookComputing.XmlRpc;
using Brunet;
using Brunet.DistributedServices;

namespace Fushare.Services {
  partial class BrunetDht {
    /// <summary>
    /// Does bulk Brunet DHT gets by calling the bulk DHT operation API of its 
    /// XML-RPC interface.
    /// </summary>
    /// <returns>Null if not all the pieces are successfully retrieved and parsed.</returns>
    /// <exception cref="Exception">Parsing errors.</exception>
    private MemBlock GetFragsInBulk(byte[] base_key, int piece_num,
      out int largest_age, out int smallest_ttl) {
      largest_age = 0;
      smallest_ttl = Int32.MaxValue;
      MemBlock fragments = new MemBlock();

      byte[][] keys = new byte[piece_num][];
      for (int i = 0; i < piece_num; i++) {
        byte[] piece_key = BuildFragmentKey(base_key, i);
        keys[i] = piece_key;
      }

      DhtGetResult[] dgrs = _dht.BulkGet(keys);
      for (int i = 0; i < dgrs.Length; i++) {
        DhtGetResult dgr = dgrs[i];
        if (dgr == null) {
          Logger.WriteLineIf(LogLevel.Error, _log_props,
            string.Format("Piece #{0} is null. Retrying...", i));
          dgr = GetWithRetries(keys[i], 2);
          if (dgr == null) {
            Logger.WriteLineIf(LogLevel.Error, _log_props,
              string.Format("Piece #{0} is null after retries. Skipping " +
              "further parsing and returning...", i));
            return null;
          }
        }
        FingerprintedData fpd;
        try {
          fpd =
            (FingerprintedData)DictionaryData.CreateDictionaryData(dgr.value);
        } catch (Exception ex) {
          Logger.WriteLineIf(LogLevel.Error, _log_props,
              ex);
          throw ex;
        }
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Piece #{0} retrieved and successfully parsed", i));
        RegularData rd = fpd.InnerData as RegularData;
        fragments = MemBlock.Concat(fragments, MemBlock.Reference(rd.PayLoad));
        if (smallest_ttl > dgr.ttl)
          smallest_ttl = dgr.ttl;
        if (largest_age < dgr.age)
          largest_age = dgr.age;
        //Now it's safe to say, this attempt succeeded.
      }

      return fragments;
    }

    private bool PutFragsInBulk(FragmentationInfo fragInfo,
      byte[] infoKey, int ttl, IList<FingerprintedData> fragments) {
      string info_key_string = Encoding.UTF8.GetString(infoKey);
      XmlRpcStruct[] parameters = new XmlRpcStruct[fragments.Count];
      // Prepare parameters for bulk put.
      int index = 0;
      foreach (FingerprintedData fpd in fragments) {
        byte[] serializedFpd = fpd.SerializeTo();
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Size of piece {0} after serialization: {1}", index, 
          serializedFpd.Length));
        byte[] piece_key = BuildFragmentKey(fragInfo.BaseKey, index);
        XmlRpcStruct param_dict = new XmlRpcStruct();
        param_dict.Add("key", piece_key);
        param_dict.Add("value", serializedFpd);
        param_dict.Add("ttl", ttl);
        parameters[index] = param_dict;
        index++;
      }

      bool[] results;
      try {
        results = _dht.BulkPut(parameters);
      } catch (Exception ex) {
        // Network related operation, log the exception and let it be handled
        // upper stream caller.
        Logger.WriteLineIf(LogLevel.Error, _log_props,
          string.Format("Exception caught in BulkPut"));
        Logger.WriteLineIf(LogLevel.Error, _log_props,
          ex);
        throw;
      }

      IList<int> failed_pieces = new List<int>();
      for (int i = 0; i < results.Length; i++) {
        bool result = results[i];
        if (!result) {
          failed_pieces.Add(i);
        }
      }

      // Log failed pieces
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        string.Format("Failed {0} pieces are:", failed_pieces.Count));
      StringBuilder sb = new StringBuilder();
      foreach (int pieceIndx in failed_pieces) {
        sb.Append(pieceIndx);
        sb.Append(" ");
      }
      Logger.WriteLineIf(LogLevel.Verbose, _log_props,
        sb.ToString());

      // Decide whether to retry.
      if (failed_pieces.Count > 20) {
        Logger.WriteLineIf(LogLevel.Info, _log_props,
          string.Format("Too many pieces failed. Returning..."));
        return false;
      }

      //Retry failed pieces.
      foreach (int pieceIndx in failed_pieces) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Retrying piece {0}", index));
        bool succ_retries = PutWithRetries((byte[])parameters[index]["key"], (byte[])
          parameters[index]["value"], (int)parameters[index]["ttl"], 2);
        if (!succ_retries) {
          // Cannot succeed on this piece even after retry...
          return false;
        }
      }

      //Put info
      bool succ_info = PutWithRetries(infoKey, fragInfo.SerializeTo(), ttl, 2);
      return succ_info;
    }

    /// <returns>Null if nothing got after retries</returns>
    private DhtGetResult GetWithRetries(byte[] key, int retries) {
      bool succ = false;
      DhtGetResult ret = null;
      string key_string = Encoding.UTF8.GetString(key);
      int retires_local = retries;
      for (; !succ && retires_local > 0; retires_local--) {
        if (retires_local < retries) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Retrying..."));
        }
        DhtGetResult[] results = _dht.Get(key);
        if (results.Length > 0) {
          succ = true;
          ret = results[0];
        }
      }

      if (retires_local <= 0) {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
          string.Format("Retries exhausted when getting {0}",
          key_string));
      } else {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("{0} successfully got.",
          key_string));
      }
      return ret;
    }

    /// <param name="retries">Should be >= 0. Zero means no retry.</param>
    private bool PutWithRetries(byte[] key, byte[] value, int ttl, int retries) {
      bool succ = false;
      string key_string = Encoding.UTF8.GetString(key);
      int retires_local = retries;
      for (; !succ && retires_local > 0; retires_local--) {
        if (retires_local < retries) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Retrying..."));
        }
        succ = _dht.Put(key, value, ttl);
      }

      if (retires_local <= 0) {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
          string.Format("Retries exhausted when putting {0}",
          key_string));
        return false;
      } else {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("{0} successfully put.",
          key_string));
        return true;
      }
    }
  }
}
