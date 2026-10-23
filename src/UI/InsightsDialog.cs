using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
    // Read-only insights. No Steam credentials, account APIs or claim operations.
    internal sealed class InsightsDialog : Form
    {
        private readonly bool english;
        private readonly string activeId;
        private readonly List<AchievementItem> activeItems;
        private readonly Label summary = new Label();
        private readonly Label recovery = new Label();
        private readonly Button clearPending = new UmiButton();
        private readonly List<string> historyIds = new List<string>();
        private readonly ListBox history = new FlatLibraryList();
        private readonly ThemedScrollBar historyScrollbar = new ThemedScrollBar();
        private readonly Label gamesMetric = new Label();
        private readonly Label unlockedMetric = new Label();
        private readonly Label snapshotsMetric = new Label();
        private readonly Button export = new UmiButton();
        private readonly Button checkUpdate = new UmiButton();
        private readonly Button dismiss = new UmiButton();
        private readonly Button rescan = new UmiButton();
        private readonly Label updateStatus = new Label();

        internal InsightsDialog(string appId, IEnumerable<AchievementItem> current, bool english)
        {
            this.english = english;
            activeId = appId;
            activeItems = (current ?? Enumerable.Empty<AchievementItem>()).ToList();
            Text = english ? "Achievement insights" : "Thống kê thành tích";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(805, 680);
            MinimumSize = new Size(665, 555);
            BackColor = UiTheme.Canvas;
            ForeColor = UiTheme.Text;
            Font = UiTheme.Font(10);
            Padding = new Padding(22);
            AutoScaleMode = AutoScaleMode.Dpi;

            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 71));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 114));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 162));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            Label title = new Label { Dock = DockStyle.Fill, Text = english ? "Achievement Dashboard" : "Bảng thống kê thành tích",
                Font = UiTheme.DisplayFont(20F, FontStyle.Bold), ForeColor = UiTheme.Text };
            root.Controls.Add(title, 0, 0);
            summary.Dock = DockStyle.Fill;
            summary.Padding = new Padding(15);
            summary.BackColor = UiTheme.Surface;
            summary.ForeColor = UiTheme.Text;
            summary.Font = UiTheme.DisplayFont(11F);
            // Three scannable metrics replace the dense all-in-one summary line.
            TableLayoutPanel metricCards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3,
                RowCount = 1, Margin = new Padding(0) };
            for (int m = 0; m < 3; m++) metricCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            metricCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            metricCards.Controls.Add(MetricCard(english ? "GAMES TRACKED" : "GAME ĐÃ QUÉT", gamesMetric, 0), 0, 0);
            metricCards.Controls.Add(MetricCard(english ? "ACHIEVEMENTS UNLOCKED" : "THÀNH TÍCH ĐÃ MỞ", unlockedMetric, 1), 1, 0);
            metricCards.Controls.Add(MetricCard(english ? "SAVED SNAPSHOTS" : "BẢN LƯU CỤC BỘ", snapshotsMetric, 2), 2, 0);
            metricCards.Controls.Add(summary, 0, 0); // Summary text is accessibility-only, no duplicate visual labels.
            summary.Visible = false;
            root.Controls.Add(metricCards, 0, 1);
            var caption = new Label { Dock = DockStyle.Fill, Text = english ? "LOCAL SNAPSHOT HISTORY" : "LỊCH SỬ SNAPSHOT TRÊN MÁY",
                ForeColor = UiTheme.Cyan, Font = UiTheme.DisplayFont(10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(2, 9, 0, 0) };
            root.Controls.Add(caption, 0, 2);
            Panel historyCard = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface,
                Padding = new Padding(12, 8, 10, 8) };
            history.Dock = DockStyle.Fill;
            history.BackColor = UiTheme.Surface;
            history.ForeColor = UiTheme.Text;
            history.BorderStyle = BorderStyle.None;
            history.DrawMode = DrawMode.OwnerDrawFixed;
            history.ItemHeight = 58;
            history.IntegralHeight = false;
            history.Font = UiTheme.Font(10F);
            history.DrawItem += DrawHistoryItem;
            historyScrollbar.Dock = DockStyle.Right;
            historyScrollbar.Width = 12;
            historyScrollbar.Visible = false;
            historyScrollbar.ValueChanged += delegate
            {
                if (history.Items.Count > 0 && history.TopIndex != historyScrollbar.Value)
                    history.TopIndex = historyScrollbar.Value;
            };
            history.MouseWheel += delegate { BeginInvoke(new Action(UpdateHistoryScroll)); };
            history.Resize += delegate { UpdateHistoryScroll(); };
            history.SelectedIndexChanged += delegate { UpdateHistoryScroll(); };
            historyCard.Controls.Add(history);
            historyCard.Controls.Add(historyScrollbar);
            root.Controls.Add(historyCard, 0, 3);
            history.DoubleClick += delegate { ShowTimeline(); };
            history.AccessibleName = english ? "Local game snapshots, double click for timeline" :
                "Lịch sử lưu game, nhấp đúp để xem diễn biến";

            recovery.Dock = DockStyle.Fill;
            recovery.BackColor = UiTheme.SurfaceMuted;
            recovery.ForeColor = UiTheme.Text;
            recovery.Padding = new Padding(14, 9, 10, 6);
            recovery.Font = UiTheme.Font(10F);
            Panel recoveryPanel = new Panel { Dock = DockStyle.Fill };
            UiTheme.StyleButton(clearPending, UmiButtonStyle.Ghost);
            clearPending.Text = english ? "Dismiss review" : "Bỏ đánh dấu";
            clearPending.Width = 126;
            clearPending.Dock = DockStyle.Right;
            clearPending.Click += delegate
            {
                if (string.IsNullOrEmpty(activeId)) return;
                if (MessageBox.Show(this, english
                    ? "Dismiss the stored recovery note? This does not change Steam."
                    : "Bỏ ghi chú khôi phục? Thao tác này không thay đổi Steam.",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                AchievementHistory.ClearPending(activeId);
                RefreshData();
            };
            recoveryPanel.Controls.Add(recovery);
            recoveryPanel.Controls.Add(clearPending);
            root.Controls.Add(recoveryPanel, 0, 4);

            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                Margin = new Padding(0, 9, 0, 0) };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));
            bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            updateStatus.Text = english ? "Update check is read-only" : "Kiểm tra cập nhật chỉ đọc";
            updateStatus.Dock = DockStyle.Fill;
            updateStatus.ForeColor = UiTheme.MutedText;
            bottom.Controls.Add(updateStatus, 0, 0);
            bottom.SetColumnSpan(updateStatus, 4);
            foreach (Button button in new[] { export, checkUpdate, rescan, dismiss })
            { UiTheme.StyleButton(button, UmiButtonStyle.Secondary); button.Dock = DockStyle.Fill; button.Margin = new Padding(0, 2, 8, 0); }
            export.Text = english ? "Export CSV" : "Xuất CSV";
            checkUpdate.Text = english ? "Check update" : "Xem cập nhật";
            rescan.Text = english ? "Rescan" : "Quét lại";
            var infoTip = new ToolTip();
            infoTip.SetToolTip(rescan, english ? "Read-only scan. Never repeats Claim." : "Quét chỉ đọc, không tự Claim lại.");
            dismiss.Text = english ? "Close" : "Đóng";
            bottom.Controls.Add(export, 0, 1);
            bottom.Controls.Add(checkUpdate, 1, 1);
            bottom.Controls.Add(rescan, 2, 1);
            bottom.Controls.Add(dismiss, 3, 1);
            root.Controls.Add(bottom, 0, 5);
            Controls.Add(root);
            UiTheme.StyleButton(dismiss, UmiButtonStyle.Primary);
            dismiss.DialogResult = DialogResult.Cancel;
            CancelButton = dismiss;
            export.Click += ExportCsv;
            checkUpdate.Click += async delegate { await CheckForUpdate(); };
            rescan.Click += delegate { DialogResult = DialogResult.Retry; Close(); };
            RefreshData();
        }

        private void RefreshData()
        {
            history.Items.Clear();
            historyIds.Clear();
            int games = 0, total = 0, unlocked = 0, count = 0;
            foreach (string id in AchievementHistory.KnownGames())
            {
                List<AchievementSnapshot> snapshots = AchievementHistory.Read(id);
                if (snapshots.Count == 0) continue;
                AchievementSnapshot last = snapshots[snapshots.Count - 1];
                games++;
                total += last.Items.Count;
                unlocked += last.Items.Count(x => x.Unlocked);
                count += snapshots.Count;
                var previous = snapshots.Count > 1 ? snapshots[snapshots.Count - 2] : null;
                string delta = previous == null ? "first scan" :
                    "+" + AchievementHistory.Changes(previous, last).Length + " unlocked";
                string game = string.IsNullOrWhiteSpace(last.Game) ? id : last.Game;
                historyIds.Add(id);
                history.Items.Add(game + "  [" + id + "]     " +
                    last.Items.Count(x => x.Unlocked) + "/" + last.Items.Count +
                    "     ·     " + delta + "     ·     " + last.Utc.Substring(0, Math.Min(16, last.Utc.Length)) + " UTC");
            }
            gamesMetric.Text = games.ToString();
            unlockedMetric.Text = unlocked + " / " + total;
            snapshotsMetric.Text = count.ToString();
            UpdateHistoryScroll();
            summary.Text = (english ? "GAMES  " : "GAME  ") + games +
                "          " + (english ? "UNLOCKED  " : "ĐÃ MỞ  ") + unlocked + "/" + total +
                "          " + (english ? "SNAPSHOTS  " : "BẢN LƯU  ") + count +
                Environment.NewLine + (english ? "Stored locally, only after a successful read-only list." :
                    "Chỉ lưu trên máy sau khi đọc danh sách thành công.");
            PendingClaim pending = string.IsNullOrEmpty(activeId) ? null : AchievementHistory.ReadPending(activeId);
            recovery.Text = pending == null
                ? (english ? "RECOVERY   No pending claim for the selected game." : "KHÔI PHỤC   Không có Claim cần kiểm tra.")
                : "RECOVERY   " + AchievementHistory.RecoverySummary(pending, activeItems, english) +
                    Environment.NewLine + (english ? "Use Read-only rescan for updated results." : "Chọn quét lại chỉ đọc để cập nhật kết quả.");
            clearPending.Visible = pending != null;
            rescan.Enabled = !string.IsNullOrEmpty(activeId);
            export.Enabled = !string.IsNullOrEmpty(activeId) && AchievementHistory.Read(activeId).Count > 0;
            if (history.Items.Count == 0)
                history.Items.Add(english ? "No snapshots yet. Scan a Steam game to start." :
                    "Chưa có bản lưu. Quét một game Steam để bắt đầu.");
        }

        private Control MetricCard(string heading, Label value, int index)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface,
                Margin = new Padding(index == 0 ? 0 : 5, 0, index == 2 ? 0 : 5, 0),
                Padding = new Padding(17, 12, 12, 10) };
            Label caption = new Label { Dock = DockStyle.Top, Height = 24,
                ForeColor = UiTheme.MutedText, Font = UiTheme.Font(8.5F, FontStyle.Bold),
                Text = heading };
            value.Dock = DockStyle.Fill;
            value.Font = UiTheme.DisplayFont(22F, FontStyle.Bold);
            value.ForeColor = index == 1 ? UiTheme.Success : (index == 2 ? UiTheme.Cyan : UiTheme.Text);
            value.TextAlign = ContentAlignment.MiddleLeft;
            card.Controls.Add(value);
            card.Controls.Add(caption);
            return card;
        }

        private void DrawHistoryItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= history.Items.Count) return;
            string raw = history.Items[e.Index].ToString();
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (SolidBrush brush = new SolidBrush(selected ? UiTheme.SurfaceMuted : UiTheme.Surface))
                e.Graphics.FillRectangle(brush, e.Bounds);
            using (Pen border = new Pen(UiTheme.Border))
                e.Graphics.DrawLine(border, e.Bounds.Left + 10, e.Bounds.Bottom - 1,
                    e.Bounds.Right - 10, e.Bounds.Bottom - 1);
            string[] parts = raw.Split(new[] { "     " }, 2, StringSplitOptions.None);
            Rectangle first = new Rectangle(e.Bounds.Left + 12, e.Bounds.Top + 8, e.Bounds.Width - 24, 25);
            Rectangle second = new Rectangle(e.Bounds.Left + 12, e.Bounds.Top + 32, e.Bounds.Width - 24, 20);
            using (Font title = UiTheme.DisplayFont(10.5F, FontStyle.Bold))
            using (Font body = UiTheme.Font(9F))
            {
                TextRenderer.DrawText(e.Graphics, parts[0], title, first, UiTheme.Text,
                    TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(e.Graphics, parts.Length > 1 ? parts[1] :
                    (english ? "Open a Steam game to start tracking" : "Mở game Steam để bắt đầu"),
                    body, second, UiTheme.MutedText,
                    TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }
            e.DrawFocusRectangle();
        }

        private void UpdateHistoryScroll()
        {
            if (history.IsDisposed) return;
            int visible = Math.Max(1, history.ClientSize.Height / Math.Max(1, history.ItemHeight));
            historyScrollbar.SetRange(0, Math.Max(0, history.Items.Count - 1), visible, history.TopIndex);
        }

        private void ShowTimeline()
        {
            int index = history.SelectedIndex;
            if (index < 0 || index >= historyIds.Count) return;
            var series = AchievementHistory.Read(historyIds[index]);
            StringBuilder text = new StringBuilder();
            for (int n = 0; n < series.Count; n++)
            {
                AchievementSnapshot snap = series[n];
                string date = snap.Utc.Length >= 16 ? snap.Utc.Substring(0, 16) : snap.Utc;
                text.Append(date).Append(" UTC — ").Append(snap.Items.Count(x => x.Unlocked))
                    .Append("/").Append(snap.Items.Count).AppendLine();
                if (n == 0) continue;
                string[] changes = AchievementHistory.Changes(series[n-1], snap);
                foreach (string name in changes.Take(7)) text.Append("  + ").AppendLine(name);
                if (changes.Length > 7) text.AppendLine("  …");
            }
            MessageBox.Show(this, text.ToString(), english ? "Snapshot timeline" : "Dòng thời gian snapshot",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportCsv(object sender, EventArgs e)
        {
            var records = AchievementHistory.Read(activeId);
            if (records.Count == 0) return;
            using (SaveFileDialog save = new SaveFileDialog {
                Title = english ? "Export snapshot" : "Xuất snapshot",
                Filter = "CSV (*.csv)|*.csv", FileName = "umi-achievements-" + activeId + ".csv" })
            {
                if (save.ShowDialog(this) == DialogResult.OK)
                    try
                    {
                        File.WriteAllText(save.FileName,
                            AchievementHistory.ExportCsv(records[records.Count - 1]), new UTF8Encoding(true));
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(this, exception.Message, english ? "Export failed" : "Xuất thất bại");
                    }
            }
        }

        // The updater never installs untrusted binaries or initiates a download.
        private async Task CheckForUpdate()
        {
            checkUpdate.Enabled = false;
            updateStatus.Text = english ? "Checking GitHub Releases..." : "Đang kiểm tra GitHub Releases...";
            try
            {
                string latest = await Task.Run(() => {
                    var req = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/TDUmii/UmiAchievementClaimer/releases/latest");
                    req.UserAgent = "UmiAchievementClaimer";
                    req.Timeout = 9000;
                    using (WebResponse response = req.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        var item = new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(reader.ReadToEnd());
                        return item != null && item.ContainsKey("tag_name") ? Convert.ToString(item["tag_name"]) : "";
                    }
                });
                if (IsDisposed || Disposing) return;
                Version remote;
                Version current = Assembly.GetExecutingAssembly().GetName().Version;
                string versionString = (latest ?? "").TrimStart('v', 'V');
                if (!Version.TryParse(versionString, out remote))
                    updateStatus.Text = english ? "No comparable public release." : "Chưa có bản phát hành có thể so sánh.";
                else if (remote > current)
                {
                    updateStatus.Text = (english ? "New release: " : "Bản mới: ") + latest;
                    if (MessageBox.Show(this,
                        english ? "Open the official GitHub Releases page?" : "Mở trang Releases GitHub chính thức?",
                        latest, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        Process.Start("https://github.com/TDUmii/UmiAchievementClaimer/releases/latest");
                }
                else updateStatus.Text = english ? "You are on the latest public version." : "Bạn đang dùng bản mới nhất đã phát hành.";
            }
            catch (Exception)
            {
                if (!IsDisposed && !Disposing) updateStatus.Text = english ? "Update unavailable. Check GitHub manually." : "Không kiểm tra được. Hãy xem trên GitHub.";
            }
            finally { if (!IsDisposed) checkUpdate.Enabled = true; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            WindowTheme.Apply(this);
        }
    }
}
