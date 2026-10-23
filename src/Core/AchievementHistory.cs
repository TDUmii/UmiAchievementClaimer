using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace UmiAchievementClaimer
{
    internal sealed class AchievementSnapshot
    {
        public string Utc;
        public string AppId;
        public string Game;
        public List<AchievementItemState> Items = new List<AchievementItemState>();
    }
    internal sealed class AchievementItemState
    {
        public string Id;
        public string Name;
        public bool Unlocked;
        public long UnlockUnix;
    }
    internal sealed class PendingClaim
    {
        public string AppId;
        public string Utc;
        public string[] SelectedIds;
        public List<AchievementItemState> Before = new List<AchievementItemState>();
    }

    // Read-only snapshots and pending-claim records are scoped to this Windows profile.
    // Never writes absolute installation paths, credentials, or Steam authentication state.
    internal static class AchievementHistory
    {
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 };
        private static string DataFolder
        {
            get
            {
                string test = AppDomain.CurrentDomain.GetData("UmiAchievementClaimer.TestHistoryPath") as string;
                return !string.IsNullOrEmpty(test) ? test : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "UmiAchievementClaimer", "history");
            }
        }
        private static string PathFor(string appId, string type)
        {
            if (!Regex.IsMatch(appId ?? "", @"^\d{1,12}$"))
                throw new ArgumentException("Invalid Steam AppID");
            return Path.Combine(DataFolder, appId + "." + type + ".json");
        }

        internal static List<AchievementSnapshot> Read(string appId)
        {
            try
            {
                string file = PathFor(appId, "snapshots");
                if (!File.Exists(file)) return new List<AchievementSnapshot>();
                return Json.Deserialize<List<AchievementSnapshot>>(File.ReadAllText(file, Encoding.UTF8))
                    ?? new List<AchievementSnapshot>();
            }
            catch (Exception)
            { return new List<AchievementSnapshot>(); }
        }
        private static void Write(string file, object data)
        {
            Directory.CreateDirectory(DataFolder);
            string temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, Json.Serialize(data), new UTF8Encoding(false));
                if (File.Exists(file)) File.Replace(temp, file, null);
                else File.Move(temp, file);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        internal static AchievementSnapshot Create(string appId, string gameName, IEnumerable<AchievementItem> items)
        {
            return new AchievementSnapshot
            {
                AppId = appId, Game = gameName, Utc = DateTime.UtcNow.ToString("o"),
                Items = items.Select(i => new AchievementItemState {
                    Id = i.Id, Name = i.Name, Unlocked = i.Achieved, UnlockUnix = i.UnlockUnix
                }).ToList()
            };
        }
        internal static bool Save(AchievementSnapshot snap)
        {
            List<AchievementSnapshot> existing = Read(snap.AppId);
            if (existing.Count > 0 && Equivalent(existing[existing.Count - 1], snap)) return false;
            existing.Add(snap);
            if (existing.Count > 30) existing.RemoveRange(0, existing.Count - 30);
            Write(PathFor(snap.AppId, "snapshots"), existing);
            return true;
        }
        internal static bool Equivalent(AchievementSnapshot one, AchievementSnapshot two)
        {
            if (one.Items.Count != two.Items.Count) return false;
            Dictionary<string, AchievementItemState> a = one.Items.ToDictionary(x => x.Id, StringComparer.Ordinal);
            return two.Items.All(x => a.ContainsKey(x.Id) && a[x.Id].Unlocked == x.Unlocked && a[x.Id].UnlockUnix == x.UnlockUnix);
        }
        internal static PendingClaim ReadPending(string appId)
        {
            try
            {
                string p = PathFor(appId, "pending");
                return File.Exists(p) ? Json.Deserialize<PendingClaim>(File.ReadAllText(p, Encoding.UTF8)) : null;
            }
            catch (Exception) { return null; }
        }
        internal static void SavePending(string appId, IEnumerable<AchievementItem> all, IEnumerable<AchievementItem> selected)
        {
            PendingClaim record = new PendingClaim {
                AppId = appId, Utc = DateTime.UtcNow.ToString("o"),
                SelectedIds = selected.Select(x => x.Id).Distinct(StringComparer.Ordinal).ToArray(),
                Before = Create(appId, "", all).Items
            };
            Write(PathFor(appId, "pending"), record);
        }
        internal static void ClearPending(string appId)
        {
            string file = PathFor(appId, "pending");
            if (File.Exists(file)) File.Delete(file);
        }
        internal static string[] Changes(AchievementSnapshot before, AchievementSnapshot after)
        {
            var states = before.Items.ToDictionary(x => x.Id, StringComparer.Ordinal);
            return after.Items.Where(x => states.ContainsKey(x.Id) && !states[x.Id].Unlocked && x.Unlocked)
                .Select(x => x.Name).ToArray();
        }
        internal static string RecoverySummary(PendingClaim pending, IEnumerable<AchievementItem> current, bool english)
        {
            if (pending == null) return english ? "No pending claim to review." : "Không có lần Claim cần đối chiếu.";
            var now = current.ToDictionary(x => x.Id, StringComparer.Ordinal);
            var before = pending.Before.ToDictionary(x => x.Id, StringComparer.Ordinal);
            int unlocked = 0, unchanged = 0, unknown = 0;
            foreach (string id in pending.SelectedIds ?? new string[0])
            {
                AchievementItem item;
                if (!now.TryGetValue(id, out item)) { unknown++; continue; }
                AchievementItemState old;
                if (!before.TryGetValue(id, out old)) { unknown++; continue; }
                if (item.Achieved && !old.Unlocked) unlocked++;
                else if (item.Achieved && old.Unlocked) unchanged++;
                else unchanged++;
            }
            return (english ? "Read-only comparison after selected claim" : "Đối chiếu chỉ đọc sau Claim") +
                "\n" + (english ? "Newly unlocked: " : "Mới mở khóa: ") + unlocked +
                "\n" + (english ? "No observed change: " : "Chưa thấy thay đổi: ") + unchanged +
                "\n" + (english ? "Unknown or missing: " : "Không xác định: ") + unknown +
                "\n" + (english ? "This does not prove Steam-server sync." : "Không xác nhận đồng bộ máy chủ Steam.");
        }
        internal static string[] KnownGames()
        {
            if (!Directory.Exists(DataFolder)) return new string[0];
            return Directory.GetFiles(DataFolder, "*.snapshots.json")
                .Select(Path.GetFileName).Where(x => Regex.IsMatch(x, @"^\d+\.snapshots\.json$"))
                .Select(x => x.Split('.')[0]).ToArray();
        }
        internal static string ExportCsv(AchievementSnapshot snapshot)
        {
            Func<string,string> esc = value => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
            // Avoid spreadsheet formula evaluation when opened in Excel.
            Func<string,string> safe = value => value != null && value.Length > 0 &&
                "=+-@".IndexOf(value[0]) >= 0 ? "'" + value : value;
            StringBuilder s = new StringBuilder("AppID,Game,API ID,Name,Unlocked,Unlock time UTC\r\n");
            foreach (AchievementItemState x in snapshot.Items)
            {
                DateTime? time = x.UnlockUnix > 0 ? (DateTime?)new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(x.UnlockUnix) : null;
                s.Append(esc(snapshot.AppId)).Append(',').Append(esc(safe(snapshot.Game))).Append(',')
                    .Append(esc(safe(x.Id))).Append(',').Append(esc(safe(x.Name))).Append(',')
                    .Append(x.Unlocked ? "true" : "false").Append(',')
                    .Append(esc(time.HasValue ? time.Value.ToString("o") : "")).Append("\r\n");
            }
            return s.ToString();
        }
    }
}
