// Copyright (c) 2011 Jiangyan Xu <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace GatorShare {
  /// <summary>
  /// Abstraction from particular logging tools.
  /// </summary>
  interface ILogger {
    void ConfigureLogger();
    void ConfigureLogger(string configFile);
    IDictionary PrepareLoggerProperties(Type objType);
    IDictionary PrepareNamedLoggerProperties(string loggerName);
    void WriteLineIf(LogLevel level, IDictionary logProperties, object message);
  }
}
