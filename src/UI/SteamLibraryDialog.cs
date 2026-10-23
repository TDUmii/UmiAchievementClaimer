using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
    // Keyboard-accessible searchable Steam library picker.
    internal sealed class SteamLibraryDialog : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wparam, string text);

        private readonly bool english;
        private readonly bool automaticSearch;
        private readonly TextBox query = new TextBox();
        private readonly Label searchPlaceholder = new Label();
        private readonly ListBox games = new FlatLibraryList();
        private readonly ThemedScrollBar libraryScroll = new ThemedScrollBar();
        private readonly Label heading = new Label();
        private readonly Label helper = new Label();
        private readonly Label count = new Label();
        private readonly Button choose = new UmiButton();
        private readonly Button manual = new UmiButton();
        private readonly Button cancel = new UmiButton();
        private List<SteamInstalledGame> allGames = new List<SteamInstalledGame>();
        private readonly Font gameTitleFont = UiTheme.DisplayFont(11F, FontStyle.Bold);
        private readonly Font gameMetaFont = UiTheme.Font(8.5F);

        internal string SelectedPath { get; private set; }

        internal SteamLibraryDialog(bool english) : this(english, true) { }

        internal SteamLibraryDialog(bool english, bool automaticSearch)
        {
            this.english = english;
            this.automaticSearch = automaticSearch;
            Text = english ? "Choose a Steam game" : "Chọn game Steam";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(480, 460);
            ClientSize = new Size(660, 585);
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = UiTheme.Canvas;
            ForeColor = UiTheme.Text;
            Font = UiTheme.Font(10F);
            Padding = new Padding(22);

            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 51));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 63));

            heading.Dock = DockStyle.Fill;
            heading.Font = UiTheme.DisplayFont(18F, FontStyle.Bold);
            heading.ForeColor = UiTheme.Text;
            heading.Text = english ? "Your Steam library" : "Thư viện Steam của bạn";
            layout.Controls.Add(heading, 0, 0);

            helper.Dock = DockStyle.Fill;
            helper.Font = UiTheme.Font(9F);
            helper.ForeColor = UiTheme.MutedText;
            helper.Text = english ? "Select an installed game. Search by name or AppID." : "Chọn game đã cài. Tìm theo tên hoặc AppID.";
            layout.Controls.Add(helper, 0, 1);

            Panel searchPanel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.SurfaceMuted, Margin = new Padding(0, 4, 0, 12), Padding = new Padding(12, 7, 12, 5) };
            query.BorderStyle = BorderStyle.None;
            query.BackColor = UiTheme.SurfaceMuted;
            query.ForeColor = UiTheme.Text;
            query.Font = UiTheme.Font(11F);
            query.Dock = DockStyle.Fill;
            query.AccessibleName = english ? "Search installed games" : "Tìm game đã cài";
            query.HandleCreated += delegate { SendMessage(query.Handle, 0x1501, new IntPtr(1), english ? "Search games or AppID..." : "Tìm game hoặc AppID..."); };
            query.TextChanged += delegate { searchPlaceholder.Visible = query.TextLength == 0 && !query.Focused; Filter(); };
            query.Enter += delegate { searchPlaceholder.Visible = false; };
            query.Leave += delegate { searchPlaceholder.Visible = query.TextLength == 0; };
            searchPlaceholder.Dock = DockStyle.Fill;
            searchPlaceholder.ForeColor = UiTheme.MutedText;
            searchPlaceholder.BackColor = UiTheme.SurfaceMuted;
            searchPlaceholder.TextAlign = ContentAlignment.MiddleLeft;
            searchPlaceholder.Font = UiTheme.Font(9.5F);
            searchPlaceholder.Text = english ? "Search games or AppID..." : "Tìm game hoặc AppID...";
            searchPlaceholder.Cursor = Cursors.IBeam;
            searchPlaceholder.Click += delegate { query.Focus(); };
            Label searchIcon = new Label { Dock = DockStyle.Left, Width = 25, Text = "⌕",
                TextAlign = ContentAlignment.MiddleLeft, BackColor = UiTheme.SurfaceMuted,
                ForeColor = UiTheme.MutedText, Font = UiTheme.DisplayFont(13F) };
            searchPanel.Controls.Add(query);
            searchPanel.Controls.Add(searchPlaceholder);
            searchPanel.Controls.Add(searchIcon);
            searchPlaceholder.BringToFront();
            searchIcon.BringToFront();
            layout.Controls.Add(searchPanel, 0, 2);

            Panel listPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            games.Dock = DockStyle.Fill;
            games.BorderStyle = BorderStyle.None;
            games.BackColor = UiTheme.Surface;
            games.ForeColor = UiTheme.Text;
            games.DrawMode = DrawMode.OwnerDrawFixed;
            games.ItemHeight = 64;
            games.IntegralHeight = false;
            games.Font = UiTheme.Font(10F);
            games.AccessibleName = english ? "Installed Steam games" : "Danh sách game Steam";
            games.DrawItem += DrawGame;
            games.SelectedIndexChanged += delegate { choose.Enabled = games.SelectedItem != null; UpdateLibraryScroll(); };
            games.Resize += delegate { UpdateLibraryScroll(); };
            games.MouseWheel += delegate { BeginInvoke(new Action(UpdateLibraryScroll)); };
            games.KeyUp += delegate { UpdateLibraryScroll(); };
            games.DoubleClick += delegate { ChooseSelected(); };
            games.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { ChooseSelected(); e.Handled = true; e.SuppressKeyPress = true; }
            };
            count.Dock = DockStyle.Bottom;
            count.Height = 30;
            count.ForeColor = UiTheme.MutedText;
            count.TextAlign = ContentAlignment.MiddleLeft;
            count.Font = UiTheme.Font(9F);
            count.Text = english ? "Finding installed games..." : "Đang tìm các game đã cài...";
            Panel scrollingContent = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
            libraryScroll.Dock = DockStyle.Right;
            libraryScroll.Width = 12;
            libraryScroll.AccessibleName = english ? "Steam library scrollbar" : "Thanh cuộn thư viện Steam";
            libraryScroll.Visible = false;
            libraryScroll.ValueChanged += delegate
            {
                if (games.Items.Count == 0) return;
                games.TopIndex = libraryScroll.Value;
                games.Invalidate();
            };
            scrollingContent.Controls.Add(games);
            scrollingContent.Controls.Add(libraryScroll);
            listPanel.Controls.Add(scrollingContent);
            listPanel.Controls.Add(count);
            layout.Controls.Add(listPanel, 0, 3);

            TableLayoutPanel actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 14, 0, 0) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));
            actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            UiTheme.StyleButton(manual, UmiButtonStyle.Ghost);
            UiTheme.StyleButton(cancel, UmiButtonStyle.Secondary);
            UiTheme.StyleButton(choose, UmiButtonStyle.Primary);
            manual.Dock = cancel.Dock = choose.Dock = DockStyle.Fill;
            manual.Margin = new Padding(0, 0, 8, 0);
            cancel.Margin = new Padding(0, 0, 8, 0);
            choose.Margin = new Padding(0);
            manual.Text = english ? "Browse folder instead" : "Chọn thư mục thủ công";
            cancel.Text = english ? "Cancel" : "Hủy";
            choose.Text = english ? "Scan game" : "Quét game";
            choose.Enabled = false;
            cancel.DialogResult = DialogResult.Cancel;
            manual.Click += delegate { DialogResult = DialogResult.Retry; Close(); };
            choose.Click += delegate { ChooseSelected(); };
            actions.Controls.Add(manual, 0, 0);
            actions.Controls.Add(cancel, 1, 0);
            actions.Controls.Add(choose, 2, 0);
            layout.Controls.Add(actions, 0, 4);
            Controls.Add(layout);
            AcceptButton = choose;
            CancelButton = cancel;

            Shown += async delegate
            {
                if (!automaticSearch) return;
                try
                {
                    List<SteamInstalledGame> discovered = await Task.Run(() => SteamLibraryCatalog.Discover());
                    if (!IsDisposed && !Disposing) Populate(discovered);
                }
                catch (Exception)
                {
                    if (!IsDisposed && !Disposing)
                        count.Text = english ? "Could not read the library. Browse a folder instead." : "Không đọc được thư viện. Hãy chọn thư mục.";
                }
            };
        }

        internal void Populate(IEnumerable<SteamInstalledGame> entries)
        {
            allGames = entries == null ? new List<SteamInstalledGame>() : entries.ToList();
            Filter();
        }

        private void Filter()
        {
            string search = query.Text.Trim();
            games.BeginUpdate();
            games.Items.Clear();
            foreach (SteamInstalledGame game in allGames)
            {
                if (search.Length == 0 ||
                    game.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    game.AppId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    games.Items.Add(game);
            }
            games.EndUpdate();
            if (games.Items.Count > 0) games.SelectedIndex = 0;
            choose.Enabled = games.SelectedItem != null;
            UpdateLibraryScroll();
            count.Text = allGames.Count == 0
                ? (english ? "No installed games found. Choose a folder manually." : "Không thấy game đã cài. Hãy chọn thư mục thủ công.")
                : string.Format(english ? "{0} of {1} installed games" : "{0}/{1} game đã cài", games.Items.Count, allGames.Count);
        }

        private void UpdateLibraryScroll()
        {
            if (games.IsDisposed || libraryScroll.IsDisposed) return;
            int visibleRows = Math.Max(1, games.ClientSize.Height / Math.Max(1, games.ItemHeight));
            libraryScroll.SetRange(0, Math.Max(0, games.Items.Count - 1), visibleRows, games.TopIndex);
        }

        private void DrawGame(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= games.Items.Count) return;
            SteamInstalledGame game = (SteamInstalledGame)games.Items[e.Index];
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (SolidBrush brush = new SolidBrush(selected ? UiTheme.SurfaceMuted : UiTheme.Surface))
                e.Graphics.FillRectangle(brush, e.Bounds);
            using (Pen line = new Pen(UiTheme.Border))
                e.Graphics.DrawLine(line, e.Bounds.Left + 8, e.Bounds.Bottom - 1, e.Bounds.Right - 8, e.Bounds.Bottom - 1);
            Rectangle name = new Rectangle(e.Bounds.Left + 15, e.Bounds.Top + 9, e.Bounds.Width - 30, 25);
            Rectangle meta = new Rectangle(e.Bounds.Left + 15, e.Bounds.Top + 36, e.Bounds.Width - 30, 22);
            TextRenderer.DrawText(e.Graphics, game.Name, gameTitleFont, name,
                UiTheme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            TextRenderer.DrawText(e.Graphics, "APP " + game.AppId + "    •    " + game.Folder, gameMetaFont, meta,
                selected ? UiTheme.Cyan : UiTheme.MutedText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            e.DrawFocusRectangle();
        }

        private void ChooseSelected()
        {
            SteamInstalledGame selected = games.SelectedItem as SteamInstalledGame;
            if (selected == null) return;
            SelectedPath = selected.Folder;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            gameTitleFont.Dispose();
            gameMetaFont.Dispose();
            base.OnFormClosed(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            WindowTheme.Apply(this);
        }
    }

    // Keep ListBox keyboard and wheel behavior while using the shared themed thumb.
    internal sealed class FlatLibraryList : ListBox
    {
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.Style &= ~0x00200000; // WS_VSCROLL
                return parameters;
            }
        }
    }
}
