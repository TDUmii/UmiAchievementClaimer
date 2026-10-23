using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UmiAchievementClaimer
{
    internal sealed class SteamInstalledGame
    {
        public string Name;
        public string AppId;
        public string Folder;
        public override string ToString() { return Name; }
    }

    // Read-only inventory. Enumerates Steam's own library manifests; never scans game DLLs.
    internal static class SteamLibraryCatalog
    {
        internal static List<SteamInstalledGame> Discover()
        {
            return FromLibraries(SteamGameDetector.FindSteamLibraries(string.Empty));
        }

        internal static List<SteamInstalledGame> FromLibraries(IEnumerable<string> libraries)
        {
            Dictionary<string, SteamInstalledGame> games = new Dictionary<string, SteamInstalledGame>(StringComparer.OrdinalIgnoreCase);
            foreach (string library in libraries ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(library)) continue;
                string apps = Path.Combine(library, "steamapps");
                string common = Path.Combine(apps, "common");
                if (!Directory.Exists(apps) || !Directory.Exists(common)) continue;
                string[] manifests;
                try { manifests = Directory.GetFiles(apps, "appmanifest_*.acf", SearchOption.TopDirectoryOnly); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                foreach (string manifest in manifests)
                {
                    try
                    {
                        if (new FileInfo(manifest).Length > 2 * 1024 * 1024) continue;
                        string content = File.ReadAllText(manifest);
                        string appId = Value(content, "appid");
                        string dir = Value(content, "installdir");
                        if (!Regex.IsMatch(appId, @"^\d+$") || string.IsNullOrWhiteSpace(dir)) continue;
                        string root = Path.GetFullPath(Path.Combine(common, dir));
                        string commonRoot = Path.GetFullPath(common).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                        if (!root.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(root)) continue;
                        string name = Value(content, "name");
                        if (string.IsNullOrWhiteSpace(name)) name = Path.GetFileName(root);
                        games[root] = new SteamInstalledGame { Name = name, AppId = appId, Folder = root };
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                    catch (ArgumentException) { }
                    catch (NotSupportedException) { }
                }
            }
            return games.Values.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.AppId, StringComparer.Ordinal).ToList();
        }

        private static string Value(string content, string key)
        {
            Match match = Regex.Match(content, "\\\"" + Regex.Escape(key) + "\\\"\\s+\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }
}
