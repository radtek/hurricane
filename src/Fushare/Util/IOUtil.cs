using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace Fushare {
  public static class IOUtil {
    public static string GetFileOrDirectoryName(string path) {
      return GetFileOrDirectoryName(path, true);
    }

    /// <summary>
    /// Gets the name of the file or directory.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="checkPath">if set to <c>true</c>, checks whether the path 
    /// exists.</param>
    public static string GetFileOrDirectoryName(string path, bool checkPath) {
      if (checkPath) {
        return GetFileOrDirectoryInfo(path).Name;
      } else {
        string[] segments = path.Split(new char[] { Path.DirectorySeparatorChar }, 
          StringSplitOptions.RemoveEmptyEntries);
        return segments[segments.Length - 1];
      }
    }

    public static DirectoryInfo GetParent(string path, bool checkPath) {
      if (checkPath) {
        var filesysInfo = GetFileOrDirectoryInfo(path);
        string parent;
        if (filesysInfo is DirectoryInfo) {
          parent = (filesysInfo as DirectoryInfo).Parent.FullName;
        } else {
          parent = (filesysInfo as FileInfo).Directory.FullName;
        }
        return new DirectoryInfo(parent);
      } else {
        // Treat it as directory. The class doesn't check existence.
        return new DirectoryInfo(path).Parent;
      }
    }

    /// <summary>
    /// Gets the file or directory info.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <exception cref="ArgumentException">If the file or directory doesn't exist.
    /// </exception>
    public static FileSystemInfo GetFileOrDirectoryInfo(string path) {
      FileSystemInfo info;
      var succ = TryGetFileOrDirectoryInfo(path, out info);
      if (succ) {
        return info;
      } else {
        throw new ArgumentException("Neither a file nor a directory exists.", "path");
      }
    }

    public static bool TryGetFileOrDirectoryInfo(string path, out FileSystemInfo info) {
      bool ret = true;
      if (Directory.Exists(path)) {
        info = new DirectoryInfo(path);
      } else if (File.Exists(path)) {
        info = new FileInfo(path);
      } else {
        info = null;
        ret = false;
      }
      return ret;
    }

    public static bool FileOrDirectoryExists(string path) {
      FileSystemInfo info;
      return TryGetFileOrDirectoryInfo(path, out info);
    }

    /// <summary>
    /// Checks whether the path is rooted.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="paramName">Name of the param.</param>
    /// <exception cref="ArgumentException">The path is not rooted.</exception>
    public static void CheckPathRooted(string path, string paramName) {
      if (!Path.IsPathRooted(path)) {
        throw new ArgumentException("The path should be rooted", paramName); 
      }
    }

    /// <summary>
    /// Checks whether the path is rooted.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <exception cref="ArgumentException">The path is not rooted.</exception>
    public static void CheckPathRooted(string path) {
      if (!Path.IsPathRooted(path)) {
        throw new ArgumentException("The path should be rooted");
      }
    }

    /// <summary>
    /// Prepares the directory for path. A file cannot be created without its parent 
    /// existing.
    /// </summary>
    /// <param name="path">The path. It doesn't have to exist.</param>
    public static void PrepareParentDiryForPath(string path) {
      var directory = GetParent(path, false);
      directory.Create();
    }

    /// <summary>
    /// Gets the random temp file/dir (Path) in system's temp directory.
    /// </summary>
    /// <remarks>The file/dir has not yet been created.</remarks>
    public static string GetRandomTempPath() {
      return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    }

    /// <summary>
    /// Writes all bytes to the file. The difference with File.WriteAllBytes is that it tries
    /// first to create all preceding directories.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="bytes">The bytes.</param>
    public static void WriteAllBytes(string path, byte[] bytes) {
      Directory.CreateDirectory(GetParent(path, false).FullName);
      File.WriteAllBytes(path, bytes);
    }

    /// <summary>
    /// Reads up to <c>bytesToRead</c> from the specified file.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="bytesToRead">The number of bytes to read.</param>
    /// <param name="fileLength">Length of the file if it is already known. (To save 
    /// some IO.)</param>
    /// <returns>The bytes read.</returns>
    public static byte[] Read(string path, long offset, int bytesToRead, long? fileLength) {
      var fi = new FileInfo(path);
      long fileLengthToUse = fileLength.HasValue ? fileLength.GetValueOrDefault() : fi.Length;
      int bytesCanBeRead = fileLengthToUse < offset + bytesToRead ? 
        (int)(fileLengthToUse - offset) : bytesToRead;
      byte[] ret = new byte[bytesCanBeRead];
      Stream stream;
      try {
        stream = File.OpenRead(path);
      } catch (IOException) {
        System.Threading.Thread.Sleep(2000);
        stream = File.OpenRead(path);
      }
      using (stream) {
        stream.Seek(offset, SeekOrigin.Begin);
        stream.Read(ret, 0, bytesCanBeRead);
      }
      return ret;
    }

    /// <summary>
    /// Reads up to <c>bytesToRead</c> from the specified file.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="bytesToRead">The number of bytes to read.</param>
    /// <returns>The bytes read.</returns>
    public static byte[] Read(string path, long offset, int bytesToRead) {
      return Read(path, offset, bytesToRead, null);
    }
  }
}
