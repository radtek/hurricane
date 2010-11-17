using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GatorShare {
  /// <summary>
  /// A utility class that intends to make retries easier.
  /// </summary>
  public class RetryUtil {
    public static TResult RetryFunc<T, TResult>(Func<T, TResult> func, T param1, int numRetries, int retryTimeout) {
      if (func == null)
        throw new ArgumentNullException("func");
      TResult result;
      do {
        try {
          result = func(param1); 
        } catch {
          if (numRetries <= 0)
            throw;
          else
            result = default(TResult);
            Thread.Sleep(retryTimeout);
        }
      } while (numRetries-- > 0);

      return result;
    }
  }
}
