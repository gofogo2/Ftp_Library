using System;
using System.Collections.Generic;
using System.IO;

namespace FTP
{
    /// <summary>
    /// ftp 디렉토리 정보
    /// </summary>
    public class FtpDirectory
    {
        #region Property

        public List<FtpDirectoryInfo> Files { get; private set; }
        public List<FtpDirectoryInfo> Directories { get; private set; }
        public string DirPath { get; private set; }

        #endregion

        #region Constructor

        public FtpDirectory()
        {
            this.Files = new List<FtpDirectoryInfo>();
            this.Directories = new List<FtpDirectoryInfo>();
        }

        public FtpDirectory(string responseStr, string dirPath)
            : this()
        {
            this.DirPath = dirPath;
            string[] split = responseStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in split)
            {
                var info = FtpDirectoryInfo.Create(line, dirPath);

                if (info != null)
                {
                    if (info.IsDirectory) this.Directories.Add(info);
                    else this.Files.Add(info);
                }
            }
        }

        #endregion

        #region Public Static Method

        public static string[] GetDirectories(string dirPath)
        {
            dirPath = dirPath.Replace("\\", "/");
            string[] dirNames = dirPath.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

            int startIndex = dirPath.StartsWith("ftp://") ? 2 : 0;
            string[] directories = new string[dirNames.Length - startIndex];
            string dir = "";
            int i = 0;

            for (; startIndex < dirNames.Length; startIndex++)
            {
                dir = Path.Combine(dir, dirNames[startIndex]);
                directories[i++] = dir.Replace("\\", "/");
            }

            return directories;
        }

        public static string GetParentDirectory(string dirPath)
        {
            string[] dirs = GetDirectories(dirPath);
            return dirs.Length > 1 ? dirs[dirs.Length - 2] : "";
        }

        #endregion
    }
}