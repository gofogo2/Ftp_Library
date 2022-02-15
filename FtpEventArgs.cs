using System;

namespace FTP
{
    public delegate void FtpDownloadProgressChangedEventHandler(object sender, FtpDownloadProgressChangedEventArgs e);
    public delegate void FtpUploadProgressChangedEventHandler(object sender, FtpUploadProgressChangedEventArgs e);
    public delegate void FtpAsyncCompletedEventHandler(object sender, FtpAsyncCompletedEventArgs e);
    public delegate void FtpDownloadStringCompletedHandler(object sender, FtpDownloadStringCompletedEventArgs e);
    public delegate void FtpUploadFileListChangedEventHandler(object sender, FtpUploadFileListChangedEventArgs e);
    public delegate void FtpDownloadDirectoryChangedEventHandler(object sender, FtpDownloadDirectoryChangedEventArgs e);

    /// <summary>
    /// 다운로드 진행률
    /// </summary>
    public class FtpDownloadProgressChangedEventArgs
    {
        #region Property

        public long BytesReceived { get; set; }
        public long TotalBytesToReceive { get; set; }

        public int ProgressPercentage
        {
            get { return Convert.ToInt32(Convert.ToDouble(this.BytesReceived) / Convert.ToDouble(this.TotalBytesToReceive) * 100); }
        }

        #endregion

        #region Constructor

        public FtpDownloadProgressChangedEventArgs()
        {
        }

        public FtpDownloadProgressChangedEventArgs(long bytesReceived, long totalBytesToReceive)
        {
            this.TotalBytesToReceive = totalBytesToReceive;
            this.BytesReceived = bytesReceived;
        }

        #endregion
    }

    /// <summary>
    /// 업로드 진행률
    /// </summary>
    public class FtpUploadProgressChangedEventArgs
    {
        #region Property

        public long BytesSent { get; set; }
        public long TotalBytesToSend { get; set; }

        public int ProgressPercentage
        {
            get { return Convert.ToInt32(Convert.ToDouble(this.BytesSent) / Convert.ToDouble(this.TotalBytesToSend) * 100); }
        }

        #endregion

        #region Constructor

        public FtpUploadProgressChangedEventArgs()
        {
        }

        public FtpUploadProgressChangedEventArgs(long bytesSent, long totalBytesToSend)
        {
            this.TotalBytesToSend = totalBytesToSend;
            this.BytesSent = bytesSent;
        }

        #endregion
    }

    /// <summary>
    /// 비동기 진행 종료
    /// </summary>
    public class FtpAsyncCompletedEventArgs
    {
        #region Property

        public bool Cancelled { get; private set; }
        public Exception Error { get; private set; }

        #endregion

        #region Constructor

        public FtpAsyncCompletedEventArgs()
        {
        }

        public FtpAsyncCompletedEventArgs(Exception error, bool cancelled)
        {
            this.Cancelled = cancelled;
            this.Error = error;
        }

        #endregion
    }

    /// <summary>
    /// 문자열 비동기 다운로드 종료
    /// </summary>
    public class FtpDownloadStringCompletedEventArgs
    {
        #region Property

        public string Result { get; private set; }

        #endregion

        #region Constructor

        public FtpDownloadStringCompletedEventArgs(string result)
        {
            this.Result = result;
        }

        #endregion
    }

    /// <summary>
    /// 파일 리스트 업로드 시 업로드 정보 변경
    /// </summary>
    public class FtpUploadFileListChangedEventArgs
    {
        #region Property

        public int UploadTotalCount { get; private set; }
        public int UploadCount { get; private set; }
        public string UploadFileName { get; private set; }
        public string UploadPath { get; private set; }

        #endregion

        #region Constructor

        public FtpUploadFileListChangedEventArgs(int uploadTotalCount, int uploadCount, string uploadFileName, string uploadPath)
        {
            this.UploadTotalCount = uploadTotalCount;
            this.UploadCount = uploadCount;
            this.UploadFileName = uploadFileName;
            this.UploadPath = uploadPath;
        }

        #endregion
    }

    /// <summary>
    /// 폴더 다운로드 시 다운로드 정보 변경
    /// </summary>
    public class FtpDownloadDirectoryChangedEventArgs
    {
        #region Property

        public int DownloadTotalCount { get; private set; }
        public int DownloadCount { get; private set; }
        public string DownloadFileName { get; private set; }
        public string DownloadPath { get; private set; }
        public string DownloadFtpPath { get; private set; }

        #endregion

        #region Constructor

        public FtpDownloadDirectoryChangedEventArgs(int downloadTotalCount, int downloadCount, string downloadFileName, string downloadPath, string downloadFtpPath)
        {
            this.DownloadTotalCount = downloadTotalCount;
            this.DownloadCount = downloadCount;
            this.DownloadFileName = downloadFileName;
            this.DownloadPath = downloadPath;
            this.DownloadFtpPath = downloadFtpPath;
        }

        #endregion
    }
}