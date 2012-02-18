/*
Copyright (c) 2010 Jiangyan Xu <jiangyan@ufl.edu>, University of Florida

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;

namespace GSeries {
  /// <summary>
  /// A utility class that intends to make retries easier.
  /// </summary>
  public class RetryUtil {
    static readonly IDictionary _log_props =
      Logger.PrepareLoggerProperties(typeof(RetryUtil));

    /// <summary>
    /// Sets the number of retries.
    /// </summary>
    /// <value>The num retries.</value>
    public int NumRetries { private get; set; }
    /// <summary>
    /// Sets the retry delay. Have TimeSpan equal to 
    /// default if no delay is desired.
    /// </summary>
    /// <value>The retry delay.</value>
    public TimeSpan RetryDelay { private get;  set; }
    public int RetryExecuted { get; private set; }

    public RetryUtil() {
      NumRetries = 1;
    }

    /// <summary>
    /// Retries the function for specfied times with specified delays between 
    /// each retry.
    /// </summary>
    /// <typeparam name="T">The type of parameter.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="func">The function.</param>
    /// <param name="param1">The param1.</param>
    /// <param name="predicate">Determines whether this is the situtation that we
    /// should retry.</param>
    /// <returns></returns>
    public TResult RetryFunc<T, TResult, TException>(Func<T, TResult> func, T param1, 
      Predicate<TException> predicate) where TException : Exception {
      if (func == null)
        throw new ArgumentNullException("func");
      TResult result;
      int numRetriesLeft = NumRetries;
      do {
        RetryExecuted = NumRetries - numRetriesLeft;

        try {
          result = func(param1); 
        } catch (TException ex) {
          Logger.WriteLineIf(LogLevel.Error, _log_props, string.Format(
            "Exception caught when retrying. Retries left: {0}", numRetriesLeft));

          if (!predicate(ex)) {
            throw;
          }

          if (numRetriesLeft <= 0)
            // Retries exhausted.
            throw;
          else
            result = default(TResult);
            if (!RetryDelay.Equals(default(TimeSpan)))
              Thread.Sleep(RetryDelay);
        }
      } while (numRetriesLeft-- > 0);

      return result;
    }
  }
}
