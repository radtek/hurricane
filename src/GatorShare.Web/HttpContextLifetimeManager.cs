using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Practices.Unity;

namespace GatorShare.Web {
  /// <summary>
  /// Stores the container in current HttpContext. This controls the lifetime of the 
  /// injections to be the same as the request.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class HttpContextLifetimeManager<T> : LifetimeManager, IDisposable {
    public override object GetValue() {
      return HttpContext.Current.Items[typeof(T).AssemblyQualifiedName];
    }

    public override void RemoveValue() {
      HttpContext.Current.Items.Remove(typeof(T).AssemblyQualifiedName);
    }

    public override void SetValue(object newValue) {
      HttpContext.Current.Items[typeof(T).AssemblyQualifiedName]
        = newValue;
    }

    public void Dispose() {
      RemoveValue();
    }
  }
}