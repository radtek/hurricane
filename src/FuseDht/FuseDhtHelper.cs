using Mono.Unix.Native;
using System;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.IO;
using System.Collections;
using Ipop;
using Brunet;
using System.Diagnostics;

namespace FuseDht {  
  /// <summary>
  /// Deal with Dht operations for FuseDht class
  /// </summary>
  public class FuseDhtHelper {
    private IDht _dht;
    private string _shadowdir;
    private readonly string _dht_addr;

    public string DhtAddress {
      get { return _dht_addr; }
    }
    
    public FuseDhtHelper(IDht dht, string shadowdir) {
      _dht = dht;
      this._shadowdir = shadowdir;

      this._dht_addr = _dht.GetDhtInfo()["address"] as string;
    }

    //public void AfterDelete(IAsyncResult itfAR) {
    //  Debug.WriteLine(string.Format("AfterDelete called. Current Thread: {0}", Thread.CurrentThread.GetHashCode()));
    //  AsyncResult ar = (AsyncResult)itfAR;
    //  DequeueOp op = (DequeueOp)ar.AsyncDelegate;
    //  bool timedout;
    //  bool succ = (bool)op.EndInvoke(out timedout, itfAR);
    //  Console.WriteLine("Is Successful={0}", succ);
    //  IList state = (IList)itfAR.AsyncState;
    //  string shadowfile = (string)state[0];
    //  string passwd = (string)state[1];
    //  if (succ) {
    //    bool ifmy;
    //    DirectoryInfo dir = new DirectoryInfo(shadowfile);
    //    string dirname = dir.Parent.Name;
    //    //delete shadow file
    //    File.Delete(shadowfile);
    //    if (dirname.Equals(Constants.DIR_MY)) {
    //      //delete passwd
    //      lock (this.passwordsFileLock) {
    //        FuseDhtUtil.DeletePasswdFromPasswords(shadowfile);
    //      }
    //    }
    //  }
    //}

    //public void AfterPut(IAsyncResult itfAR) {
    //  Console.WriteLine("AfterCreat called. Current Thread: {0}", Thread.CurrentThread.GetHashCode());
    //  AsyncResult ar = (AsyncResult)itfAR;
    //  DequeueOp op = (DequeueOp)ar.AsyncDelegate;
    //  bool timedout;
    //  bool succ = (bool)op.EndInvoke(out timedout, itfAR);
    //  Console.WriteLine("Is Successful={0}", succ);
    //  IList state = (IList)itfAR.AsyncState;
    //  string shadowfile = (string)state[0];
    //  string passwd = (string)state[1];
    //  PutMode putmode = (PutMode)state[2];

    //  if (putmode == PutMode.Recreate) {
    //    //no further task
    //    return;
    //  }
    //  //i.e. /home/jx/shadowdht/key/my/p2pid1.txt
    //  string[] paths = FuseDhtUtil.ParsePath(shadowfile);
    //  DirectoryInfo dir = new DirectoryInfo(shadowfile);	//ctor accpets file path
    //  dir = dir.Parent;	//my
    //  dir = dir.Parent;	//key
    //  //string 
    //  if (!succ) {
    //    if (dir.GetFiles(Constants.FILE_OFFLINE).Length == 0) {
    //      string offline = dir.FullName + "/" + Constants.FILE_OFFLINE;
    //      try {
    //        File.Create(offline);
    //      } catch {
    //        Console.WriteLine("Creating {0} failed", offline);
    //      }
    //    }
    //  } else {
    //    Console.WriteLine("Put successful!");
    //    //delete offline file
    //    FileInfo[] fi = dir.GetFiles(Constants.FILE_OFFLINE);
    //    if (fi.Length != 0) {
    //      try {
    //        fi[0].Delete();
    //      } catch {
    //        Console.WriteLine("Deleting {0} failed", fi[0].FullName);
    //      }
    //    }
    //    //append passwd
    //    try {
    //      lock (passwordsFileLock) {
    //        FuseDhtUtil.Append2Passwords(passwd, shadowfile);
    //      }
    //    } catch (Exception e) {
    //      Console.WriteLine("Error when append passwd to file. \n" + e.ToString());
    //    }
    //  }
    //}

//    public void DhtPut(string shadowdir, string path) {
//      byte[] buf;
//      int ttl;
//      string[] paths = FuseDhtUtil.ParsePath(path);
//      string key = paths[0];
//      int millisec;
//      PutMode putmode;
//      try {
//        buf = File.ReadAllBytes(shadowdir + path);	//open,read,close
//        //				ttl = Int32.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(shadowdir + "/" + key + "/" + Constants.DIR_ETC + "/" + Constants.FILE_TTL)));
//        //				millisec = Int32.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(shadowdir + "/" + key + "/" + Constants.DIR_ETC + "/" + Constants.FILE_DQ_WAIT)));
//        //				string pm = Encoding.UTF8.GetString(File.ReadAllBytes(shadowdir + "/" + key + "/" + Constants.DIR_ETC + "/" + Constants.FILE_TTL));
//        //				putmode = Constants.GetPutMode(pm);
//        ttl = (int)FuseDhtUtil.ReadParam(shadowdir, key, Constants.FILE_TTL);
//        millisec = (int)FuseDhtUtil.ReadParam(shadowdir, key, Constants.FILE_DQ_WAIT);
//        putmode = (PutMode)FuseDhtUtil.ReadParam(shadowdir, key, Constants.FILE_PUT_MODE);
//      } catch {
//        Console.Error.WriteLine("Exception caught when read shadow file");
//        return;
//      }
//      string passwd;

//      //call dht
//      BlockingQueue bq;
//      switch (putmode) {
//        case PutMode.Create:
//          passwd = FuseDhtUtil.GenHashedPasswd();
//          bq = _dht.Create(Encoding.UTF8.GetBytes(key), ttl, passwd, buf);
//          break;
//        case PutMode.Recreate:
//          passwd = FuseDhtUtil.GetPasswdFromPasswords(shadowdir + path);
//          if (passwd == null) {
//            Console.WriteLine("No passwd found, cannot recreate");
//            return;
//          }
//          bq = _dht.Recreate(Encoding.UTF8.GetBytes(key), ttl, passwd, buf);
//          break;
//        case PutMode.Put:
//        default:
//          //put by default
//          passwd = FuseDhtUtil.GenHashedPasswd();
//          bq = _dht.Put(Encoding.UTF8.GetBytes(key), ttl, passwd, buf);
//          break;
//      }

//      DequeueOp op = new DequeueOp(bq.Dequeue);

//      bool timedout;
//      IList state = new ArrayList();
//      state.Add(shadowdir + path);
//      state.Add(passwd);
//      state.Add(putmode);

//      op.BeginInvoke(millisec, out timedout, new AsyncCallback(AfterPut), state);
//      Console.WriteLine("Current Thread: {0}", Thread.CurrentThread.GetHashCode());
//#if FUSE_UNIT
//      Console.WriteLine("Waiting Here");
//      Thread.Sleep(5000);
//#endif
//    }

//    public void DhtGet(string shadowdir, string cachepath) {
//      DirectoryInfo di = new DirectoryInfo(shadowdir + cachepath);
//      di.Delete(true);
//      di.Create();
//      using (StreamWriter wr = File.CreateText(di.FullName + "/" + Constants.FILE_DONE)) {
//        wr.WriteLine("0");
//        wr.Close();
//      }

//      //			string mbpath = di.Parent.FullName + "/" + Constants.DIR_ETC + "/" + Constants.FILE_MAX_BYTES;
//      //			string dq_waitpath = di.Parent.FullName + "/" + Constants.DIR_ETC + "/" + Constants.FILE_DQ_WAIT;
//      string key = di.Parent.Name;
//      byte[] token = null;
//      int mb = (int)FuseDhtUtil.ReadParam(shadowdir, key, Constants.FILE_MAX_BYTES);
//      int millisec = (int)FuseDhtUtil.ReadParam(shadowdir, key, Constants.FILE_DQ_WAIT);

//      //			try
//      //			{
//      //				mb = Int32.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(mbpath)));
//      //			}
//      //			catch
//      //			{
//      //				Console.WriteLine(Constants.FILE_MAX_BYTES + "not valid or not found, use conf file instead");
//      //				try
//      //				{
//      //					mb = Int32.Parse(FuseDhtUtil.GetValueFromConf(shadowdir, Constants.FILE_MAX_BYTES));
//      //				}
//      //				catch
//      //				{
//      //					Console.WriteLine(Constants.FILE_MAX_BYTES + "not valid in conf, use hardcoded value instead");
//      //					mb = Constants.DEFAULT_MAX_BYTES;
//      //				}
//      //			}
//      //			
//      //			//TODO improve the parameter reading mechanism
//      //			millisec = Int32.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dq_waitpath)));
//      IList state = new ArrayList();
//      state.Add(key);
//      state.Add(mb);
//      state.Add(millisec);
//      state.Add(shadowdir);

//      Console.WriteLine("Putting KeepGetting in queue");
//      ThreadPool.QueueUserWorkItem(new WaitCallback(this.KeepGetting), state);
//#if FUSE_UNIT
//      Console.WriteLine("Waiting Here");
//      Thread.Sleep(5000);
//#endif
//    }

    //return ture if dht is called.
//    public bool DhtDelete(string shadowdir, string path) {
//      bool ret = false;
//      string[] paths = FuseDhtUtil.ParsePath(path);
//      string key = paths[0];
//      string folder = paths[1];	///key1/my
//      string filename = paths[2];
//      string passwd = null;
//      BlockingQueue bq;
//      if (folder.Equals(Constants.DIR_MY)) {
//        //read passwd from passwords
//        try {
//          passwd = FuseDhtUtil.GetPasswdFromPasswords(shadowdir + path);
//          if (string.IsNullOrEmpty(passwd)) {
//            //no entry in the file
//            passwd = Encoding.UTF8.GetString(File.ReadAllBytes(shadowdir + "/" + key + "/" + Constants.DIR_ETC + "/" + Constants.FILE_PASSWD));
//          }
//        } catch (Exception e) {
//          //passwords file not found or malformed
//          throw new FileStructureException("Read passwd error", e);
//        }
//      } else if (folder.Equals(Constants.DIR_CACHE)) {
//        try {
//          byte[] bp = File.ReadAllBytes(shadowdir + "/" + key + "/" + Constants.DIR_ETC + "/" + Constants.FILE_PASSWD);
//          //the passwd file might be empty
//          if (bp != null) {
//            passwd = Encoding.UTF8.GetString(bp);
//          }
//        } catch (Exception e) {
//          //passwd file not found or malformed
//          throw new FileStructureException("Read passwd error", e);
//        }
//      }

//      if (!string.IsNullOrEmpty(passwd)) {
//        bq = this._dht.Delete(Encoding.UTF8.GetBytes(key), passwd);
//        DequeueOp op = new DequeueOp(bq.Dequeue);

//        bool timedout;
//        IList state = new ArrayList();
//        state.Add(shadowdir + path);
//        state.Add(passwd);
//        int millisec = (int)FuseDhtUtil.ReadParam(shadowdir, key, Constants.FILE_DQ_WAIT);
//        //				try
//        //				{
//        //					string strDqwait = Encoding.UTF8.GetString(File.ReadAllBytes(shadowdir + "/" + key + "/" + Constants.DIR_ETC + "/" + Constants.FILE_DQ_WAIT));
//        //					millisec = Int32.Parse(strDqwait);
//        //				}
//        //				catch
//        //				{
//        //					//get default value
//        //				}
//        op.BeginInvoke(millisec, out timedout, new AsyncCallback(AfterDelete), state);
//        Console.WriteLine("Current Thread: {0}", Thread.CurrentThread.GetHashCode());
//#if FUSE_UNIT
//        Console.WriteLine("Waiting Here");
//        Thread.Sleep(5000);
//#endif
//        return true;
//      } else {
//        Console.WriteLine("No passwd found. File won't be deleted");
//        return false;
//      }
//    }

//    private void KeepGetting(object state) {
//#if FUSE_DEBUG
//      Console.WriteLine("KeepGetting called!");
//#endif
//      IList lstate = (IList)state;
//      string key = (string)lstate[0];
//      int mb = (int)lstate[1];
//      int millisec = (int)lstate[2];
//      string shadowdir = (string)lstate[3];
//#if FUSE_DEBUG
//      Console.WriteLine("Params:key={0}, mb={1}, millisec={2}, shadowdir={3}", key, mb, millisec, shadowdir);
//#endif
//      BlockingQueue bq;

//      int remaining_items;
//      byte[] token = null;
//      DhtGetResultItem[] items;
//      bool firstbatch = true;
//      do {
//#if FUSE_DEBUG
//        Console.WriteLine("Calling Dht.Get");
//#endif
//        bq = _dht.Get(Encoding.UTF8.GetBytes(key), mb, token);
//        bool timedout = false;
//        Console.WriteLine("Dequeuing");
//        DhtGetResult result = new DhtGetResult((IList)(bq.Dequeue(millisec, out timedout)));

//        if (timedout == true) {
//          Console.WriteLine("Deqeue timedout, quit Get operation");
//          return;
//        }

//        if (firstbatch) {
//          Console.WriteLine("First batch");
//          string offlinepath = shadowdir + "/" + key + "/" + Constants.FILE_OFFLINE;
//          if (File.Exists(offlinepath)) {
//            File.Delete(offlinepath);
//          }
//          firstbatch = false;
//        }

//        remaining_items = result.remaining_items;
//        token = result.next_token;
//        items = result.items;
//        Console.WriteLine("Items:{0}", items.Length);
//        foreach (DhtGetResultItem item in items) {
//          string filename = FuseDhtUtil.GenFilenameFromContent(item.data, Constants.DEFAULT_FN_LENGTH);
//          string filepath = shadowdir + "/" + key + "/" + Constants.DIR_CACHE + "/" + filename;
//          while (File.Exists(filepath)) {
//            filepath += "_";
//          }

//          //filename ok now
//          using (FileStream fs = File.Create(filepath)) {
//            fs.Write(item.data, 0, item.data.Length);
//          }
//        }
//      } while (remaining_items > 0);

//      string donepath = shadowdir + "/" + key + "/" + Constants.DIR_CACHE + "/" + Constants.FILE_DONE;
//      using (StreamWriter wr = File.CreateText(donepath)) {
//        wr.WriteLine("1");
//        Console.WriteLine("Set .done to 1");
//      }
//    }
  }

}
