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
      try {
        int key_index = 0;
        foreach (DhtGetResult dgr in dgrs) {
          FingerprintedData fpd =
              (FingerprintedData)DictionaryData.CreateDictionaryData(dgr.value);
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("Piece {0} retrieved and successfully parsed", 
              Encoding.UTF8.GetString(keys[key_index++])));
          RegularData rd = fpd.InnerData as RegularData;
          fragments = MemBlock.Concat(fragments, MemBlock.Reference(rd.PayLoad));
          if (smallest_ttl > dgr.ttl)
            smallest_ttl = dgr.ttl;
          if (largest_age < dgr.age)
            largest_age = dgr.age;
          //Now it's safe to say, this attempt succeeded.
        }
      } catch (Exception ex) {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
            ex);
        throw ex;
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

      bool[] result;
      try {
        result = _dht.BulkPut(parameters);
      } catch (Exception ex) {
        Console.WriteLine(ex);
        throw;
      }

      foreach (bool succ in result) {
        if (!succ) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
                    string.Format("Put failed."));
          // @TODO Could insert some retrying code here.
          return false;
        }
      }

      //Put info
      bool succ_info = false;
      int retries_info = 3;
      for (; !succ_info && retries_info > 0; retries_info--) {
        if (retries_info < 3) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("Retrying..."));
        }
        succ_info = _dht.Put(infoKey, fragInfo.SerializeTo(), ttl);
      }

      if (retries_info <= 0) {
        Logger.WriteLineIf(LogLevel.Error, _log_props,
            string.Format("Retries exhausted when putting FragmentationInfo : {0}",
            info_key_string));
        return false;
      } else {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("FragmentationInfo successfully put to DHT with key {0}",
            info_key_string));
      }
      return true;
    }
  }
}
