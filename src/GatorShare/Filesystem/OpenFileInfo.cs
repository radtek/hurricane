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
using System.IO;

namespace GatorShare.Filesystem {
  /// <summary>
  /// An immutable class that contains information of an open file.
  /// </summary>
  public class OpenFileInfo {
    #region Fields
    readonly VirtualPath _virtualPath;
    readonly VirtualFile _virtualFile;
    readonly IntPtr _fileHandle;
    readonly FileAccess _fileAccess; 
    #endregion

    #region Properties
    public IntPtr FileHandle {
      get { return _fileHandle; }
    }

    public VirtualFile VirtualFile {
      get { return _virtualFile; }
    }

    public FileAccess FileAccess {
      get { return _fileAccess; }
    }


    public VirtualPath VirtualPath {
      get { return _virtualPath; }
    }
    #endregion

    public OpenFileInfo(IntPtr fileHandle, VirtualPath virtualPath, 
      VirtualFile virtualFile, FileAccess fileAccess) {
      _fileHandle = fileHandle;
      _virtualPath = virtualPath;
      _virtualFile = virtualFile;
      _fileAccess = fileAccess;
    }

    public override string ToString() {
      var sb = new StringBuilder();
      sb.AppendLine("FileHandle: " + FileHandle);
      sb.AppendLine("FileAccess: " + FileAccess);
      sb.AppendLine("VirtualPath: " + VirtualPath);
      return sb.ToString();
    }
  }
}
