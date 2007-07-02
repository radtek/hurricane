
using System;

namespace FuseDht {
  /**
   * This exception should be thrown when FuseDht experiences unexpected structural
   * errors like:
   * config files/arg files not in their places or contain invalid values
   */
  public class FuseDhtStructureException: Exception {
    /**
     * The path in fuse system where the exceptional situation occurs
     */
    private string _error_f_path;

    public string ErrorFusePath {
      get { return _error_f_path; }
    }

    public string ErrorPath {
      get { return _error_f_path; }
    }

    public FuseDhtStructureException()
      : base() {
    }

    public FuseDhtStructureException(string mes, string errorFPath)
      : base(mes) {
      this._error_f_path = errorFPath;
    }

    public FuseDhtStructureException(string message)
      : base(message) {
    }

    public FuseDhtStructureException(string errorFPath, Exception inner)
      : base("Valid configuration/file system structure expected", inner) {
      _error_f_path = errorFPath;
    }

    public FuseDhtStructureException(string mes, string errorFPath, Exception inner)
      : base(mes, inner) {
      _error_f_path = errorFPath;
    }

    public override string ToString() {
      return string.Format("ErrorFusePath: {0}\n", ErrorFusePath) + base.ToString();
    }
  }

}
