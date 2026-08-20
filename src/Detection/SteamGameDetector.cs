using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UmiAchievementClaimer
{
internal static class SteamGameDetector
{
    private sealed class ArchivedSteamApi
    {
        public string ArchivePath;
        public string EntryName;
        public ushort Machine;
        public string InterfaceVersion;
        public string Format;
        public long Offset;
        public long Length;
    }

    private static string M(bool english, string vietnamese, string englishText)
    {
        return english ? englishText : vietnamese;
    }

    public static DetectedGame Detect(string selectedPath, bool english)
    {
        if (string.IsNullOrWhiteSpace(selectedPath) || !Directory.Exists(selectedPath))
            throw new InvalidOperationException(M(english, "Thư mục đã chọn không tồn tại.", "The selected folder does not exist."));

        string selected = Normalize(selectedPath);
        DetectedGame game = FindManifestGame(selected);
        if (game == null)
        {
            game = new DetectedGame
            {
                RootDirectory = selected,
                Name = new DirectoryInfo(selected).Name,
                AppId = ReadNearbyAppId(selected)
            };
            if (string.IsNullOrEmpty(game.AppId))
                throw new InvalidOperationException(M(english, "Không tìm thấy appmanifest hoặc steam_appid.txt cho thư mục này.", "No appmanifest or steam_appid.txt was found for this folder."));
        }

        string[] dlls;
        try
        {
            dlls = Directory.GetFiles(game.RootDirectory, "steam_api*.dll", SearchOption.AllDirectories)
                .Where(path => string.Equals(Path.GetFileName(path), "steam_api.dll", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFileName(path), "steam_api64.dll", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path.Length)
                .ToArray();
        }
        catch (Exception error)
        {
            throw new InvalidOperationException(M(english, "Không thể quét Steam API DLL: ", "Could not scan for a Steam API DLL: ") + error.Message);
        }

        if (dlls.Length == 0)
        {
            ArchivedSteamApi archived = FindArchivedSteamApi(game.RootDirectory) ?? FindEmbeddedCabSteamApi(game.RootDirectory);
            if (archived == null)
            {
                string compatibilityProvider = FindCompatibilitySteamApi(game.RootDirectory, game.AppId);
                if (!string.IsNullOrEmpty(compatibilityProvider))
                {
                    game.SteamApiPath = compatibilityProvider;
                    game.Architecture = "x64";
                    game.InterfaceVersion = "AUTO";
                    game.UsesCompatibilityProvider = true;
                    return game;
                }
                if (string.Equals(game.AppId, "1515950", StringComparison.Ordinal))
                    throw new InvalidOperationException(M(english,
                        "Capcom Arcade Stadium cần một Steam API x64 tương thích từ thư viện Steam cục bộ, nhưng không tìm thấy provider phù hợp. Hãy cài hoặc cập nhật một game Steamworks x64 tiêu chuẩn rồi quét lại.",
                        "Capcom Arcade Stadium needs a compatible x64 Steam API from the local Steam library, but no suitable provider was found. Install or update a standard x64 Steamworks game, then scan again."));
                throw new InvalidOperationException(M(english,
                    "Không tìm thấy Steam API dạng file rời hoặc được nhúng trong JAR/ZIP/CAB.",
                    "No loose Steam API DLL or Steam API embedded in a JAR/ZIP/CAB archive was found."));
            }
            game.SteamApiPath = archived.ArchivePath;
            game.SteamApiArchiveEntry = archived.EntryName;
            game.SteamApiArchiveFormat = archived.Format;
            game.SteamApiArchiveOffset = archived.Offset;
            game.SteamApiArchiveLength = archived.Length;
            game.Architecture = archived.Machine == 0x8664 ? "x64" : "x86";
            game.InterfaceVersion = string.IsNullOrEmpty(archived.InterfaceVersion) ? "AUTO" : archived.InterfaceVersion;
            return game;
        }

        string preferred64 = dlls.FirstOrDefault(path => ReadMachine(path) == 0x8664);
        string preferred32 = dlls.FirstOrDefault(path => ReadMachine(path) == 0x014c);
        game.SteamApiPath = preferred64 ?? preferred32;
        if (game.SteamApiPath == null)
            throw new InvalidOperationException(M(english, "Steam API DLL không phải PE x86/x64 được hỗ trợ.", "The Steam API DLL is not a supported x86/x64 PE file."));

        ushort machine = ReadMachine(game.SteamApiPath);
        game.Architecture = machine == 0x8664 ? "x64" : "x86";
        game.InterfaceVersion = FindUserStatsVersion(game.SteamApiPath, game.RootDirectory);
        if (string.IsNullOrEmpty(game.InterfaceVersion))
            game.InterfaceVersion = "AUTO";
        return game;
    }

    private static string FindCompatibilitySteamApi(string selected, string appId)
    {
        if (!string.Equals(appId, "1515950", StringComparison.Ordinal))
            return null;

        foreach (string library in FindSteamLibraries(selected))
        {
            string common = Path.Combine(library, "steamapps", "common");
            foreach (string relativePath in new[]
            {
                @"wallpaper_engine\bin\steam_api64.dll",
                @"wallpaper_engine\distribution\bin\steam_api64.dll"
            })
            {
                string candidate = Path.Combine(common, relativePath);
                if (File.Exists(candidate) && ReadMachine(candidate) == 0x8664)
                    return candidate;
            }
        }
        return null;
    }

    private static ArchivedSteamApi FindArchivedSteamApi(string gameRoot)
    {
        string[] archives;
        try
        {
            archives = Directory.GetFiles(gameRoot, "*", SearchOption.AllDirectories)
                .Where(path => string.Equals(Path.GetExtension(path), ".jar", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path.Length)
                .ToArray();
        }
        catch { return null; }

        foreach (string archivePath in archives)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                {
                    List<ZipArchiveEntry> steamEntries = archive.Entries
                        .Where(entry => string.Equals(Path.GetFileName(entry.FullName), "steam_api64.dll", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(Path.GetFileName(entry.FullName), "steam_api.dll", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(entry => string.Equals(Path.GetFileName(entry.FullName), "steam_api64.dll", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (ZipArchiveEntry entry in steamEntries)
                    {
                        byte[] bytes = ReadArchiveEntry(entry, 16L * 1024L * 1024L);
                        ushort machine = ReadMachine(bytes);
                        if (machine != 0x8664 && machine != 0x014c)
                            continue;

                        string version = FindUserStatsVersion(bytes);
                        if (string.IsNullOrEmpty(version))
                        {
                            foreach (ZipArchiveEntry companion in archive.Entries.Where(item =>
                                Path.GetFileName(item.FullName).IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0
                                && string.Equals(Path.GetExtension(item.FullName), ".dll", StringComparison.OrdinalIgnoreCase)))
                            {
                                string found = FindUserStatsVersion(ReadArchiveEntry(companion, 32L * 1024L * 1024L));
                                if (!string.IsNullOrEmpty(found))
                                    version = found;
                            }
                        }
                        return new ArchivedSteamApi
                        {
                            ArchivePath = archivePath,
                            EntryName = entry.FullName,
                            Machine = machine,
                            InterfaceVersion = version,
                            Format = "zip"
                        };
                    }
                }
            }
            catch { }
        }
        return null;
    }

    private static ArchivedSteamApi FindEmbeddedCabSteamApi(string gameRoot)
    {
        string[] executables;
        try
        {
            executables = Directory.GetFiles(gameRoot, "*.exe", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path.Length)
                .ToArray();
        }
        catch { return null; }

        foreach (string executable in executables)
        {
            long offset;
            if (!TryFindCabinetOffset(executable, out offset))
                continue;

            long cabinetLength;
            string steamEntry;
            if (!TryReadCabinet(executable, offset, out cabinetLength, out steamEntry))
                continue;

            ushort machine = ReadMachine(executable);
            if (machine != 0x8664 && machine != 0x014c)
                continue;

            return new ArchivedSteamApi
            {
                ArchivePath = executable,
                EntryName = steamEntry,
                Machine = machine,
                InterfaceVersion = string.Empty,
                Format = "cab",
                Offset = offset,
                Length = cabinetLength
            };
        }
        return null;
    }

    private static bool TryFindCabinetOffset(string path, out long offset)
    {
        offset = 0;
        try
        {
            FileInfo info = new FileInfo(path);
            if (info.Length < 36 || info.Length > 256L * 1024L * 1024L)
                return false;
            byte[] bytes = File.ReadAllBytes(path);
            for (int index = 0; index <= bytes.Length - 4; index++)
            {
                if (bytes[index] == (byte)'M' && bytes[index + 1] == (byte)'S'
                    && bytes[index + 2] == (byte)'C' && bytes[index + 3] == (byte)'F')
                {
                    offset = index;
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    private static bool TryReadCabinet(string path, long offset, out long cabinetLength, out string steamEntry)
    {
        cabinetLength = 0;
        steamEntry = string.Empty;
        try
        {
            using (FileStream stream = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
            {
                stream.Seek(offset, SeekOrigin.Begin);
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "MSCF")
                    return false;
                reader.ReadUInt32();
                cabinetLength = reader.ReadUInt32();
                reader.ReadUInt32();
                uint filesOffset = reader.ReadUInt32();
                reader.ReadUInt32();
                reader.ReadByte();
                reader.ReadByte();
                reader.ReadUInt16();
                ushort fileCount = reader.ReadUInt16();
                ushort flags = reader.ReadUInt16();
                reader.ReadUInt16();
                reader.ReadUInt16();

                if (cabinetLength <= 0 || offset + cabinetLength > stream.Length || (flags & 0x0004) != 0)
                    return false;
                stream.Seek(offset + filesOffset, SeekOrigin.Begin);
                for (int index = 0; index < fileCount; index++)
                {
                    reader.ReadUInt32();
                    reader.ReadUInt32();
                    reader.ReadUInt16();
                    reader.ReadUInt16();
                    reader.ReadUInt16();
                    reader.ReadUInt16();
                    StringBuilder name = new StringBuilder();
                    byte value;
                    while ((value = reader.ReadByte()) != 0 && name.Length < 1024)
                        name.Append((char)value);
                    string fileName = name.ToString();
                    if (string.Equals(Path.GetFileName(fileName), "steam_api.dll", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(Path.GetFileName(fileName), "steam_api64.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        steamEntry = fileName;
                        return true;
                    }
                }
            }
        }
        catch { }
        return false;
    }

    private static byte[] ReadArchiveEntry(ZipArchiveEntry entry, long maximumSize)
    {
        if (entry == null || entry.Length <= 0 || entry.Length > maximumSize)
            return new byte[0];
        using (Stream source = entry.Open())
        using (MemoryStream destination = new MemoryStream())
        {
            source.CopyTo(destination);
            return destination.ToArray();
        }
    }

    private static DetectedGame FindManifestGame(string selected)
    {
        DetectedGame best = null;
        int bestLength = -1;
        foreach (string library in FindSteamLibraries(selected))
        {
            string steamApps = Path.Combine(library, "steamapps");
            if (!Directory.Exists(steamApps))
                continue;

            foreach (string manifest in Directory.GetFiles(steamApps, "appmanifest_*.acf", SearchOption.TopDirectoryOnly))
            {
                string text;
                try { text = File.ReadAllText(manifest); }
                catch { continue; }

                string appId = MatchValue(text, "appid");
                string name = MatchValue(text, "name");
                string installDir = MatchValue(text, "installdir");
                if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(installDir))
                    continue;

                string root = Normalize(Path.Combine(steamApps, "common", installDir));
                bool selectedInsideGame = selected.Equals(root, StringComparison.OrdinalIgnoreCase)
                    || selected.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                if (selectedInsideGame && root.Length > bestLength)
                {
                    best = new DetectedGame
                    {
                        Name = string.IsNullOrEmpty(name) ? new DirectoryInfo(root).Name : name,
                        AppId = appId,
                        RootDirectory = root
                    };
                    bestLength = root.Length;
                }
            }
        }
        return best;
    }

    private static IEnumerable<string> FindSteamLibraries(string selected)
    {
        HashSet<string> libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string marker = Path.DirectorySeparatorChar + "steamapps" + Path.DirectorySeparatorChar + "common" + Path.DirectorySeparatorChar;
        int markerIndex = selected.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex > 0)
            libraries.Add(selected.Substring(0, markerIndex));

        string registryPath = null;
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                registryPath = key == null ? null : key.GetValue("SteamPath") as string;
        }
        catch { }

        if (!string.IsNullOrEmpty(registryPath) && Directory.Exists(registryPath))
            libraries.Add(Normalize(registryPath));

        string defaultSteam = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        if (Directory.Exists(defaultSteam))
            libraries.Add(Normalize(defaultSteam));
        if (Directory.Exists(@"D:\SteamLibrary"))
            libraries.Add(Normalize(@"D:\SteamLibrary"));

        foreach (string primary in libraries.ToArray())
        {
            string vdf = Path.Combine(primary, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf))
                continue;
            string text;
            try { text = File.ReadAllText(vdf); }
            catch { continue; }
            foreach (Match match in Regex.Matches(text, "\\\"path\\\"\\s+\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase))
            {
                string path = match.Groups[1].Value.Replace("\\\\", "\\");
                if (Directory.Exists(path))
                    libraries.Add(Normalize(path));
            }
        }
        return libraries;
    }

    private static string MatchValue(string text, string key)
    {
        Match match = Regex.Match(text, "\\\"" + Regex.Escape(key) + "\\\"\\s+\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static string ReadNearbyAppId(string directory)
    {
        DirectoryInfo current = new DirectoryInfo(directory);
        for (int depth = 0; current != null && depth < 5; depth++, current = current.Parent)
        {
            foreach (string fileName in new[] { "steam_appid.txt", "steam.appid.txt" })
            {
                string path = Path.Combine(current.FullName, fileName);
                if (File.Exists(path))
                {
                    string value = File.ReadAllText(path, Encoding.ASCII).Trim();
                    if (Regex.IsMatch(value, "^[0-9]+$"))
                        return value;
                }
            }
        }
        return string.Empty;
    }

    private static ushort ReadMachine(string path)
    {
        try
        {
            byte[] header = new byte[4096];
            using (FileStream stream = File.OpenRead(path))
            {
                int read = stream.Read(header, 0, header.Length);
                if (read < 64)
                    return 0;
                int peOffset = BitConverter.ToInt32(header, 0x3c);
                if (peOffset < 0 || peOffset + 6 > read)
                    return 0;
                return BitConverter.ToUInt16(header, peOffset + 4);
            }
        }
        catch { return 0; }
    }

    private static ushort ReadMachine(byte[] bytes)
    {
        try
        {
            if (bytes == null || bytes.Length < 64)
                return 0;
            int peOffset = BitConverter.ToInt32(bytes, 0x3c);
            if (peOffset < 0 || peOffset + 6 > bytes.Length)
                return 0;
            return BitConverter.ToUInt16(bytes, peOffset + 4);
        }
        catch { return 0; }
    }

    private static string FindUserStatsVersion(string dllPath, string gameRoot)
    {
        string embedded = FindUserStatsVersionInFile(dllPath);
        if (!string.IsNullOrEmpty(embedded))
            return embedded;

        List<string> candidates = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Normalize(dllPath) };

        Action<string> addCandidate = delegate(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;
            string normalized = Normalize(path);
            if (seen.Add(normalized))
                candidates.Add(normalized);
        };

        try
        {
            string pluginDirectory = Path.GetDirectoryName(dllPath);
            foreach (string path in Directory.GetFiles(pluginDirectory, "*", SearchOption.TopDirectoryOnly))
                addCandidate(path);
        }
        catch { }

        try
        {
            foreach (string path in Directory.GetFiles(gameRoot, "*", SearchOption.AllDirectories))
            {
                if (path.IndexOf("_BackUpThisFolder", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                string name = Path.GetFileName(path);
                bool likelySteamWrapper = name.IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0
                    && string.Equals(Path.GetExtension(name), ".dll", StringComparison.OrdinalIgnoreCase);
                bool likelyMetadata = string.Equals(name, "global-metadata.dat", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase);
                if (likelySteamWrapper || likelyMetadata)
                    addCandidate(path);
            }
        }
        catch { }

        int highest = -1;
        string result = string.Empty;
        foreach (string candidate in candidates)
        {
            string found = FindUserStatsVersionInFile(candidate);
            Match match = Regex.Match(found, "([0-9]{3})$");
            int number;
            if (match.Success && int.TryParse(match.Groups[1].Value, out number) && number > highest)
            {
                highest = number;
                result = found;
            }
        }
        return result;
    }

    private static string FindUserStatsVersionInFile(string path)
    {
        try
        {
            FileInfo info = new FileInfo(path);
            if (info.Length <= 0 || info.Length > 256L * 1024L * 1024L)
                return string.Empty;
            string ascii = Encoding.ASCII.GetString(File.ReadAllBytes(path));
            int highest = -1;
            string result = string.Empty;
            foreach (Match match in Regex.Matches(ascii, "STEAMUSERSTATS_INTERFACE_VERSION([0-9]{3})"))
            {
                int number;
                if (int.TryParse(match.Groups[1].Value, out number) && number > highest)
                {
                    highest = number;
                    result = match.Value;
                }
            }
            return result;
        }
        catch { return string.Empty; }
    }

    private static string FindUserStatsVersion(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;
        string ascii = Encoding.ASCII.GetString(bytes);
        int highest = -1;
        string result = string.Empty;
        foreach (Match match in Regex.Matches(ascii, "STEAMUSERSTATS_INTERFACE_VERSION([0-9]{3})"))
        {
            int number;
            if (int.TryParse(match.Groups[1].Value, out number) && number > highest)
            {
                highest = number;
                result = match.Value;
            }
        }
        return result;
    }

    private static string Normalize(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
}
