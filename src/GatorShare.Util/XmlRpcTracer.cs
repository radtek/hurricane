using CookComputing.XmlRpc;
using System.IO;
using System.Collections;
using System;

namespace GatorShare {
  /// <summary>
  /// Provides logging of XML-RPC messages
  /// </summary>
  /// <remarks>
  /// Like <c>Tracer</c> and <c>RequestResponseLogger</c> classes in XmlRpc.Net library but 
  /// leverages Fushare's Logging system.
  /// </remarks>
  public class XmlRpcTracer : XmlRpcLogger {
    private static readonly IDictionary _log_props = 
        Logger.PrepareLoggerProperties(typeof(XmlRpcTracer));
    
    protected override void OnRequest(object sender,
        XmlRpcRequestEventArgs e) {
      base.OnRequest(sender, e);
      this.DumpStream(e.RequestStream);
    }

    protected override void OnResponse(object sender,
        XmlRpcResponseEventArgs e) {
      base.OnResponse(sender, e);
      this.DumpStream(e.ResponseStream);
    }

    private void DumpStream(Stream stm) {
      stm.Position = 0;
      TextReader reader = new StreamReader(stm);
      String s = reader.ReadLine();
      while (s != null) {
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
            s);
        s = reader.ReadLine();
      }
    }
  }
}