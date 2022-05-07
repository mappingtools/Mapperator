using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Mapperator.ConsoleApp.Resources;

namespace Mapperator.ConsoleApp {
    public static class ConfigManager {
        private static readonly JsonSerializer Serializer = new() {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        public static Config Config { get; private set; } = new();

        public static void LoadConfig() {
            if (File.Exists(Constants.ConfigPath)) {
                LoadFromJson();
            } else {
                DefaultPaths();
                CreateJson();
            }
        }

        private static void LoadFromJson() {
            try {
                using var sr = new StreamReader(Constants.ConfigPath);
                using var reader = new JsonTextReader(sr);
                Config = Serializer.Deserialize<Config>(reader) ?? Config;
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        private static void CreateJson() {
            try {
                using var sw = new StreamWriter(Constants.ConfigPath);
                using var writer = new JsonTextWriter(sw);
                Serializer.Serialize(writer, Config);
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        private static void DefaultPaths() {
            if (string.IsNullOrWhiteSpace(Config.OsuPath)) {
                Config.OsuPath = TryFindOsuPath(out var osuPath) ? osuPath : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");
            }

            if (!string.IsNullOrWhiteSpace(Config.SongsPath)) return;
            var beatmapDirectory = GetBeatmapDirectory(Path.Combine(Config.OsuPath, $"osu!.{Environment.UserName}.cfg"));
            Config.SongsPath = Path.Combine(Config.OsuPath, beatmapDirectory);
        }

        private static bool TryFindOsuPath(out string osuPath) {
            osuPath = string.Empty;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;

            try {
                var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (regKey is null) return false;
                osuPath = FindByDisplayName(regKey, "osu!");
                return true;
            } catch (KeyNotFoundException) {
                try {
#pragma warning disable CA1416
                    var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
#pragma warning restore CA1416
                    if (regKey is null) return false;
                    osuPath = FindByDisplayName(regKey, "osu!");
                    return true;
                }
                catch (KeyNotFoundException) {
                    return false;
                }
            }
        }

        private static string FindByDisplayName(RegistryKey parentKey, string name) {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new InvalidOperationException(Strings.WindowsOnlyOperation);

            var nameList = parentKey.GetSubKeyNames();
            foreach (var t in nameList) {
                var regKey = parentKey.OpenSubKey(t);
                var displayName = regKey?.GetValue("DisplayName");
                var uninstallString = regKey?.GetValue("UninstallString");

                if (displayName is not null &&
                    displayName.ToString() == name &&
                    uninstallString is not null) {
                    return Path.GetDirectoryName(uninstallString.ToString())!;
                }
            }

            throw new KeyNotFoundException($"Could not find registry key with display name \"{name}\".");
        }

        private static string GetBeatmapDirectory(string configPath) {
            try {
                foreach (var line in File.ReadLines(configPath)) {
                    var split = line.Split('=');
                    if (split[0].Trim() == "BeatmapDirectory") {
                        return split[1].Trim();
                    }
                }
            } catch (Exception exception) {
                Console.WriteLine(exception);
            }

            return "Songs";
        }
    }
}
