using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

// Optional live, read-only smoke test of public Steam artwork in the compiled EXE.
// No Steam account, worker, installed game or achievement change is involved.
internal static class ArtworkSmoke
{
    private static Type artworkType;
    private static int passed;
    private static int failed;

    private static object Load(string appId)
    {
        Task task = (Task)artworkType.GetMethod("LoadAsync").Invoke(null, new object[] { appId });
        task.GetAwaiter().GetResult();
        return task.GetType().GetProperty("Result").GetValue(task, null);
    }
    private static Image Image(string url)
    {
        Task<Image> task = (Task<Image>)artworkType.GetMethod("LoadImageAsync").Invoke(null, new object[] { url });
        return task.GetAwaiter().GetResult();
    }
    private static object Field(object target, string name) { return artworkType.GetField(name).GetValue(target); }
    private static Dictionary<string, string> Icons(object target, string name) { return (Dictionary<string, string>)Field(target, name); }
    private static void Check(bool condition, string name)
    {
        Console.WriteLine((condition ? "PASS " : "FAIL ") + name);
        if (condition) passed++; else failed++;
    }

    private static void Save(Image image, string directory, string name)
    {
        image.Save(Path.Combine(directory, name), ImageFormat.Png);
        Console.WriteLine("FIXTURE " + name + " " + image.Width + "x" + image.Height);
    }

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 2) throw new ArgumentException("Usage: ArtworkSmoke.exe APPLICATION.exe OUTPUT_DIRECTORY");
            Assembly application = Assembly.LoadFrom(Path.GetFullPath(args[0]));
            artworkType = application.GetType("UmiAchievementClaimer.SteamArtwork", true);
            string output = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(output);
            Stopwatch elapsed = Stopwatch.StartNew();
            using (IDisposable invalid = (IDisposable)Load("../invalid-app"))
                Check(Field(invalid, "Cover") == null && Icons(invalid, "IconUrls").Count == 0, "invalid AppID returns empty artwork");
            using (Image invalid = Image("https://example.com/untrusted.png"))
                Check(invalid == null, "non-Steam image host is rejected");
            using (Image invalid = Image("http://shared.fastly.steamstatic.com/untrusted.png"))
                Check(invalid == null, "unencrypted image URL is rejected");

            List<string> sources = new List<string> { "Public Steam artwork smoke: AppID 620 (Portal 2). No Steam runtime was invoked." };
            using (IDisposable artwork = (IDisposable)Load("620"))
            {
                Image cover = (Image)Field(artwork, "Cover");
                Dictionary<string, string> unlocked = Icons(artwork, "IconUrls");
                Dictionary<string, string> locked = Icons(artwork, "LockedIconUrls");
                Check(cover != null, "Portal 2 public cover downloads");
                Check(unlocked.Keys.Any(key => key.StartsWith("id:", StringComparison.Ordinal)), "unlocked schema includes API ID keys");
                Check(locked.Keys.Any(key => key.StartsWith("id:", StringComparison.Ordinal)), "locked schema includes API ID keys");
                Check(unlocked.Keys.Any(key => key.StartsWith("name:", StringComparison.Ordinal)), "schema includes display-name fallback keys");
                Console.WriteLine("METADATA unlocked_keys=" + unlocked.Count + " locked_keys=" + locked.Count);
                if (cover != null)
                {
                    string coverUrl = (string)Field(artwork, "CoverUrl");
                    Console.WriteLine("COVER_SOURCE " + coverUrl);
                    sources.Add("portal2-cover.png: " + coverUrl);
                    Save(cover, output, "portal2-cover.png");
                }
                string iconKey = unlocked.ContainsKey("id:ACH.SURVIVE_CONTAINER_RIDE") && locked.ContainsKey("id:ACH.SURVIVE_CONTAINER_RIDE")
                    ? "id:ACH.SURVIVE_CONTAINER_RIDE" : unlocked.Keys.FirstOrDefault(key => key.StartsWith("id:", StringComparison.Ordinal) && locked.ContainsKey(key));
                Check(iconKey != null, "both icon states share at least one API ID");
                if (iconKey != null)
                {
                    Console.WriteLine("ICON_KEY " + iconKey);
                    for (int state = 0; state < 2; state++)
                    {
                        string url = (state == 0 ? unlocked : locked)[iconKey];
                        string name = state == 0 ? "portal2-icon-unlocked.png" : "portal2-icon-locked.png";
                        Console.WriteLine("ICON_SOURCE " + url);
                        sources.Add(name + " (" + iconKey + "): " + url);
                        using (Image icon = Image(url))
                        {
                            Check(icon != null, state == 0 ? "unlocked icon downloads" : "locked icon downloads");
                            if (icon != null) Save(icon, output, name);
                        }
                    }
                }
            }
            File.WriteAllLines(Path.Combine(output, "steam-artwork-sources.txt"), sources);
            Console.WriteLine("ELAPSED_MS " + elapsed.ElapsedMilliseconds);
            Console.WriteLine("RESULT " + passed + " PASS, " + failed + " FAIL");
            Console.WriteLine("Scope: official public Steam HTTP artwork only. No profile, game detection, worker or claim was used.");
            return failed == 0 ? 0 : 1;
        }
        catch (Exception error)
        {
            Console.WriteLine("FATAL " + error);
            return 2;
        }
    }
}
