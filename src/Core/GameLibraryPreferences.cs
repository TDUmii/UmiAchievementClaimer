using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UmiAchievementClaimer
{
    // Only Steam AppIDs are remembered locally. Never stores account details or absolute paths.
    internal static class GameLibraryPreferences
    {
        private static readonly HashSet<string> favorites = new HashSet<string>(StringComparer.Ordinal);
        private static readonly List<string> recent = new List<string>();
        private static bool loaded;

        private static string FilePath
        {
            get
            {
                // Test harness provides an AppDomain-scoped temporary file; production uses AppData.
                string testFile = AppDomain.CurrentDomain.GetData("UmiAchievementClaimer.TestLibraryPath") as string;
                return !string.IsNullOrEmpty(testFile) ? testFile :
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "UmiAchievementClaimer", "library-history.txt");
            }
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    if (line.Length < 3) continue;
                    string id = line.Substring(2).Trim();
                    if (!ValidId(id)) continue;
                    if (line.StartsWith("F:", StringComparison.Ordinal)) favorites.Add(id);
                    if (line.StartsWith("R:", StringComparison.Ordinal) && !recent.Contains(id)) recent.Add(id);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static bool ValidId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.Length <= 20 && id.All(char.IsDigit);
        }

        internal static bool IsFavorite(string appId)
        {
            EnsureLoaded();
            return appId != null && favorites.Contains(appId);
        }

        internal static int RecentRank(string appId)
        {
            EnsureLoaded();
            int index = recent.IndexOf(appId ?? string.Empty);
            return index < 0 ? int.MaxValue : index;
        }

        internal static void ToggleFavorite(string appId)
        {
            EnsureLoaded();
            if (!ValidId(appId)) return;
            if (!favorites.Add(appId)) favorites.Remove(appId);
            Save();
        }

        internal static void MarkRecent(string appId)
        {
            EnsureLoaded();
            if (!ValidId(appId)) return;
            recent.Remove(appId);
            recent.Insert(0, appId);
            if (recent.Count > 20) recent.RemoveRange(20, recent.Count - 20);
            Save();
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                string[] entries = favorites.OrderBy(id => id, StringComparer.Ordinal).Select(id => "F:" + id)
                    .Concat(recent.Select(id => "R:" + id)).ToArray();
                string temp = FilePath + ".tmp";
                File.WriteAllLines(temp, entries);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temp, FilePath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
