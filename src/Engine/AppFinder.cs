using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Media;
using Microsoft.Win32;

namespace ScrollIt.Engine
{
    public class InstalledAppInfo
    {
        public string DisplayName { get; set; }
        public string ProcessName { get; set; }
        public string ExePath { get; set; }
        public string SourceInfo { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1}.exe)", DisplayName, ProcessName);
        }
    }

    public static class AppFinder
    {
        private static readonly object _syncLock = new object();
        private static List<InstalledAppInfo> _cachedApps = new List<InstalledAppInfo>();
        private static volatile bool _isIndexed = false;
        private static volatile bool _isIndexing = false;

        public static void InitializeAsync()
        {
            if (_isIndexed || _isIndexing) return;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                }
                catch { }
                Reindex();
            });
        }

        public static void Reindex()
        {
            if (_isIndexing) return;
            _isIndexing = true;
            try
            {
                Dictionary<string, InstalledAppInfo> appDict = new Dictionary<string, InstalledAppInfo>(StringComparer.OrdinalIgnoreCase);

                // 1. Scan Currently Running Processes
                try
                {
                    foreach (Process p in Process.GetProcesses())
                    {
                        try
                        {
                            string pName = p.ProcessName.ToLowerInvariant();
                            if (string.IsNullOrEmpty(pName) || IsSystemProcess(pName)) continue;

                            string exePath = null;
                            string title = p.MainWindowTitle;
                            try
                            {
                                if (p.MainModule != null) exePath = p.MainModule.FileName;
                            }
                            catch { }

                            string dispName = !string.IsNullOrEmpty(title) ? title : FormatNiceName(pName);

                            if (!appDict.ContainsKey(pName))
                            {
                                appDict[pName] = new InstalledAppInfo
                                {
                                    ProcessName = pName,
                                    DisplayName = dispName,
                                    ExePath = exePath,
                                    SourceInfo = "En cours d'exécution"
                                };
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                // 2. Scan Registry App Paths (HKLM & HKCU)
                string[] appPathRoots = new string[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths"
                };

                foreach (string root in appPathRoots)
                {
                    ScanAppPaths(Registry.CurrentUser, root, appDict);
                    ScanAppPaths(Registry.LocalMachine, root, appDict);
                }

                // 3. Scan Registry Installed Programs (Uninstall Keys)
                string[] uninstallRoots = new string[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (string root in uninstallRoots)
                {
                    ScanUninstallPrograms(Registry.CurrentUser, root, appDict);
                    ScanUninstallPrograms(Registry.LocalMachine, root, appDict);
                }

                // 4. Scan Common Game Launchers (Riot Games, Steam, Epic, Ubisoft, Battle.net)
                ScanKnownGamingFolders(appDict);

                // 5. Scan Start Menu Shortcuts (.lnk files)
                try
                {
                    string commonProgs = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
                    string userProgs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

                    ScanStartMenuFolder(commonProgs, appDict);
                    ScanStartMenuFolder(userProgs, appDict);
                }
                catch { }

                List<InstalledAppInfo> resultList = new List<InstalledAppInfo>(appDict.Values);
                resultList.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

                lock (_syncLock)
                {
                    _cachedApps = resultList;
                    _isIndexed = true;
                }
            }
            catch { }
            finally
            {
                _isIndexing = false;
            }
        }

        public static List<InstalledAppInfo> Search(string query, int maxResults = 12)
        {
            if (string.IsNullOrEmpty(query)) return new List<InstalledAppInfo>();

            query = query.Trim().ToLowerInvariant().Replace(".exe", "");

            List<InstalledAppInfo> snapshot;
            lock (_syncLock)
            {
                snapshot = new List<InstalledAppInfo>(_cachedApps);
            }

            if (snapshot.Count == 0)
            {
                InitializeAsync();
                return GetQuickRunningMatches(query, maxResults);
            }

            List<InstalledAppInfo> exactMatches = new List<InstalledAppInfo>();
            List<InstalledAppInfo> startsWithMatches = new List<InstalledAppInfo>();
            List<InstalledAppInfo> containsMatches = new List<InstalledAppInfo>();

            HashSet<string> seenProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < snapshot.Count; i++)
            {
                InstalledAppInfo app = snapshot[i];
                if (seenProcesses.Contains(app.ProcessName)) continue;

                string dispLower = (app.DisplayName != null) ? app.DisplayName.ToLowerInvariant() : "";
                string procLower = (app.ProcessName != null) ? app.ProcessName.ToLowerInvariant() : "";

                if (dispLower.Equals(query, StringComparison.OrdinalIgnoreCase) || procLower.Equals(query, StringComparison.OrdinalIgnoreCase))
                {
                    exactMatches.Add(app);
                    seenProcesses.Add(app.ProcessName);
                }
                else if (dispLower.StartsWith(query, StringComparison.OrdinalIgnoreCase) || procLower.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                {
                    startsWithMatches.Add(app);
                    seenProcesses.Add(app.ProcessName);
                }
                else if (dispLower.Contains(query) || procLower.Contains(query))
                {
                    containsMatches.Add(app);
                    seenProcesses.Add(app.ProcessName);
                }
            }

            List<InstalledAppInfo> results = new List<InstalledAppInfo>();
            results.AddRange(exactMatches);
            results.AddRange(startsWithMatches);
            results.AddRange(containsMatches);

            if (results.Count > maxResults)
            {
                results = results.GetRange(0, maxResults);
            }

            return results;
        }

        public static List<InstalledAppInfo> GetAllApps()
        {
            lock (_syncLock)
            {
                return new List<InstalledAppInfo>(_cachedApps);
            }
        }

        private static void ScanAppPaths(RegistryKey baseKey, string subKeyPath, Dictionary<string, InstalledAppInfo> dict)
        {
            try
            {
                using (RegistryKey key = baseKey.OpenSubKey(subKeyPath))
                {
                    if (key == null) return;
                    foreach (string subName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey appKey = key.OpenSubKey(subName))
                            {
                                if (appKey == null) continue;
                                string path = appKey.GetValue(null) as string;
                                if (!string.IsNullOrEmpty(path))
                                {
                                    path = path.Trim('\"');
                                    if (File.Exists(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string pName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                                        if (IsSystemProcess(pName)) continue;

                                        string dispName = FormatNiceName(pName);
                                        try
                                        {
                                            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(path);
                                            if (!string.IsNullOrEmpty(fvi.FileDescription))
                                            {
                                                dispName = fvi.FileDescription;
                                            }
                                            else if (!string.IsNullOrEmpty(fvi.ProductName))
                                            {
                                                dispName = fvi.ProductName;
                                            }
                                        }
                                        catch { }

                                        if (!dict.ContainsKey(pName))
                                        {
                                            dict[pName] = new InstalledAppInfo
                                            {
                                                ProcessName = pName,
                                                DisplayName = dispName,
                                                ExePath = path,
                                                SourceInfo = "Application installée"
                                            };
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static void ScanUninstallPrograms(RegistryKey baseKey, string subKeyPath, Dictionary<string, InstalledAppInfo> dict)
        {
            try
            {
                using (RegistryKey key = baseKey.OpenSubKey(subKeyPath))
                {
                    if (key == null) return;
                    foreach (string subName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey progKey = key.OpenSubKey(subName))
                            {
                                if (progKey == null) continue;

                                string dispName = progKey.GetValue("DisplayName") as string;
                                if (string.IsNullOrEmpty(dispName)) continue;

                                string iconPath = progKey.GetValue("DisplayIcon") as string;
                                string installLoc = progKey.GetValue("InstallLocation") as string;

                                string exePath = null;
                                if (!string.IsNullOrEmpty(iconPath))
                                {
                                    string candidate = iconPath.Split(',')[0].Trim('\"');
                                    if (File.Exists(candidate) && candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                    {
                                        exePath = candidate;
                                    }
                                }

                                if (string.IsNullOrEmpty(exePath) && !string.IsNullOrEmpty(installLoc) && Directory.Exists(installLoc))
                                {
                                    try
                                    {
                                        string[] exes = Directory.GetFiles(installLoc, "*.exe", SearchOption.TopDirectoryOnly);
                                        if (exes != null && exes.Length > 0)
                                        {
                                            exePath = exes[0];
                                        }
                                    }
                                    catch { }
                                }

                                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                                {
                                    string pName = Path.GetFileNameWithoutExtension(exePath).ToLowerInvariant();
                                    if (IsSystemProcess(pName)) continue;

                                    if (!dict.ContainsKey(pName))
                                    {
                                        dict[pName] = new InstalledAppInfo
                                        {
                                            ProcessName = pName,
                                            DisplayName = dispName,
                                            ExePath = exePath,
                                            SourceInfo = "Programme Windows"
                                        };
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static void ScanKnownGamingFolders(Dictionary<string, InstalledAppInfo> dict)
        {
            // Riot Games (League of Legends, Valorant, Riot Client)
            string[] riotCandidates = new string[]
            {
                @"C:\Riot Games\League of Legends\LeagueClient.exe",
                @"C:\Riot Games\League of Legends\Game\League of Legends.exe",
                @"C:\Riot Games\VALORANT\live\VALORANT.exe",
                @"C:\Riot Games\Riot Client\RiotClientServices.exe",
                @"D:\Riot Games\League of Legends\LeagueClient.exe",
                @"D:\Riot Games\League of Legends\Game\League of Legends.exe",
                @"D:\Riot Games\VALORANT\live\VALORANT.exe",
                @"E:\Riot Games\League of Legends\LeagueClient.exe"
            };

            foreach (string rPath in riotCandidates)
            {
                try
                {
                    if (File.Exists(rPath))
                    {
                        string pName = Path.GetFileNameWithoutExtension(rPath).ToLowerInvariant();
                        string disp = (pName == "leagueclient" || pName == "league of legends") ? "League of Legends" :
                                      (pName == "valorant") ? "VALORANT" : FormatNiceName(pName);

                        dict[pName] = new InstalledAppInfo
                        {
                            ProcessName = pName,
                            DisplayName = disp,
                            ExePath = rPath,
                            SourceInfo = "Jeu / Riot Games"
                        };
                    }
                }
                catch { }
            }

            // Steam Library Games
            try
            {
                string[] steamRoots = new string[]
                {
                    @"C:\Program Files (x86)\Steam\steamapps\common",
                    @"C:\Program Files\Steam\steamapps\common",
                    @"D:\SteamLibrary\steamapps\common",
                    @"D:\Steam\steamapps\common",
                    @"E:\SteamLibrary\steamapps\common",
                    @"E:\Steam\steamapps\common"
                };

                foreach (string sRoot in steamRoots)
                {
                    if (Directory.Exists(sRoot))
                    {
                        foreach (string gameDir in Directory.GetDirectories(sRoot))
                        {
                            try
                            {
                                string folderName = Path.GetFileName(gameDir);
                                string[] exes = Directory.GetFiles(gameDir, "*.exe", SearchOption.TopDirectoryOnly);
                                foreach (string exe in exes)
                                {
                                    string pName = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                                    if (pName.Contains("crash") || pName.Contains("unity") || pName.Contains("unins") || pName.Contains("redist") || pName.Contains("setup")) continue;

                                    if (!dict.ContainsKey(pName))
                                    {
                                        dict[pName] = new InstalledAppInfo
                                        {
                                            ProcessName = pName,
                                            DisplayName = folderName,
                                            ExePath = exe,
                                            SourceInfo = "Jeu Steam"
                                        };
                                        break;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private static void ScanStartMenuFolder(string folderPath, Dictionary<string, InstalledAppInfo> dict)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

            try
            {
                string[] lnkFiles = Directory.GetFiles(folderPath, "*.lnk", SearchOption.AllDirectories);
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = (shellType != null) ? Activator.CreateInstance(shellType) : null;

                foreach (string lnk in lnkFiles)
                {
                    try
                    {
                        string shortcutName = Path.GetFileNameWithoutExtension(lnk);
                        if (shortcutName.ToLowerInvariant().Contains("uninstall") || shortcutName.ToLowerInvariant().Contains("désinstaller")) continue;

                        string targetPath = null;
                        if (shell != null)
                        {
                            try
                            {
                                dynamic shortcut = shell.CreateShortcut(lnk);
                                targetPath = (string)shortcut.TargetPath;
                            }
                            catch { }
                        }

                        if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath) && targetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            string pName = Path.GetFileNameWithoutExtension(targetPath).ToLowerInvariant();
                            if (IsSystemProcess(pName)) continue;

                            if (!dict.ContainsKey(pName))
                            {
                                dict[pName] = new InstalledAppInfo
                                {
                                    ProcessName = pName,
                                    DisplayName = shortcutName,
                                    ExePath = targetPath,
                                    SourceInfo = "Raccourci Windows"
                                };
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static bool IsSystemProcess(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            return name == "svchost" || name == "dwm" || name == "explorer" || name == "system" ||
                   name == "idle" || name == "smss" || name == "csrss" || name == "wininit" ||
                   name == "services" || name == "lsass" || name == "winlogon" || name == "fontdrvhost" ||
                   name == "sihost" || name == "taskhostw" || name == "ctfmon" || name == "shellexperiencehost" ||
                   name == "searchui" || name == "searchapp" || name == "searchhost" || name == "runtimebroker" ||
                   name == "lockapp" || name == "applicationframehost" || name == "scroll-it" ||
                   name == "scroll-it-portable" || name == "scroll-it-setup";
        }

        private static string FormatNiceName(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return "";
            if (processName.Length <= 3) return processName.ToUpperInvariant();
            return char.ToUpperInvariant(processName[0]) + processName.Substring(1);
        }

        private static List<InstalledAppInfo> GetQuickRunningMatches(string query, int maxResults)
        {
            List<InstalledAppInfo> matches = new List<InstalledAppInfo>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        string pName = p.ProcessName.ToLowerInvariant();
                        if (string.IsNullOrEmpty(pName) || IsSystemProcess(pName) || seen.Contains(pName)) continue;

                        string title = p.MainWindowTitle;
                        string disp = !string.IsNullOrEmpty(title) ? title : FormatNiceName(pName);

                        if (disp.ToLowerInvariant().Contains(query) || pName.Contains(query))
                        {
                            seen.Add(pName);
                            matches.Add(new InstalledAppInfo
                            {
                                ProcessName = pName,
                                DisplayName = disp,
                                SourceInfo = "En cours d'exécution"
                            });
                            if (matches.Count >= maxResults) break;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return matches;
        }
    }
}
