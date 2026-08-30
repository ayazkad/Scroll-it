using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Win32;

namespace ScrollIt.Engine
{
    [DataContract]
    public class ScrollPreset
    {
        [DataMember] public string Name { get; set; }
        [DataMember] public string Description { get; set; }
        [DataMember] public double StepSize { get; set; }
        [DataMember] public double AnimationTime { get; set; }
        [DataMember] public double AccelerationMultiplier { get; set; }
        [DataMember] public double FrictionTail { get; set; }

        public ScrollPreset() { }

        public ScrollPreset(string name, string description, double stepSize, double animTime, double accel, double friction)
        {
            Name = name;
            Description = description;
            StepSize = stepSize;
            AnimationTime = animTime;
            AccelerationMultiplier = accel;
            FrictionTail = friction;
        }
    }

    [DataContract]
    public class AppSettings
    {
        [DataMember] public bool Enabled { get; set; }
        [DataMember] public string ActivePreset { get; set; }
        [DataMember] public double StepSize { get; set; }
        [DataMember] public double AnimationTime { get; set; }
        [DataMember] public double AccelerationMultiplier { get; set; }
        [DataMember] public double FrictionTail { get; set; }
        [DataMember] public bool StartWithWindows { get; set; }
        [DataMember] public bool MinimizeToTrayOnClose { get; set; }
        [DataMember] public bool BypassCtrlZoom { get; set; }
        [DataMember] public bool ReverseDirection { get; set; }
        [DataMember] public string Language { get; set; }
        [DataMember] public string AccentColor { get; set; }
        [DataMember] public string BackdropStyle { get; set; }
        [DataMember] public List<string> BlacklistedApps { get; set; }

        public AppSettings()
        {
            Enabled = true;
            ActivePreset = "Mac OS";
            StepSize = 120.0;
            AnimationTime = 400.0;
            AccelerationMultiplier = 1.4;
            FrictionTail = 0.95;
            StartWithWindows = false;
            MinimizeToTrayOnClose = true;
            BypassCtrlZoom = true;
            ReverseDirection = false;
            Language = "auto";
            AccentColor = "Cyan";
            BackdropStyle = "Mica";
            BlacklistedApps = new List<string>();
        }
    }

    public static class SettingsManager
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "scroll-it"
        );

        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");

        public static AppSettings Current { get; private set; }

        public static readonly Dictionary<string, ScrollPreset> Presets = new Dictionary<string, ScrollPreset>
        {
            {
                "Mac OS",
                new ScrollPreset("Mac OS", "Inertie fluide type macOS, accélération progressive et amorti soyeux.", 120, 400, 1.4, 0.95)
            },
            {
                "Snappy",
                new ScrollPreset("Snappy", "Réponse vive et précise avec un amorti court, idéal pour travailler rapidement.", 120, 160, 1.2, 0.50)
            },
            {
                "Cinematic Glide",
                new ScrollPreset("Cinematic Glide", "Glisse ultra-douce et allongée, idéale pour la lecture d'articles et de flux.", 140, 650, 2.5, 0.88)
            },
            {
                "Ultra Smooth",
                new ScrollPreset("Ultra Smooth", "Défilement soyeux et équilibré avec accélération fluide sur plusieurs crans.", 120, 500, 2.0, 0.85)
            }
        };

        public static event Action SettingsChanged;

        static SettingsManager()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                if (File.Exists(SettingsFilePath))
                {
                    using (FileStream stream = File.OpenRead(SettingsFilePath))
                    {
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppSettings));
                        Current = (AppSettings)serializer.ReadObject(stream);
                    }
                }
            }
            catch
            {
                Current = null;
            }

            if (Current == null)
            {
                Current = new AppSettings();
                Save();
            }
            else if (Current.StepSize == 80.0 && (Current.ActivePreset == "Mac OS" || string.IsNullOrEmpty(Current.ActivePreset)))
            {
                Current.StepSize = 120.0;
                Save();
            }

            if (string.IsNullOrEmpty(Current.Language))
            {
                Current.Language = "auto";
            }
            if (string.IsNullOrEmpty(Current.AccentColor))
            {
                Current.AccentColor = "Cyan";
            }
            if (string.IsNullOrEmpty(Current.BackdropStyle))
            {
                Current.BackdropStyle = "Mica";
            }
            I18n.SetLanguageByCode(Current.Language);

            // Sync Windows startup registry state
            Current.StartWithWindows = IsAutoStartEnabled();
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                using (FileStream stream = File.Create(SettingsFilePath))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppSettings));
                    serializer.WriteObject(stream, Current);
                }
            }
            catch
            {
                // Ignore filesystem save errors
            }

            if (SettingsChanged != null)
            {
                SettingsChanged();
            }
        }

        public static void ApplyPreset(string presetName)
        {
            if (Presets.ContainsKey(presetName))
            {
                ScrollPreset preset = Presets[presetName];
                Current.ActivePreset = presetName;
                Current.StepSize = preset.StepSize;
                Current.AnimationTime = preset.AnimationTime;
                Current.AccelerationMultiplier = preset.AccelerationMultiplier;
                Current.FrictionTail = preset.FrictionTail;
                Save();
            }
        }

        public static void SetAutoStart(bool enable)
        {
            Current.StartWithWindows = enable;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        string exePath = Assembly.GetExecutingAssembly().Location;
                        if (enable)
                        {
                            key.SetValue("scroll-it", "\"" + exePath + "\" --minimized");
                        }
                        else
                        {
                            if (key.GetValue("scroll-it") != null)
                            {
                                key.DeleteValue("scroll-it", false);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Registry write permission handling
            }
            Save();
        }

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null)
                    {
                        return key.GetValue("scroll-it") != null;
                    }
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
