using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace FTP
{
    /// <summary>
    /// ftp 서버에 업로드할 폴더 정보
    /// </summary>
    public class FtpUploadDirectory
    {
        #region Variable

        private List<FtpUploadFile> _files;

        #endregion

        #region Property

        public string UploadPath { get; private set; }

        public ReadOnlyCollection<FtpUploadFile> Files
        {
            get { return this._files.AsReadOnly(); }
        }

        #endregion

        #region Constructor

        public FtpUploadDirectory(string uploadPath)
        {
            this.UploadPath = uploadPath;
            this._files = new List<FtpUploadFile>();
        }

        #endregion

        #region Public Method

        public void Add(FtpUploadFile file)
        {
            if (file == null ||
                string.IsNullOrWhiteSpace(file.Name) ||
                string.IsNullOrWhiteSpace(file.LocalPath))
            {
                throw new FtpException("매개변수가 null 이거나 속성 값이 올바르지 않습니다.");
            }

            if (!File.Exists(file.LocalPath)) throw new FtpException("업로드할 파일이 존재하지 않습니다.");

            var exists = this._files.Exists(x => x.LocalPath == file.LocalPath && x.Name == file.Name);

            if (!exists)
            {
                this._files.Add(file);
            }
        }

        public void Add(string fileName, string fileLocalPath)
        {
            FtpUploadFile file = new FtpUploadFile(fileName, fileLocalPath);
            Add(file);
        }

        #endregion
    }

    /// <summary>
    /// ftp 업로드할 파일 정보ㅁ
    /// </summary>
    public class FtpUploadFile
    {
        #region Property

        public string Name { get; set; }
        public string LocalPath { get; set; }

        #endregion

        #region Constructor

        public FtpUploadFile()
        {
        }

        public FtpUploadFile(string name, string localPath)
        {
            this.Name = name;
            this.LocalPath = localPath;
        }

        #endregion
    }
}