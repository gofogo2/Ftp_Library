using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace FTP
{
    /// <summary>
    /// ftp 서버 이용에 필요한 기본메소드를 통한 확장 메소드들 정의
    /// </summary>
    public partial class Ftp
    {
        #region Variable

        private List<FtpUploadDirectory> _uploadDirList;
        private List<FtpDirectory> _downloadList;

        private int _uploadDirIndex;
        private int _uploadFileIndex;
        private int _uploadTotalCount;
        private int _uploadCount;

        private int _downloadDirIndex;
        private int _downloadFileIndex;
        private int _downloadTotalCount;
        private int _downloadCount;
        private string _downloadLocalDirPath;

        private UploadFileListChangedAction _uploadFileListChangedAction;
        private DownloadDirectoryChangedAction _downloadDirectoryChangedAction;

        private delegate void UploadFileListChangedAction(string uploadPath, string localPath, AsyncCompletedEventHandler completed);
        private delegate void DownloadDirectoryChangedAction(string downloadPath, string localPath, AsyncCompletedEventHandler completed);

        #endregion

        #region Event

        public event FtpUploadFileListChangedEventHandler UploadFileListChanged;
        public event FtpDownloadDirectoryChangedEventHandler DownloadDirectoryChanged;

        #endregion

        #region Public Method

        /// <summary>
        /// 모든 서브 디렉토리 반환
        /// </summary>
        /// <param name="dirInfo">디렉토리 정보</param>
        /// <returns>디렉토리 리스트</returns>
        public List<FtpDirectory> GetAllDirectories(FtpDirectory dirInfo)
        {
            List<FtpDirectory> list = new List<FtpDirectory>();

            if (dirInfo != null)
            {
                MakeDirectoryList(dirInfo, list);
            }

            return list;
        }

        /// <summary>
        /// 모든 서브 디렉토리 반환
        /// </summary>
        /// <param name="dirInfo">디렉토리 경로</param>
        /// <returns>디렉토리 리스트</returns>
        public List<FtpDirectory> GetAllDirectories(string dirPath)
        {
            var dirInfo = ListDirectoryDetails(dirPath);
            return GetAllDirectories(dirInfo);
        }

        /// <summary>
        /// 서브 디렉토리들의 모든 파일 반환
        /// </summary>
        /// <param name="dirInfo">디렉토리 정보</param>
        /// <returns>파일 정보 리스트</returns>
        public List<FtpDirectoryInfo> GetAllFiles(FtpDirectory dirInfo)
        {
            List<FtpDirectoryInfo> list = new List<FtpDirectoryInfo>();

            foreach (var dir in GetAllDirectories(dirInfo))
            {
                foreach (var file in dir.Files)
                {
                    list.Add(file);
                }
            }

            return list;
        }

        /// <summary>
        /// 현재 디렉토리의 서브 디렉토리 정보 반환
        /// </summary>
        /// <param name="dirInfo">디렉토리 정보</param>
        /// <returns>서브 디렉토리 리스트</returns>
        public List<FtpDirectory> ListSubDirectoryDetails(FtpDirectory dirInfo)
        {
            List<FtpDirectory> subDirectories = new List<FtpDirectory>();

            foreach (var dir in dirInfo.Directories)
            {
                var subDirInfo = ListDirectoryDetails(dir.FullPath);

                if (subDirInfo != null)
                {
                    subDirectories.Add(subDirInfo);
                }
                else
                {
                    return null;
                }
            }

            return subDirectories;
        }

        /// <summary>
        /// 리스트 안의 모든 파일들 업로드 ( WebClient )
        /// </summary>
        /// <param name="uploadDirList">업로드 폴더 정보</param>
        public void UploadFileListAsyncWC(FtpUploadDirectory ftpUpDir)
        {
            UploadFileList(ftpUpDir, UploadFileAsyncWC);
        }

        /// <summary>
        /// 리스트 안의 모든 파일들 업로드 ( WebClient )
        /// </summary>
        /// <param name="uploadDirList">업로드 폴더 정보 리스트</param>
        public void UploadFileListAsyncWC(List<FtpUploadDirectory> uploadDirList)
        {
            UploadFileList(uploadDirList, UploadFileAsyncWC);
        }

        /// <summary>
        /// 리스트 안의 모든 파일들 업로드 ( FtpWebRequest )
        /// </summary>
        /// <param name="uploadDirList">업로드 폴더 정보 리스트</param>
        public void UploadFileListAsync(List<FtpUploadDirectory> uploadDirList)
        {
            UploadFileList(uploadDirList, UploadFileAsync);
        }

        /// <summary>
        /// 디렉토리 경로의 모든 파일들 다운로드 ( WebClient )
        /// </summary>
        /// <param name="downloadPath">다운로드할 ftp 폴더 경로</param>
        /// <param name="localPath">다운로드 되는 로컬 폴더 경로</param>
        public void DownloadDirectoryAsyncWC(string downloadPath, string localPath)
        {
            DownloadDirectory(downloadPath, localPath, DownloadFileAsyncWC);
        }

        /// <summary>
        /// 디렉토리 경로의 모든 파일들 다운로드 ( FtpWebRequest )
        /// </summary>
        /// <param name="downloadPath">다운로드할 ftp 폴더 경로</param>
        /// <param name="localPath">다운로드 되는 로컬 폴더 경로</param>
        public void DownloadDirectoryAsync(string downloadPath, string localPath)
        {
            DownloadDirectory(downloadPath, localPath, DownloadFileAsync);
        }

        #endregion

        #region Private Method

        #region Upload

        private void UploadFileList(FtpUploadDirectory ftpUpDir, UploadFileListChangedAction action)
        {
            this._uploadDirList = new List<FtpUploadDirectory>();

            this._uploadFileListChangedAction = action;
            this._uploadDirList.Add(ftpUpDir);
            this._uploadDirIndex = 0;
            this._uploadFileIndex = 0;
            this._uploadTotalCount = ftpUpDir.Files.Count;
            this._uploadCount = 0;

            if ((_uploadDirList.Count > 0 && _uploadDirList[0].Files.Count > 0))
            {
                NextUploadFileList();
            }
            else
            {
                RaiseUploadFileListAsyncCompleted(new FtpAsyncCompletedEventArgs(null, false));
            }
        }

        private void UploadFileList(List<FtpUploadDirectory> uploadDirList, UploadFileListChangedAction action)
        {
            this._uploadFileListChangedAction = action;
            this._uploadDirList = uploadDirList;
            this._uploadDirIndex = 0;
            this._uploadFileIndex = 0;
            this._uploadTotalCount = 0;
            this._uploadCount = 0;

            foreach (var uploadDir in uploadDirList)
            {
                this._uploadTotalCount += uploadDir.Files.Count;
            }

            if ((uploadDirList.Count > 0 && uploadDirList[0].Files.Count > 0))
            {
                NextUploadFileList();
            }
            else
            {
                RaiseUploadFileListAsyncCompleted(new FtpAsyncCompletedEventArgs(null, false));
            }
        }

        private void NextUploadFileList()
        {
            var dir = this._uploadDirList[this._uploadDirIndex];

            if (!DirectoryExists(dir.UploadPath))
            {
                if (!CreateDirectory(dir.UploadPath))
                {
                    var error = new FtpException("디렉토리 생성 중 오류가 발생했습니다.");
                    RaiseUploadFileListAsyncCompleted(new FtpAsyncCompletedEventArgs(error, false));
                    return;
                }
            }

            if (dir.Files.Count > this._uploadFileIndex)
            {
                var file = dir.Files[this._uploadFileIndex++];
                string uploadPath = string.Format("{0}/{1}", dir.UploadPath, file.Name);

                ++this._uploadCount;

                RaiseUploadFileListChanged(new FtpUploadFileListChangedEventArgs(this._uploadTotalCount, this._uploadCount, file.Name, uploadPath));
                this._uploadFileListChangedAction(uploadPath, file.LocalPath, UploadFileCompleted);
            }
            else
            {
                this._uploadFileIndex = 0;
                ++this._uploadDirIndex;
                NextUploadFileList();
            }
        }

        private void UploadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            bool completed = false;
            int uploadLastDirIndex = this._uploadDirList.Count - 1;

            if (uploadLastDirIndex == this._uploadDirIndex)
            {
                var lastDir = this._uploadDirList[uploadLastDirIndex];
                completed = this._uploadFileIndex == lastDir.Files.Count;
            }

            if (e.Error != null || this._uploadCancel)
            {
                completed = true;
                this._uploadCancel = false;
            }

            if (!completed)
            {
                NextUploadFileList();
            }
            else
            {
                this._uploadDirList.Clear();
                this._uploadDirList = null;

                RaiseUploadFileListAsyncCompleted(new FtpAsyncCompletedEventArgs(e.Error, e.Cancelled));
            }
        }

        private void RaiseUploadFileListChanged(FtpUploadFileListChangedEventArgs e)
        {
            var OnUploadFileListChanged = UploadFileListChanged;
            if (OnUploadFileListChanged != null)
            {
                OnUploadFileListChanged(this, e);
            }
        }

        private void RaiseUploadFileListAsyncCompleted(FtpAsyncCompletedEventArgs e)
        {
            var OnUploadFileAsyncCompleted = UploadFileAsyncCompleted;
            if (OnUploadFileAsyncCompleted != null)
            {
                OnUploadFileAsyncCompleted(this, e);
            }
        }

        #endregion

        #region Download

        private void DownloadDirectory(string downloadPath, string localPath, DownloadDirectoryChangedAction action)
        {
            this._downloadDirIndex = 0;
            this._downloadFileIndex = 0;
            this._downloadTotalCount = 0;
            this._downloadCount = 0;
            this._downloadDirectoryChangedAction = action;
            this._downloadLocalDirPath = localPath;

            var rootDir = ListDirectoryDetails(downloadPath);

            if (rootDir != null)
            {
                this._downloadList = GetAllDirectories(rootDir);

                foreach (var dir in this._downloadList)
                {
                    this._downloadTotalCount += dir.Files.Count;
                }

                if (this._downloadList.Count > this._downloadDirIndex)
                {
                    var dir = this._downloadList[this._downloadDirIndex];
                    DownloadDirectory(dir);
                }
                else
                {
                    RaiseDownloadDirectoryAsyncCompleted(new FtpAsyncCompletedEventArgs(null, false));
                }
            }
            else
            {
                FtpException error = new FtpException("다운로드할 디렉토리 정보를 가져오지 못했습니다.");
                RaiseDownloadDirectoryAsyncCompleted(new FtpAsyncCompletedEventArgs(error, false));
            }
        }

        private void DownloadDirectory(FtpDirectory currentDir)
        {
            string dirLocalPath = string.Format("{0}/{1}", this._downloadLocalDirPath, currentDir.DirPath);

            if (!Directory.Exists(dirLocalPath))
            {
                Directory.CreateDirectory(dirLocalPath);
            }

            var files = currentDir.Files;

            if (files.Count > 0)
            {
                var file = files[0];
                DownloadFile(file);
            }
            else
            {
                NextDirectoryDownload();
            }
        }

        private void NextDirectoryDownload()
        {
            if (this._downloadList.Count > ++this._downloadDirIndex)
            {
                var nextDir = this._downloadList[this._downloadDirIndex];
                DownloadDirectory(nextDir);
            }
            else
            {
                this._downloadList.Clear();
                this._downloadList = null;

                RaiseDownloadDirectoryAsyncCompleted(new FtpAsyncCompletedEventArgs(null, false));
            }
        }

        private void DownloadFile(FtpDirectoryInfo file)
        {
            string downloadLocalPath = string.Format("{0}/{1}", this._downloadLocalDirPath, file.FullPath);
            var args = new FtpDownloadDirectoryChangedEventArgs(this._downloadTotalCount, ++this._downloadCount, file.Name, downloadLocalPath, file.FullPath);
            RaiseDownloadDirectoryChanged(args);

            this._downloadDirectoryChangedAction(file.FullPath, downloadLocalPath, DownloadFileCompleted);
            this._downloadFileIndex++;
        }

        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var currentDir = this._downloadList[this._downloadDirIndex];
            var files = currentDir.Files;

            bool completed = files.Count <= this._downloadFileIndex && this._downloadList.Count - 1 <= this._downloadDirIndex;

            if (e.Error != null || this._downloadCancel)
            {
                completed = true;
                this._downloadCancel = false;
            }

            if (!completed)
            {
                if (files.Count > this._downloadFileIndex)
                {
                    var file = files[this._downloadFileIndex];
                    DownloadFile(file);
                }
                else
                {
                    this._downloadFileIndex = 0;
                    NextDirectoryDownload();
                }
            }
            else
            {
                this._downloadList.Clear();
                this._downloadList = null;

                RaiseDownloadDirectoryAsyncCompleted(new FtpAsyncCompletedEventArgs(null, false));
            }
        }

        private void RaiseDownloadDirectoryChanged(FtpDownloadDirectoryChangedEventArgs args)
        {
            var OnDownloadDirectoryChanged = DownloadDirectoryChanged;
            if (OnDownloadDirectoryChanged != null)
            {
                OnDownloadDirectoryChanged(this, args);
            }
        }

        private void RaiseDownloadDirectoryAsyncCompleted(FtpAsyncCompletedEventArgs e)
        {
            var OnDownloadFileAsyncCompleted = DownloadFileAsyncCompleted;
            if (OnDownloadFileAsyncCompleted != null)
            {
                OnDownloadFileAsyncCompleted(this, e);
            }
        }

        #endregion

        private void MakeDirectoryList(FtpDirectory rootDir, List<FtpDirectory> list)
        {
            list.Add(rootDir);

            var subDirectories = ListSubDirectoryDetails(rootDir);

            if (subDirectories != null)
            {
                foreach (var subDir in subDirectories)
                {
                    MakeDirectoryList(subDir, list);
                }
            }
            else
            {
                list.Clear();
            }
        }

        #endregion
    }
}