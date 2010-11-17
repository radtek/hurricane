using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Brunet;
using Brunet.Rpc;
using Brunet.DistributedServices;

namespace GatorShare.External.DictionaryService {
  /// <summary>
  /// The asynchronous methods and inner classes in BrunetDht
  /// </summary>
  /// <remarks>
  /// The asynchronous implementation is supposed to concurrently access DHT, 
  /// but not intended to be accessed by multiple threads. (not thread-safe)
  /// </remarks>
  partial class BrunetDht {
    public const int Concurrency = 5;

    #region DHT operation state classes
    /// <summary>
    /// The global state shared by all DHT operations of a single logic operation on a 
    /// large dataset
    /// </summary>
    abstract class AsyncFragsOpGlobalState {
      /// <summary>
      /// An object that can be used to synchronize access to the global state object.
      /// </summary>
      public readonly object SyncRoot = new object();
      public readonly byte[] BaseKey;
      public AutoResetEvent BatchFinishedEvent = new AutoResetEvent(false);
      /// <summary>
      /// The level of concurrency in puts. A value less than or equal than 1
      /// means max concurrency
      /// </summary>
      public int Concurrency = -1;
      /// <summary>
      /// Pieces number.
      /// </summary>
      public readonly int ExpectedPositiveReturnNum;
      /// <summary>
      /// Channel that contains the returned values.
      /// </summary>
      public Channel Returns;

      /// <summary>
      /// The number of returned succesful results.
      /// </summary>
      public int OpSuccCount = 0;

      public AsyncFragsOpGlobalState(int expectedPositiveReturnNum, byte[] baseKey,
        Channel returns) {
        this.Returns = returns;
        this.ExpectedPositiveReturnNum = expectedPositiveReturnNum;
        this.BaseKey = baseKey;
      }
    }

    class AsyncPutFragsGlobalState : AsyncFragsOpGlobalState {
      public AsyncPutFragsGlobalState(int expectedPositiveReturnNum, byte[] baseKey,
        Channel returns)
        : base(expectedPositiveReturnNum, baseKey, returns) {
      }
    }

    class AsyncGetFragsGlobalState : AsyncFragsOpGlobalState {
      /// <summary>
      /// List of fragments in their raw (DhtGetResult) form
      /// </summary>
      public DhtGetResult[] Fragments;
      /*
       * Min TTL and Max Age? Not in here.
       */

      public AsyncGetFragsGlobalState(int expectedPositiveReturnNum, byte[] baseKey,
        Channel returns)
        : base(expectedPositiveReturnNum, baseKey, returns) {
        this.Fragments = new DhtGetResult[expectedPositiveReturnNum];
      }
    }

    /// <summary>
    /// Represents the state of a single DHT operation.
    /// </summary>
    class AsyncOpState {
      public byte[] Key;
      public byte[] Value;
      public int Ttl;

      /// <summary>
      /// Use this when Get.
      /// </summary>
      public AsyncOpState(byte[] key) {
        this.Key = key;
      }

      /// <summary>
      /// Use this when Put.
      /// </summary>
      public AsyncOpState(byte[] key, byte[] value, int ttl) {
        this.Key = key;
        this.Value = value;
        this.Ttl = ttl;
      }
    }

    /// <summary>
    /// Contains the global state for all the fragments in an operation of a large
    /// datum and the state for the single piece
    /// </summary>
    /// <remarks>Used by all the DHT operations.</remarks>
    class AsyncFragsOpState {
      public AsyncFragsOpGlobalState GlobalState;
      public AsyncOpState PieceState;

      /// <param name="global">Get or Put global state class</param>
      public AsyncFragsOpState(AsyncFragsOpGlobalState global,
        AsyncOpState piece) {
        this.GlobalState = global;
        this.PieceState = piece;
      }
    }
    #endregion

    #region EventArgs, not really
    /// <remarks>
    /// This was originally design to be a EventArgs but now it is not
    /// </remarks>
    class FragOpStoppedEventArgs {
      /// <summary>
      /// Should be set to empty string if put succeeded.
      /// </summary>
      public readonly AsyncOpState FailedPiece;

      public bool IsSuccessful {
        get {
          if (FailedPiece == null) {
            return true;
          } else {
            return false;
          }
        }
      }

      /// <param name="failedPiece">
      /// Use null as the the failed piece if successful
      /// </param>
      public FragOpStoppedEventArgs(AsyncOpState failedPiece) {
        this.FailedPiece = failedPiece;
      }
    }

    /// <summary>
    /// The arguments of the the event fired by OnDhtPutResturns
    /// </summary>
    class PutFragsStoppedEventArgs : FragOpStoppedEventArgs {
      public PutFragsStoppedEventArgs(AsyncOpState failedPiece)
        : base(failedPiece) {
      }
    }

    /// <summary>
    /// The arguments of the event fired by OnDhtGetReturns
    /// </summary>
    /// <seealso cref="PutFragsStoppedEventArgs"/>
    class GetFragsStoppedEventArgs : FragOpStoppedEventArgs {
      public readonly DhtGetResult[] Fragments;

      /// <summary>
      /// Use this in the case of success
      /// </summary>
      public GetFragsStoppedEventArgs(DhtGetResult[] fragments)
        : base(null) {
        this.Fragments = fragments;
      }

      /// <summary>
      /// Use this in the case of failure
      /// </summary>
      public GetFragsStoppedEventArgs(AsyncOpState failedPiece)
        : base(failedPiece) {
      }
    }
    #endregion

    #region Async Methods
    /// <summary>
    /// Handles results of async puts of pieces. 
    /// </summary>
    /// <remarks>Fails on the first failed piece.</remarks>
    public void OnDhtPutReturns(IAsyncResult result) {
      AsyncResult ar = (AsyncResult)result;
      AsyncFragsOpState apfs = (AsyncFragsOpState)ar.AsyncState;
      BrunetDhtPutOp op = (BrunetDhtPutOp)ar.AsyncDelegate;
      AsyncPutFragsGlobalState global_state =
        (AsyncPutFragsGlobalState)apfs.GlobalState;
      AsyncOpState piece_state = apfs.PieceState;

      bool succ = op.EndInvoke(ar);
      lock (global_state.SyncRoot) {
        if (succ) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Put succeeded for piece: {0}",
            Encoding.UTF8.GetString(piece_state.Key)));

          global_state.OpSuccCount++;
          if (global_state.OpSuccCount == global_state.ExpectedPositiveReturnNum) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("All pieces of {0} successfully put. Firing PutStoppedEvent",
              Encoding.UTF8.GetString(global_state.BaseKey)));
            global_state.Returns.Enqueue(new PutFragsStoppedEventArgs(null));
          }
          if ((global_state.Concurrency > 1 && global_state.OpSuccCount %
            global_state.Concurrency == 0) || global_state.OpSuccCount ==
            global_state.ExpectedPositiveReturnNum) {
            // put concurrently and a batch finishes.
            global_state.BatchFinishedEvent.Set();
          }
        } else {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Put failed for piece: {0}. Firing PutStoppedEvent",
            Encoding.UTF8.GetString(piece_state.Key)));
          //No retry at this level currently, stop put operation.
          global_state.Returns.Enqueue(new PutFragsStoppedEventArgs(piece_state));
        }
      }
    }

    /// <summary>
    /// Concurrently puts pieces into DHT
    /// </summary>
    /// <returns>True if successful</returns>
    private bool PutFragsConcurrently(FragmentationInfo fragInfo,
    byte[] infoKey, int ttl, IList<FingerprintedData> fragments) {
      bool ret;
      //prepare the global state
      int max_enqueues = 1; //we only need a true/false answer
      BlockingQueue bq_result = new BlockingQueue(max_enqueues);
      AsyncPutFragsGlobalState global_state =
          new AsyncPutFragsGlobalState(fragments.Count, fragInfo.BaseKey, bq_result);
      //@TODO tweak the number of concurrency_degree here.
      int concurrency_degree = Concurrency;
      global_state.Concurrency = concurrency_degree;
      //Put pieces
      for (int index = 0; index < fragments.Count; index++) {
        FingerprintedData fpd = fragments[index];
        byte[] serializedFpd = fpd.SerializeTo();
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("Size after serialization {0}", serializedFpd.Length));
        byte[] piece_key = BuildFragmentKey(fragInfo.BaseKey, index);
        string piece_key_string = Encoding.UTF8.GetString(piece_key);
        //piece state
        AsyncOpState aps = new AsyncOpState(piece_key, serializedFpd, ttl);
        AsyncFragsOpState apfs = new AsyncFragsOpState(global_state, aps);
        //async put, one instance of IDht per put because thread safty not guaranteed
        IDht proxy = DhtServiceClient.GetXmlRpcDhtClient(_svc_uri.Port);
        BrunetDhtClientOperations brdht_ops = new BrunetDhtClientOperations(
          proxy);
        // Fire the async XML-RPC call.
        lock (global_state.SyncRoot) {
          brdht_ops.BeginPutWithCallback(piece_key, serializedFpd, ttl,
          new AsyncCallback(this.OnDhtPutReturns), apfs);
        }

        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Put piece {0} to DHT began (asynchronously)",
          piece_key_string));

        if ((concurrency_degree > 1 && (index + 1) % concurrency_degree == 0) ||
          index == fragments.Count - 1) {
          // Stop to wait for batch finish or all finish
          global_state.BatchFinishedEvent.WaitOne();
          if (concurrency_degree > 1) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("Batch {0} finished. Moving on",
              (int)((index + 1) / concurrency_degree)));
          }
        }
      }

      //block here until result comes
      PutFragsStoppedEventArgs stop_args =
        (PutFragsStoppedEventArgs)bq_result.Dequeue();

      // Deal with info object.
      if (stop_args.IsSuccessful) {
        ret = _dht.Put(infoKey, fragInfo.SerializeTo(), ttl);
        if (ret) {
          Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            string.Format("FragmentationInfo {0} successfully put",
            Encoding.UTF8.GetString(infoKey)));
        }
      } else {
        ret = false;
      }

      return ret;
    }

    public void OnDhtGetReturns(IAsyncResult result) {
      AsyncResult ar = (AsyncResult)result;
      AsyncFragsOpState agfs = (AsyncFragsOpState)ar.AsyncState;
      BrunetDhtGetOp op = (BrunetDhtGetOp)ar.AsyncDelegate;
      AsyncGetFragsGlobalState global_state =
        (AsyncGetFragsGlobalState)agfs.GlobalState;
      AsyncOpState piece_state = agfs.PieceState;

      DhtGetResult[] returns = op.EndInvoke(ar);

      if (returns != null && returns.Length > 0) {
        // We only need the first one.
        DhtGetResult dgr = returns[0];
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Get succeeded for piece: {0}",
          Encoding.UTF8.GetString(piece_state.Key)));
        lock (global_state.SyncRoot) {
          byte[] piece_key = piece_state.Key;
          int piece_indx = BrunetDht.GetPieceIndexFromFragmentKey(piece_key);
          global_state.Fragments[piece_indx] = dgr;
          global_state.OpSuccCount++;

          if ((global_state.Concurrency > 1 && global_state.OpSuccCount %
            global_state.Concurrency == 0) || global_state.OpSuccCount ==
            global_state.ExpectedPositiveReturnNum) {
            // put concurrently and a batch finishes.
            global_state.BatchFinishedEvent.Set();
          }
          if (global_state.OpSuccCount == global_state.ExpectedPositiveReturnNum) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("All pieces of {0} successfully got. Firing GetStoppedEvent",
              Encoding.UTF8.GetString(global_state.BaseKey)));
            global_state.Returns.Enqueue(new GetFragsStoppedEventArgs(global_state.Fragments));
          }
        }
      } else {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Get failed for piece: {0}. Firing GetStoppedEvent",
          Encoding.UTF8.GetString(piece_state.Key)));
        //No retry at this level currently, stop put operation.
        lock (global_state.SyncRoot) {
          global_state.Returns.Enqueue(new PutFragsStoppedEventArgs(piece_state));
        }
      }
    }

    /// <summary>
    /// Concurrenly gets the fragments from DHT.
    /// </summary>
    /// <returns>The fragments</returns>
    private MemBlock GetFragsConcurrently(byte[] baseKey, int pieceNum,
      out int largestAge, out int smallestTtl) {
      // First set the int values to be invalid.
      smallestTtl = Int32.MaxValue;
      largestAge = 0;

      int max_enqueues = 1;
      BlockingQueue bq_result = new BlockingQueue(max_enqueues);
      AsyncGetFragsGlobalState global_state =
        new AsyncGetFragsGlobalState(pieceNum, baseKey, bq_result);
      int concurrency_degree = Concurrency;
      global_state.Concurrency = concurrency_degree;

      for (int i = 0; i < pieceNum; i++) {
        int index = i;
        byte[] piece_key = BuildFragmentKey(baseKey, i);
        string piece_key_string = Encoding.UTF8.GetString(piece_key);
        // piece state
        AsyncOpState aps = new AsyncOpState(piece_key);
        AsyncFragsOpState afos = new AsyncFragsOpState(global_state, aps);
        // async get, one instance of IDht per get because thread safty not guaranteed
        IDht proxy = DhtServiceClient.GetXmlRpcDhtClient(_svc_uri.Port);
        BrunetDhtClientOperations brdht_ops = new BrunetDhtClientOperations(
          proxy);
        lock (global_state.SyncRoot) {
          brdht_ops.BeginGetWithCallback(piece_key,
            new AsyncCallback(this.OnDhtGetReturns), afos);
        }
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Get piece {0} from DHT began (asynchronously)",
          piece_key_string));

        if ((concurrency_degree > 1 && (index + 1) % concurrency_degree == 0) ||
          index == pieceNum - 1) {
          // Stop to wait for batch finish or all finish
          global_state.BatchFinishedEvent.WaitOne();
          if (concurrency_degree > 1) {
            Logger.WriteLineIf(LogLevel.Verbose, _log_props,
              string.Format("Batch {0} finished. Moving on",
              (int)((index + 1) / concurrency_degree)));
          }
        }
      }

      //block here until result comes
      GetFragsStoppedEventArgs stop_args =
        (GetFragsStoppedEventArgs)bq_result.Dequeue();
      //All results have returned.
      MemBlock ret = new MemBlock();
      if (stop_args.IsSuccessful) {
        // We successfully got everything but aren't sure whether they are 
        // correct. Now parse them.
        for (int i = 0; i < global_state.Fragments.Length; i++) {
          try {
            DhtGetResult dgr = global_state.Fragments[i];
            FingerprintedData fpd =
                (FingerprintedData)DictionaryData.CreateDictionaryData(dgr.value);
            RegularData rd = fpd.InnerData as RegularData;
            // This piece is OK.
            ret = MemBlock.Concat(ret, MemBlock.Reference(rd.PayLoad));
            if (smallestTtl > dgr.ttl)
              smallestTtl = dgr.ttl;
            if (largestAge < dgr.age)
              largestAge = dgr.age;
            //Now it's safe to say, this attempt succeeded.
          } catch (Exception) {
            Logger.WriteLineIf(LogLevel.Error, _log_props,
              string.Format("Parsing Piece failed at index {0}", i));
            throw;
          }
        }
        return ret;
      }
      return null;
    }
    #endregion
  }
}
