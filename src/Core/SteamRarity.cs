using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace UmiAchievementClaimer
{
    // Uses Steam's public aggregate endpoint. Missing/unpublished statistics remain N/A.
    internal static class SteamRarity
    {
        internal static Dictionary<string, double> Parse(string json)
        {
            Dictionary<string, double> result = new Dictionary<string, double>(StringComparer.Ordinal);
            var data = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            if (data == null || !data.ContainsKey("achievementpercentages")) return result;
            var outer = data["achievementpercentages"] as Dictionary<string, object>;
            if (outer == null || !outer.ContainsKey("achievements")) return result;
            var entries = outer["achievements"] as object[];
            if (entries == null) return result;
            foreach (object entry in entries)
            {
                var item = entry as Dictionary<string, object>;
                if (item == null || !item.ContainsKey("name") || !item.ContainsKey("percent")) continue;
                string name = Convert.ToString(item["name"], CultureInfo.InvariantCulture);
                double value;
                if (string.IsNullOrWhiteSpace(name) ||
                    !double.TryParse(Convert.ToString(item["percent"], CultureInfo.InvariantCulture),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out value)) continue;
                if (value >= 0 && value <= 100) result[name] = value;
            }
            return result;
        }
        internal static Task<Dictionary<string,double>> FetchAsync(string appId)
        {
            if (!Regex.IsMatch(appId ?? "", @"^\d{1,12}$"))
                return Task.FromResult(new Dictionary<string,double>());
            return Task.Run(() => {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(
                        "https://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v0002/?gameid=" + appId);
                    request.Timeout = 9000;
                    request.ReadWriteTimeout = 9000;
                    request.UserAgent = "UmiAchievementClaimer/1.11";
                    using (WebResponse response = request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string json = reader.ReadToEnd();
                        if (json.Length > 1024 * 1024) return new Dictionary<string,double>();
                        return Parse(json);
                    }
                }
                catch (Exception) { return new Dictionary<string,double>(); }
            });
        }
        internal static string Tier(double percent, bool english)
        {
            if (percent < 0) return english ? "Unavailable" : "Không có dữ liệu";
            if (percent <= 5) return english ? "Ultra rare" : "Cực hiếm";
            if (percent <= 20) return english ? "Rare" : "Hiếm";
            return english ? "Common" : "Phổ biến";
        }
    }
}
