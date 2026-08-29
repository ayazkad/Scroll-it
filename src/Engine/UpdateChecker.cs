using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ScrollIt.Engine
{
    public class UpdateInfo
    {
        public bool IsSuccess { get; set; }
        public bool HasUpdate { get; set; }
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public string ReleaseUrl { get; set; }
        public string ReleaseNotes { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class UpdateChecker
    {
        public const string CurrentVersion = "1.1.0";
        public const string RepositoryOwner = "ayazkad";
        public const string RepositoryName = "Scroll-it";
        private const string ApiUrl = "https://api.github.com/repos/ayazkad/Scroll-it/releases/latest";
        public const string DefaultReleasesPage = "https://github.com/ayazkad/Scroll-it/releases";

        public static void CheckForUpdatesAsync(Action<UpdateInfo> onCompleted)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                UpdateInfo result = CheckForUpdates();
                if (onCompleted != null)
                {
                    onCompleted(result);
                }
            });
        }

        public static UpdateInfo CheckForUpdates()
        {
            UpdateInfo info = new UpdateInfo
            {
                CurrentVersion = CurrentVersion,
                ReleaseUrl = DefaultReleasesPage
            };

            try
            {
                // Force TLS 1.2 for modern GitHub API requirements
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiUrl);
                request.Method = "GET";
                request.UserAgent = "Scroll-it-Client/" + CurrentVersion;
                request.Accept = "application/vnd.github.v3+json";
                request.Timeout = 10000;
                request.ReadWriteTimeout = 10000;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();

                    // Parse tag_name (e.g. "tag_name": "v1.0.2" or "1.0.2")
                    Match tagMatch = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                    if (tagMatch.Success)
                    {
                        info.LatestVersion = tagMatch.Groups[1].Value.Trim().TrimStart('v', 'V');
                    }

                    // Parse html_url
                    Match urlMatch = Regex.Match(json, "\"html_url\"\\s*:\\s*\"([^\"]+)\"");
                    if (urlMatch.Success)
                    {
                        info.ReleaseUrl = urlMatch.Groups[1].Value.Trim().Replace("\\/", "/");
                    }

                    // Parse body / release notes
                    Match bodyMatch = Regex.Match(json, "\"body\"\\s*:\\s*\"([^\"]+)\"");
                    if (bodyMatch.Success)
                    {
                        info.ReleaseNotes = Regex.Unescape(bodyMatch.Groups[1].Value);
                    }

                    if (!string.IsNullOrEmpty(info.LatestVersion))
                    {
                        info.HasUpdate = IsNewerVersion(info.LatestVersion, CurrentVersion);
                        info.IsSuccess = true;
                    }
                    else
                    {
                        info.IsSuccess = false;
                        info.ErrorMessage = "Unable to parse release tag";
                    }
                }
            }
            catch (Exception ex)
            {
                info.IsSuccess = false;
                info.ErrorMessage = ex.Message;
            }

            return info;
        }

        public static bool IsNewerVersion(string latestStr, string currentStr)
        {
            try
            {
                Version latest = ParseVersion(latestStr);
                Version current = ParseVersion(currentStr);
                return latest > current;
            }
            catch
            {
                return string.Compare(latestStr, currentStr, StringComparison.OrdinalIgnoreCase) > 0;
            }
        }

        private static Version ParseVersion(string vStr)
        {
            if (string.IsNullOrEmpty(vStr)) return new Version(0, 0, 0);
            vStr = vStr.Trim().TrimStart('v', 'V');

            string[] parts = vStr.Split(new[] { '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            int mj = 0;
            int mn = 0;
            int b = 0;
            if (parts.Length > 0) int.TryParse(parts[0], out mj);
            if (parts.Length > 1) int.TryParse(parts[1], out mn);
            if (parts.Length > 2) int.TryParse(parts[2], out b);

            return new Version(mj, mn, b);
        }
    }
}
