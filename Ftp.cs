using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;

namespace FTP
{
    /// <summary>
    /// ftp 서버 이용에 필요한 기본 메소드를 정의
    /// </summary>
    public sealed partial class Ftp
    {
        #region Variable

        private const int DOWNLOAD_BYTE = 2048;
        private const int UPLOAD_BYTE = 2048;

        private List<object> _uploadClientList;
        private List<object> _downloadClientList;

        private bool _uploadCancel;
        private bool _downloadCancel;

        #endregion

        #region Property

        public FtpConnection Connection { get; set; }

        #endregion

        #region Evnet

        public event FtpDownloadProgressChangedEventHandler DownloadProgressChanged;
        public event FtpAsyncCompletedEventHandler DownloadFileAsyncCompleted;
        public event FtpDownloadStringCompletedHandler DownloadStringAsyncCompleted;

        public event FtpUploadProgressChangedEventHandler UploadProgressChanged;
        public event FtpAsyncCompletedEventHandler UploadFileAsyncCompleted;

        #endregion

        #region Constructor

        public Ftp()
        {
            this._uploadClientList = new List<object>();
            this._downloadClientList = new List<object>();
        }

        public Ftp(string hostName)
            : this()
        {
            this.Connection = new FtpConnection(hostName);
        }

        public Ftp(string hostName, string userName, string password)
            : this()
        {
            this.Connection = new FtpConnection(hostName, userName, password);
        }

        public Ftp(FtpConnection connection)
            : this()
        {
            this.Connection = connection;
        }

        #endregion

        #region Public Method

        /// <summary>
        /// 디렉토리 생성 ( 부모 디렉토리까지 모두 생성 )
        /// </summary>
        /// <param name="dirPath">디렉토리 경로</param>
        /// <returns>생성 여부</returns>
        public bool CreateDirectory(string dirPath)
        {
            FtpWebRequest request = this.Connection.GetRequest(dirPath, WebRequestMethods.Ftp.MakeDirectory);

            if (this.Connection.GetResponseString(request) == null)
            {
                string[] dirs = FtpDirectory.GetDirectories(dirPath);

                foreach (string dir in dirs)
                {
                    if (!DirectoryExists(dir))
                    {
                        request = this.Connection.GetRequest(dir, WebRequestMethods.Ftp.MakeDirectory);
                        if (this.Connection.GetResponseString(request) == null) return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 디렉토리 삭제 ( 서브 디렉토리와 파일들 모두 삭제 )
        /// </summary>
        /// <param name="dirPath">디렉토리 경로</param>
        /// <returns>삭제 여부</returns>
        public bool DeleteDirectory(string dirPath)
        {
            FtpWebRequest request = this.Connection.GetRequest(dirPath, WebRequestMethods.Ftp.RemoveDirectory);

            if (this.Connection.GetResponseString(request) == null)
            {
                var dirInfo = ListDirectoryDetails(dirPath);

                foreach (var file in dirInfo.Files)
                {
                    if (!DeleteFile(file.FullPath)) return false;
                }

                foreach (var dir in dirInfo.Directories)
                {
                    if (!DeleteDirectory(dir.FullPath)) return false;
                }

                if (!DeleteDirectory(dirPath)) return false;
            }

            return true;
        }

        /// <summary>
        /// 디렉토리 정보 ( 이름 )
        /// </summary>
        /// <param name="dirPath">디렉토리 경로</param>
        /// <returns>이름 리스트</returns>
        public List<string> ListDirectory(string dirPath)
        {
            FtpWebRequest request = this.Connection.GetRequest(dirPath, WebRequestMethods.Ftp.ListDirectory);
            string str = this.Connection.GetResponseString(request);

            if (str == null)
            {
                return new List<string>();
            }
            else
            {
                string[] split = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                return new List<string>(split);
            }
        }

        /// <summary>
        /// 디렉토리 상세 정보
        /// </summary>
        /// <param name="dirPath">디렉토리 경로</param>
        /// <returns>디렉토리 정보</returns>
        public FtpDirectory ListDirectoryDetails(string dirPath)
        {
            FtpWebRequest request = this.Connection.GetRequest(dirPath, WebRequestMethods.Ftp.ListDirectoryDetails);
            string str = this.Connection.GetResponseString(request);

            if (str == null) return null;
            else return new FtpDirectory(str, dirPath);
        }

        /// <summary>
        /// 디렉토리 존재 유무
        /// </summary>
        /// <param name="dirPath">디렉토리 경로</param>
        /// <returns>존재 유무</returns>
        public bool DirectoryExists(string dirPath)
        {
            /*
            string parentDir = FtpDirectory.GetParentDirectory(dirPath);
            var dirNames = ListDirectory(parentDir);

            if (dirNames == null)
            {
                throw new FtpException("디렉토리 정보를 가져 올 수 없습니다.");
            }
            else
            {
                var exists = dirNames.Contains(dirPath);

                if (!exists)
                {
                    List<string> fullNames = new List<string>();

                    foreach (var dirName in dirNames)
                    {
                        fullNames.Add(Path.Combine(parentDir, dirName));
                    }

                    dirPath = dirPath.Replace("/", "\\");
                    exists = fullNames.Contains(dirPath);
                }

                return exists;
            }*/

            var dirNames = ListDirectory(dirPath);

            if (dirNames == null)
            {
                throw new FtpException("디렉토리 정보를 가져 올 수 없습니다.");
            }
            else
            {
                return dirNames.Count > 0;
            }
        }

        /// <summary>
        /// 파일 삭제
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        /// <returns>삭제 여부</returns>
        public bool DeleteFile(string filePath)
        {
            FtpWebRequest request = this.Connection.GetRequest(filePath, WebRequestMethods.Ftp.DeleteFile);
            return this.Connection.GetResponseString(request) != null;
        }

        /// <summary>
        /// 파일 사이즈
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        /// <returns>파일 사이즈</returns>
        public long GetFileSize(string filePath)
        {
            FtpWebRequest request = this.Connection.GetRequest(filePath, WebRequestMethods.Ftp.GetFileSize);

            try
            {
                using (var response = this.Connection.GetResponse(request))
                {
                    long fileSize = response.ContentLength;
                    response.Close();
                    return fileSize;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return -1;
            }
        }

        /// <summary>
        /// 파일 존재 유무
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        /// <returns>존재 유무</returns>
        public bool FileExists(string filePath)
        {
            try
            {
                return GetFileSize(filePath) != -1;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return false;
            }
        }

        /// <summary>
        /// 이름 변경
        /// </summary>
        /// <param name="sourcePath">소스 경로</param>
        /// <param name="reName">변경할 이름</param>
        /// <returns>변경 여부</returns>
        public bool Rename(string sourcePath, string reName)
        {
            FtpWebRequest request = this.Connection.GetRequest(sourcePath, WebRequestMethods.Ftp.Rename);
            request.RenameTo = reName;

            return this.Connection.GetResponseString(request) != null;
        }

        /// <summary>
        /// 비동기 파일 업로드 ( WebClient )
        /// </summary>
        /// <param name="uploadPath">업로드할 ftp 파일 경로</param>
        /// <param name="localPath">업로드할 로컬 파일 경로</param>
        public void UploadFileAsyncWC(string uploadPath, string localPath)
        {
            var dirPath = Path.GetDirectoryName(uploadPath);

            if (!this.DirectoryExists(dirPath))
            {
                this.CreateDirectory(dirPath);
            }

            UploadFileAsyncWC(uploadPath, localPath, null);
        }

        /// <summary>
        /// 비동기 파일 업로드 ( FtpWebRequest )
        /// </summary>
        /// <param name="uploadPath">업로드 되는 ftp 파일 경로</param>
        /// <param name="localPath">업로드할 로컬 파일 경로</param>
        public void UploadFileAsync(string uploadPath, string localPath)
        {
            var dirPath = Path.GetDirectoryName(uploadPath);

            if (!this.DirectoryExists(dirPath))
            {
                this.CreateDirectory(dirPath);
            }

            UploadFileAsync(uploadPath, localPath, null);
        }

        /// <summary>
        /// 비동기 파일 다운로드 ( WebClient)
        /// </summary>
        /// <param name="downloadPath">다운로드할 ftp 파일 경로</param>
        /// <param name="localPath">다운로드 되는 로컬 파일 경로</param>
        public void DownloadFileAsyncWC(string downloadPath, string localPath)
        {
            DownloadFileAsyncWC(downloadPath, localPath, null);
        }

        /// <summary>
        /// 비동기 파일 다운로드 ( FtpWebRequest)
        /// </summary>
        /// <param name="downloadPath">다운로드할 ftp 파일 경로</param>
        /// <param name="localPath">다운로드 되는 로컬 파일 경로</param>
        public void DownloadFileAsync(string downloadPath, string localPath)
        {
            DownloadFileAsync(downloadPath, localPath, null);
        }

        public string DownloadString(string filePath)
        {
            string str = null;
            WebClient wc = null;

            try
            {
                using (wc = new WebClient())
                {
                    this._downloadClientList.Add(wc);

                    wc.Credentials = this.Connection.GetCredential();

                    Uri ftpUri = this.Connection.GetFtpUri(filePath);
                    str = wc.DownloadString(ftpUri);
                }
            }
            catch
            {
            }
            finally
            {
                this._downloadClientList.Remove(wc);
            }

            return str;
        }

        public void DownloadStringAsync(string filePath)
        {
            WebClient wc = null;

            try
            {
                using (wc = new WebClient())
                {
                    this._downloadClientList.Add(wc);

                    Uri ftpUri = this.Connection.GetFtpUri(filePath);

                    wc.Credentials = this.Connection.GetCredential();
                    wc.DownloadStringCompleted += wc_DownloadStringCompleted;
                    wc.DownloadStringAsync(ftpUri);
                }
            }
            catch
            {
                this._downloadClientList.Remove(wc);

                var onDownloadStringAsyncCompleted = DownloadStringAsyncCompleted;
                if (onDownloadStringAsyncCompleted != null)
                {
                    onDownloadStringAsyncCompleted(this, new FtpDownloadStringCompletedEventArgs(null));
                }
            }
        }

        private void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            this._downloadClientList.Remove(sender);

            var onDownloadStringAsyncCompleted = DownloadStringAsyncCompleted;
            if (onDownloadStringAsyncCompleted != null)
            {
                if (e.Error == null)
                {
                    onDownloadStringAsyncCompleted(this, new FtpDownloadStringCompletedEventArgs(e.Result));
                }
                else
                {
                    onDownloadStringAsyncCompleted(this, new FtpDownloadStringCompletedEventArgs(null));
                }
            }         
        }

        /// <summary>
        /// 업로드 취소
        /// </summary>
        public void CancelUploadAsync()
        {
            this._uploadCancel = true;
            Cancel(this._uploadClientList);
        }

        /// <summary>
        /// 다운로드 취소
        /// </summary>
        public void CancelDownloadAsync()
        {
            this._downloadCancel = true;
            Cancel(this._downloadClientList);
        }

        #endregion

        #region Private Method

        private void UploadFileAsyncWC(string uploadPath, string localPath, AsyncCompletedEventHandler completed)
        {
            Uri ftpUri = this.Connection.GetFtpUri(uploadPath);
            WebClient wc = null;

            using (wc = new WebClient())
            {
                this._uploadClientList.Add(wc);

                wc.Credentials = this.Connection.GetCredential();

                var OnUploadProgressChanged = UploadProgressChanged;
                if (OnUploadProgressChanged != null)
                {
                    wc.UploadProgressChanged += (s, e) =>
                    {
                        var args = e.UserState as FtpUploadProgressChangedEventArgs;
                        args.BytesSent = e.BytesSent;
                        OnUploadProgressChanged(this, args);
                    };
                }

                wc.UploadFileCompleted += (s, e) =>
                {
                    this._uploadClientList.Remove(s);

                    if (completed != null)
                    {
                        completed(this, e);
                    }
                    else
                    {
                        var OnUploadFileAsyncCompleted = UploadFileAsyncCompleted;
                        if (OnUploadFileAsyncCompleted != null)
                        {
                            OnUploadFileAsyncCompleted(this, new FtpAsyncCompletedEventArgs(e.Error, e.Cancelled));
                        }
                    }
                };

                try
                {
                    FileInfo fi = new FileInfo(localPath);
                    long fileSize = fi.Length;
                    var args = new FtpUploadProgressChangedEventArgs(0, fileSize); 
                    wc.UploadFileAsync(ftpUri, null, localPath, args);
                }
                catch (Exception exception)
                {
                    this._uploadClientList.Remove(wc);
                    throw new FtpException(exception.Message, exception);
                }
            }
        }

        private void UploadFileAsync(string uploadPath, string localPath, AsyncCompletedEventHandler completed)
        {
            BackgroundWorker uploadWorker = new BackgroundWorker();
            uploadWorker.WorkerReportsProgress = true;
            uploadWorker.WorkerSupportsCancellation = true;

            this._uploadClientList.Add(uploadWorker);
            var OnUploadProgressChanged = UploadProgressChanged;

            uploadWorker.DoWork += (s, e) =>
            {
                FtpWebRequest request = this.Connection.GetRequest(uploadPath, WebRequestMethods.Ftp.UploadFile);
                request.UseBinary = true;

                using (FileStream fs = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                {
                    request.ContentLength = fs.Length;

                    using (Stream stream = request.GetRequestStream())
                    {
                        byte[] buffer = new byte[UPLOAD_BYTE];
                        int readBytes = 0;

                        try
                        {
                            var args = new FtpUploadProgressChangedEventArgs(readBytes, fs.Length);
                            long sentBytes = 0;

                            while ((readBytes = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (uploadWorker.CancellationPending)
                                {
                                    e.Cancel = true;
                                    break;
                                }

                                stream.Write(buffer, 0, readBytes);

                                if (OnUploadProgressChanged != null)
                                {
                                    sentBytes += readBytes;
                                    args.BytesSent = sentBytes;
                                    uploadWorker.ReportProgress(0, args);
                                }
                            }

                            stream.Close();
                        }
                        catch (Exception exception)
                        {
                            e.Result = exception;
                            stream.Close();
                        }
                    }

                    fs.Close();
                }
            };

            if (OnUploadProgressChanged != null)
            {
                uploadWorker.ProgressChanged += (s, e) =>
                {
                    var args = e.UserState as FtpUploadProgressChangedEventArgs;
                    OnUploadProgressChanged(this, args);
                };
            }

            uploadWorker.RunWorkerCompleted += (s, e) =>
            {
                this._uploadClientList.Remove(s);
                var args = e;

                if (!e.Cancelled && e.Result is Exception)
                {
                    var exception = e.Result as Exception;
                    args = new RunWorkerCompletedEventArgs(e.Result, exception, e.Cancelled);
                }

                if (completed != null)
                {
                    completed(this, args);
                }
                else
                {
                    var OnUploadFileAsyncCompleted = UploadFileAsyncCompleted;
                    if (OnUploadFileAsyncCompleted != null)
                    {
                        OnUploadFileAsyncCompleted(this, new FtpAsyncCompletedEventArgs(args.Error, args.Cancelled));
                    }
                }
            };

            try
            {
                uploadWorker.RunWorkerAsync();
            }
            catch (Exception exception)
            {
                this._uploadClientList.Remove(uploadWorker);
                throw new FtpException(exception.Message, exception);
            }
        }

        private void DownloadFileAsyncWC(string downloadPath, string localPath, AsyncCompletedEventHandler completed)
        {
            Uri ftpUri = this.Connection.GetFtpUri(downloadPath);
            WebClient wc = null;

            using (wc = new WebClient())
            {
                this._downloadClientList.Add(wc);

                wc.Credentials = this.Connection.GetCredential();

                var OnDownloadProgressChanged = DownloadProgressChanged;
                if (OnDownloadProgressChanged != null)
                {
                    wc.DownloadProgressChanged += (s, e) =>
                    {
                        var args = e.UserState as FtpDownloadProgressChangedEventArgs;
                        args.BytesReceived = e.BytesReceived;
                        OnDownloadProgressChanged(this, args);
                    };
                }

                wc.DownloadFileCompleted += (s, e) =>
                {
                    this._downloadClientList.Remove(s);

                    if (completed != null)
                    {
                        completed(this, e);
                    }
                    else
                    {
                        var OnDownloadFileAsyncCompleted = DownloadFileAsyncCompleted;
                        if (OnDownloadFileAsyncCompleted != null)
                        {
                            OnDownloadFileAsyncCompleted(this, new FtpAsyncCompletedEventArgs(e.Error, e.Cancelled));
                        }
                    }
                };

                try
                {
                    long fileSize = GetFileSize(downloadPath);
                    var args = new FtpDownloadProgressChangedEventArgs(0, fileSize);
                    wc.DownloadFileAsync(ftpUri, localPath, args);
                }
                catch (Exception exception)
                {
                    this._downloadClientList.Remove(wc);
                    throw new FtpException(exception.Message, exception);
                }
            }
        }

        private void DownloadFileAsync(string downloadPath, string localPath, AsyncCompletedEventHandler completed)
        {
            BackgroundWorker downloadWorker = new BackgroundWorker();
            downloadWorker.WorkerReportsProgress = true;
            downloadWorker.WorkerSupportsCancellation = true;

            this._downloadClientList.Add(downloadWorker);
            var OnDownloadProgressChanged = DownloadProgressChanged;

            downloadWorker.DoWork += (s, e) =>
            {
                FtpWebRequest request = this.Connection.GetRequest(downloadPath, WebRequestMethods.Ftp.DownloadFile);
                request.UseBinary = true;

                using (var response = this.Connection.GetResponse(request))
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (FileStream fs = new FileStream(localPath, FileMode.Create, FileAccess.Write))
                        {
                            byte[] buffer = new byte[DOWNLOAD_BYTE];
                            int readBytes = 0;

                            try
                            {
                                long totalBytes = GetFileSize(downloadPath);
                                var args = new FtpDownloadProgressChangedEventArgs(readBytes, totalBytes);

                                while ((readBytes = stream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    if (downloadWorker.CancellationPending)
                                    {
                                        e.Cancel = true;
                                        break;
                                    }

                                    fs.Write(buffer, 0, readBytes);

                                    if (OnDownloadProgressChanged != null)
                                    {
                                        args.BytesReceived = fs.Length;
                                        downloadWorker.ReportProgress(0, args);
                                    }
                                }

                                fs.Close();
                            }
                            catch
                            {
                                fs.Close();
                            }
                        }

                        stream.Close();
                    }

                    response.Close();
                }
            };

            if (OnDownloadProgressChanged != null)
            {
                downloadWorker.ProgressChanged += (s, e) =>
                {
                    var args = e.UserState as FtpDownloadProgressChangedEventArgs;
                    OnDownloadProgressChanged(this, args);
                };
            }

            downloadWorker.RunWorkerCompleted += (s, e) =>
            {
                this._downloadClientList.Remove(s);
                var args = e;

                if (!e.Cancelled && e.Result is Exception)
                {
                    var exception = e.Result as Exception;
                    args = new RunWorkerCompletedEventArgs(e.Result, exception, e.Cancelled);
                }

                if (completed != null)
                {
                    completed(this, args);
                }
                else
                {
                    var OnAsyncDownloadCompleted = DownloadFileAsyncCompleted;
                    if (OnAsyncDownloadCompleted != null)
                    {
                        OnAsyncDownloadCompleted(this, new FtpAsyncCompletedEventArgs(e.Error, e.Cancelled));
                    }
                }
            };

            try
            {
                downloadWorker.RunWorkerAsync();
            }
            catch (Exception exception)
            {
                this._downloadClientList.Remove(downloadWorker);
                throw new FtpException(exception.Message, exception);
            }
        }

        private void Cancel(List<object> list)
        {
            foreach (var client in list)
            {
                if (client is WebClient) (client as WebClient).CancelAsync();
                else if (client is BackgroundWorker) (client as BackgroundWorker).CancelAsync();
            }
        }

        #endregion
    }
}