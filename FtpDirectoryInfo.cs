using System;
using System.Text.RegularExpressions;

namespace FTP
{
    /// <summary>
    /// ftp 디렉토리의 실제 정보
    /// 
    /// 정규식 파싱 참고 소스 : https://bpm2.svn.codeplex.com/svn/Workflows.Components.FileManipulation/FtpClient.cs
    /// </summary>
    public class FtpDirectoryInfo
    {
        #region Variable

        private static string[] _patterns = new string[] {
            "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\w+\\s+\\w+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{4})\\s+(?<name>.+)", 
            "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\d+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{4})\\s+(?<name>.+)", 
            "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\d+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{1,2}:\\d{2})\\s+(?<name>.+)", 
            "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\w+\\s+\\w+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{1,2}:\\d{2})\\s+(?<name>.+)", 
            "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})(\\s+)(?<size>(\\d+))(\\s+)(?<ctbit>(\\w+\\s\\w+))(\\s+)(?<size2>(\\d+))\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{2}:\\d{2})\\s+(?<name>.+)",
            "(?<timestamp>\\d{2}\\-\\d{2}\\-\\d{2}\\s+\\d{2}:\\d{2}[Aa|Pp][mM])\\s+(?<dir>\\<\\w+\\>){0,1}(?<size>\\d+){0,1}\\s+(?<name>.+)",
            "(?<size>(\\d+))\\s+\\d+\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{1,2}:\\d{2})\\s+(?<name>.+)"};

        #endregion

        #region Property

        public bool IsDirectory { get; private set; }
        public string Name { get; private set; }
        public string DirectoryPath { get; private set; }
        public string Permission { get; private set; }
        public long Size { get; private set; }
        public DateTime Time { get; private set; }

        public string FullPath
        {
            get
            {
                return string.IsNullOrWhiteSpace(DirectoryPath) ?
                    this.Name : string.Format("{0}/{1}", this.DirectoryPath, this.Name);
            }
        }

        #endregion

        #region Private Constructor

        private FtpDirectoryInfo(Match match, string dirPath)
        {
            this.DirectoryPath = dirPath;
            this.Name = match.Groups["name"].Value;

            Int64 size;
            Int64.TryParse(match.Groups["size"].Value, out size);
            this.Size = size;

            this.Permission = match.Groups["permission"].Value;

            string dir = match.Groups["dir"].Value;
            this.IsDirectory = !dir.Equals("") && !dir.Equals("-");

            string timestamp = match.Groups["timestamp"].Value;

            try
            {
                Regex dtRegex = new Regex("([A-Za-z][A-Za-z][A-Za-z])\\s+(\\d+)\\s+(\\d+):(\\d+)", RegexOptions.IgnoreCase);
                Regex dtRegex2 = new Regex("([A-Za-z][A-Za-z][A-Za-z])\\s+(\\d+)\\s+(\\d\\d\\d\\d)\\s+(\\d+):(\\d+)", RegexOptions.IgnoreCase);
                Regex dtRegex3 = new Regex("([0-9]{2})-([0-9]{2})-([0-9]{2})\\s+([0-9]{2}):([0-9]{2})([A-Za-z]{2})");

                Match dtMatch1 = dtRegex.Match(timestamp);
                Match dtMatch2 = dtRegex2.Match(timestamp);
                Match dtMatch3 = dtRegex3.Match(timestamp);

                if (dtMatch1.Success)
                {
                    int year = DateTime.Now.Year;
                    int month = MonthStringConverter(dtMatch1.Groups[1].Value);
                    int day = int.Parse(dtMatch1.Groups[2].Value);
                    int hour = int.Parse(dtMatch1.Groups[3].Value);
                    int minute = int.Parse(dtMatch1.Groups[4].Value);
                    this.Time = new DateTime(year, month, day, hour, minute, 0);
                }
                else if (dtMatch2.Success)
                {
                    int year = int.Parse(dtMatch2.Groups[3].Value);
                    int month = MonthStringConverter(dtMatch2.Groups[1].Value);
                    int day = int.Parse(dtMatch2.Groups[2].Value);
                    int hour = int.Parse(dtMatch2.Groups[4].Value);
                    int minute = int.Parse(dtMatch2.Groups[5].Value);
                    this.Time = new DateTime(year, month, day, hour, minute, 0);
                }
                else if (dtMatch3.Success)
                {
                    int year = 2000 + int.Parse(dtMatch3.Groups[3].Value);
                    int month = int.Parse(dtMatch3.Groups[1].Value);
                    int day = int.Parse(dtMatch3.Groups[2].Value);
                    int hour = int.Parse(dtMatch3.Groups[4].Value);
                    int minutes = int.Parse(dtMatch3.Groups[5].Value);
                    string ampm = dtMatch3.Groups[6].Value;

                    hour += ampm.Equals("PM") && hour != 12 ? 12 : 0;

                    this.Time = new DateTime(year, month, day, hour, minutes, 0);
                }
                else
                {
                    this.Time = DateTime.Parse(timestamp);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                this.Time = Convert.ToDateTime(null);
            }
        }

        #endregion

        #region Private Method

        private int MonthStringConverter(string month)
        {
            switch (month.ToUpper())
            {
                case "JAN": return 1;
                case "FEB": return 2;
                case "MAR": return 3;
                case "APR": return 4;
                case "MAY": return 5;
                case "JUN": return 6;
                case "JUL": return 7;
                case "AUG": return 8;
                case "SEP": return 9;
                case "OCT": return 10;
                case "NOV": return 11;
                case "DEC": return 12;
                default: return DateTime.Now.Month;
            }
        }

        #endregion

        #region Static Method

        public static FtpDirectoryInfo Create(string lineStr, string dirPath, bool isWindows = true)
        {
            foreach (var pattern in _patterns)
            {
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                Match match;

                match = regex.Match(lineStr);

                if (match.Success)
                {
                    return new FtpDirectoryInfo(match, dirPath);
                }
            }

            return null;
        }

        #endregion
    }
}