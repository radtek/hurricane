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
using System.Collections;

namespace Fushare.Filesystem {
  /// <summary>
  /// Context class for the virtual file system.
  /// </summary>
  public class FilesysContext {
    static readonly IDictionary _log_props =
      Logger.PrepareLoggerProperties(typeof(FilesysContext));
    /// <summary>
    /// The key is the virtual path. 
    /// </summary>
    readonly Dictionary<IntPtr, OpenFileInfo> _openFiles =
      new Dictionary<IntPtr, OpenFileInfo>();

    public FilesysContext() { }

    public void AddOpenFile(IntPtr fileHandle, OpenFileInfo openFileInfo) {
      lock (_openFiles) {
        _openFiles[fileHandle] = openFileInfo;
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Open file {0}:{1} added to filesys context.", 
          fileHandle, openFileInfo.VirtualPath));
      }
    }

    public void RemoveOpenFile(IntPtr fileHandle) {
      lock (_openFiles) {
        _openFiles.Remove(fileHandle);
        Logger.WriteLineIf(LogLevel.Verbose, _log_props,
          string.Format("Open file {0} removed from filesys context.", 
          fileHandle));
      }
    }

    public bool TryGetOpenFileInfo(IntPtr fileHandle, out OpenFileInfo openFileInfo) {
      lock (_openFiles) {
        return _openFiles.TryGetValue(fileHandle, out openFileInfo);
      }
    }
  }
}
