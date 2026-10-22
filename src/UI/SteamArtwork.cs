using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace UmiAchievementClaimer
{
// Public, read-only artwork. No Steam account, cookies, API key or game DLL is used.
// Metadata is small; callers fetch only visible icons and own those returned images.
internal sealed class SteamArtwork : IDisposable
{
    private const int MaximumBytes = 2 * 1024 * 1024;
    private static readonly SemaphoreSlim Downloads = new SemaphoreSlim(4);
    public Image Cover;
    public string CoverUrl;
    public readonly Dictionary<string, string> IconUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<string, string> LockedIconUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    static SteamArtwork()
    {
        // Keep OS defaults on modern Framework versions; add TLS 1.2 to older defaults.
        if ((int)ServicePointManager.SecurityProtocol != 0)
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
    }

    public static async Task<SteamArtwork> LoadAsync(string appId)
    {
        SteamArtwork artwork = new SteamArtwork();
        uint numericId;
        if (!uint.TryParse(appId, NumberStyles.None, CultureInfo.InvariantCulture, out numericId) || numericId == 0)
            return artwork;
        string id = numericId.ToString(CultureInfo.InvariantCulture);
        using (CancellationTokenSource deadline = new CancellationTokenSource(15000))
        {
            Task<string> store = ReadTextAsync("https://store.steampowered.com/api/appdetails?appids=" + id + "&l=english", deadline.Token);
            Task<string> community = ReadTextAsync("https://steamcommunity.com/stats/" + id + "/achievements/?l=english", deadline.Token);
            await Task.WhenAll(store, community).ConfigureAwait(false);
            string coverUrl = null;
            try { coverUrl = artwork.ReadStore(store.Result, id); }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            try { artwork.ReadCommunity(community.Result); }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            Uri allowedCover;
            artwork.CoverUrl = TryAllowedUri(coverUrl, true, out allowedCover) ? allowedCover.AbsoluteUri : null;
            artwork.Cover = await ReadImageAsync(artwork.CoverUrl, deadline.Token).ConfigureAwait(false);
            if (artwork.Cover == null) artwork.Cover = TryLocalSteamCover(id);
        }
        return artwork;
    }

    private static Image TryLocalSteamCover(string id)
    {
        try
        {
            string[] roots = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
                @"C:\Program Files (x86)\Steam",
                @"D:\Steam"
            };
            foreach (string root in roots)
            {
                string folder = Path.Combine(root, "appcache", "librarycache");
                foreach (string location in new[] { Path.Combine(folder, id), folder })
                {
                    if (!Directory.Exists(location)) continue;
                    foreach (string pattern in new[] { "library_header.jpg", "library_capsule.jpg", "library_hero.jpg", id + "_header.jpg", id + "_library_600x900.jpg" })
                    {
                        string[] paths = Directory.GetFiles(location, pattern, location == folder ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
                        foreach (string path in paths)
                        {
                            try
                            {
                                FileInfo info = new FileInfo(path);
                                if (info.Length <= 0 || info.Length > MaximumBytes) continue;
                                using (Image source = Image.FromFile(path))
                                {
                                    if (source.Width > 4096 || source.Height > 4096) continue;
                                    return new Bitmap(source);
                                }
                            }
                            catch (ArgumentException) { }
                            catch (IOException) { }
                            catch (OutOfMemoryException) { }
                        }
                    }
                }
            }
        }
        catch (Exception) { }
        return null;
    }

    // The caller owns the result and must dispose it when changing games or closing.
    public static Task<Image> LoadImageAsync(string url)
    {
        return ReadImageAsync(url, CancellationToken.None);
    }

    private string ReadStore(string json, string id)
    {
        if (string.IsNullOrEmpty(json)) return null;
        JavaScriptSerializer serializer = Serializer();
        Dictionary<string, object> root = Object(serializer.DeserializeObject(json));
        Dictionary<string, object> app = Object(Value(root, id));
        Dictionary<string, object> data = Object(Value(app, "data"));
        Dictionary<string, object> achievements = Object(Value(data, "achievements"));
        IEnumerable highlighted = Value(achievements, "highlighted") as IEnumerable;
        if (highlighted != null)
            foreach (object entry in highlighted)
            {
                Dictionary<string, object> item = Object(entry);
                AddUrl(IconUrls, null, Text(item, "name"), Text(item, "path"));
            }
        return Text(data, "header_image");
    }

    private void ReadCommunity(string html)
    {
        if (string.IsNullOrEmpty(html)) return;
        Match script = Regex.Match(html,
            @"<script\b[^>]*\bid\s*=\s*[""']valve-ssr-data[""'][^>]*>([\s\S]*?)</script>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (script.Success)
        {
            JavaScriptSerializer serializer = Serializer();
            Dictionary<string, object> root = Object(serializer.DeserializeObject(script.Groups[1].Value));
            string queryData = Text(Object(Value(root, "renderContext")), "queryData");
            if (!string.IsNullOrEmpty(queryData))
                ReadAchievementNodes(serializer.DeserializeObject(queryData));
        }

        // Older Steam Community pages render the name and image directly in HTML.
        foreach (Match row in Regex.Matches(html,
            @"<div\b[^>]*class=[""']achieveRow[^""']*[""'][^>]*>[\s\S]*?<img\b[^>]*src=[""']([^""']+)[""'][^>]*>[\s\S]*?<h3\b[^>]*>([\s\S]*?)</h3>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            string name = WebUtility.HtmlDecode(Regex.Replace(row.Groups[2].Value, "<[^>]+>", string.Empty)).Trim();
            AddUrl(IconUrls, null, name, WebUtility.HtmlDecode(row.Groups[1].Value));
        }
    }

    private void ReadAchievementNodes(object node)
    {
        Dictionary<string, object> item = Object(node);
        if (item != null)
        {
            string id = Text(item, "api_name");
            string name = Text(item, "name");
            AddUrl(IconUrls, id, name, Text(item, "icon_achieved"));
            AddUrl(LockedIconUrls, id, name, Text(item, "icon_unachieved"));
            foreach (object child in item.Values)
                if (!(child is string)) ReadAchievementNodes(child);
        }
        else
        {
            IEnumerable children = node as IEnumerable;
            if (children != null && !(node is string))
                foreach (object child in children) ReadAchievementNodes(child);
        }
    }

    private static JavaScriptSerializer Serializer()
    {
        return new JavaScriptSerializer { MaxJsonLength = MaximumBytes, RecursionLimit = 64 };
    }

    private static Dictionary<string, object> Object(object value) { return value as Dictionary<string, object>; }
    private static object Value(Dictionary<string, object> source, string key)
    {
        object value;
        return source != null && source.TryGetValue(key, out value) ? value : null;
    }
    private static string Text(Dictionary<string, object> source, string key) { return Value(source, key) as string; }

    private static void AddUrl(Dictionary<string, string> target, string id, string name, string url)
    {
        Uri uri;
        if (!TryAllowedUri(url, true, out uri)) return;
        if (!string.IsNullOrWhiteSpace(id)) target["id:" + id] = uri.AbsoluteUri;
        if (!string.IsNullOrWhiteSpace(name)) target["name:" + name] = uri.AbsoluteUri;
    }

    private static bool TryAllowedUri(string url, bool imageOnly, out Uri uri)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps
            || !uri.IsDefaultPort || uri.UserInfo.Length != 0) return false;
        string host = uri.DnsSafeHost;
        bool cdn = host.EndsWith(".steamstatic.com", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "steamcdn-a.akamaihd.net", StringComparison.OrdinalIgnoreCase);
        return cdn || !imageOnly && (string.Equals(host, "steamcommunity.com", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "store.steampowered.com", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string> ReadTextAsync(string url, CancellationToken token)
    {
        byte[] bytes = await ReadBytesAsync(url, false, token).ConfigureAwait(false);
        return bytes == null ? null : Encoding.UTF8.GetString(bytes);
    }

    private static async Task<Image> ReadImageAsync(string url, CancellationToken token)
    {
        byte[] bytes = await ReadBytesAsync(url, true, token).ConfigureAwait(false);
        if (bytes == null) return null;
        try
        {
            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (Image source = Image.FromStream(stream, false, true))
            {
                if (source.Width > 4096 || source.Height > 4096 || (long)source.Width * source.Height > 16000000)
                    return null;
                return new Bitmap(source);
            }
        }
        catch (ArgumentException) { return null; }
        catch (OutOfMemoryException) { return null; }
        catch (System.Runtime.InteropServices.ExternalException) { return null; }
    }

    private static async Task<byte[]> ReadBytesAsync(string url, bool imageOnly, CancellationToken token)
    {
        Uri uri;
        if (!TryAllowedUri(url, imageOnly, out uri)) return null;
        bool acquired = false;
        using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            timeout.CancelAfter(5000);
            try
            {
                await Downloads.WaitAsync(timeout.Token).ConfigureAwait(false);
                acquired = true;
                for (int redirects = 0; redirects < 4; redirects++)
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                    request.AllowAutoRedirect = false;
                    request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    request.UserAgent = "UmiAchievementClaimer (public Steam artwork)";
                    request.Timeout = request.ReadWriteTimeout = 5000;
                    request.UseDefaultCredentials = false;
                    using (timeout.Token.Register(request.Abort))
                    using (HttpWebResponse response = (HttpWebResponse)await Task<WebResponse>.Factory.FromAsync(
                        request.BeginGetResponse, request.EndGetResponse, null).ConfigureAwait(false))
                    {
                        int status = (int)response.StatusCode;
                        if (status >= 300 && status < 400)
                        {
                            string location = response.Headers[HttpResponseHeader.Location];
                            Uri redirected;
                            if (!Uri.TryCreate(uri, location, out redirected)
                                || !TryAllowedUri(redirected.AbsoluteUri, imageOnly, out uri)) return null;
                            continue;
                        }
                        if (status != 200 || response.ContentLength > MaximumBytes) return null;
                        using (Stream stream = response.GetResponseStream())
                        using (MemoryStream data = new MemoryStream())
                        {
                            byte[] buffer = new byte[16384];
                            int read;
                            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) != 0)
                            {
                                if (data.Length + read > MaximumBytes) return null;
                                data.Write(buffer, 0, read);
                            }
                            return data.ToArray();
                        }
                    }
                }
            }
            catch (WebException) { }
            catch (IOException) { }
            catch (OperationCanceledException) { }
            finally { if (acquired) Downloads.Release(); }
        }
        return null;
    }

    public void Dispose()
    {
        if (Cover != null) { Cover.Dispose(); Cover = null; }
    }
}
}
