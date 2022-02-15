using System;
using System.IO;
using System.Net;

namespace FTP
{
    /// <summary>
    /// ftp 서버에 접속하기 위한 계정 정보 클래스
    /// </summary>
    public class FtpConnection
    {
        #region Property

        #region HostName

        private string hostName;
        public string HostName
        {
            get
            {
                return hostName.StartsWith("ftp://") ?
                    this.hostName :
                    string.Format("ftp://{0}", this.hostName);
            }
            set
            {
                this.hostName = value;
            }
        }

        #endregion

        #region UserName

        public string UserName { get; set; }

        #endregion

        #region Password

        public string Password { get; set; }

        #endregion

        #endregion

        #region Constructor

        public FtpConnection(string hostName) :
            this(hostName, "anonymous", "")
        {
        }

        public FtpConnection(string hostName, string userName, string password)
        {
            this.HostName = hostName;
            this.UserName = userName;
            this.Password = password;
        }

        #endregion

        #region Public Method

        public bool IsConnected()
        {
            var request = WebRequest.Create(this.HostName) as FtpWebRequest;
            request.Credentials = GetCredential();
            request.Method = WebRequestMethods.Ftp.PrintWorkingDirectory;
            
            return GetResponseString(request) != null;
        }

        public Uri GetFtpUri(string ftpPath)
        {
            return new Uri(string.Format("{0}//{1}", this.HostName, ftpPath));
        }

        public ICredentials GetCredential()
        {
            return new NetworkCredential(this.UserName, this.Password);
        }

        public FtpWebRequest GetRequest(string ftpPath, string method)
        {
            Uri ftpUri = GetFtpUri(ftpPath);
            var request = WebRequest.Create(ftpUri) as FtpWebRequest;
            request.Method = method;
            request.Credentials = GetCredential();
            request.KeepAlive = false;
            return request;
        }

        public FtpWebResponse GetResponse(FtpWebRequest request)
        {
            return request.GetResponse() as FtpWebResponse;
        }

        public string GetResponseString(FtpWebRequest request)
        {
            try
            {
                string str;

                using (FtpWebResponse response = GetResponse(request))
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            str = reader.ReadToEnd();
                            reader.Close();
                        }

                        stream.Close();
                    }

                    response.Close();
                }

                return str;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return null;
            }
        }

        #endregion
    }
}