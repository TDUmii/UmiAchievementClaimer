using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
// Steam Midnight. Operate: select a game, review, select explicitly, confirm.
// Navy surfaces, lavender actions, cyan navigation and mint status. Keep Steam workers isolated.
// First viewport: compact game summary, searchable achievement rows, fixed selection footer.
// Finish: native render review and DESIGN.md describe the shipped surface.
internal sealed class ClaimerForm : Form
{
    private readonly Panel headerPanel = new Panel();
    private readonly TableLayoutPanel pageLayout = new TableLayoutPanel();
    private readonly Panel gameCard = new Panel();
    private readonly Panel pathInputFrame = new Panel();
    private readonly Panel searchHost = new Panel();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wparam, string text);
    private void SetSearchCue()
    {
        if (searchBox.IsHandleCreated)
            SendMessage(searchBox.Handle, 0x1501, new IntPtr(1),
                L("Tìm achievement...", "Search achievements..."));
    }
    private readonly Panel achievementListHost = new Panel();
    private readonly CardPanel informationCard = new CardPanel();
    private readonly CardPanel achievementCard = new CardPanel();
    private readonly CardPanel logCard = new CardPanel();
    private readonly PictureBox brandLogo = new PictureBox();
    private readonly Label titleLabel = new Label();
    private readonly Label versionLabel = new Label();
    private readonly Panel versionHost = new Panel();
    private FlowLayoutPanel headerActions;
    private readonly Label subtitleLabel = new Label();
    private readonly Label steamStatusLabel = new Label();
    private readonly Label gameFolderTitleLabel = new Label();
    private readonly Label pathCueLabel = new Label();
    private readonly Label detectionTitleLabel = new Label();
    private readonly Label detectionStatusLabel = new Label();
    private readonly PictureBox gameCover = new PictureBox();
    private readonly Label coverFallback = new Label();
    private readonly Label safeModeLabel = new Label();
    private readonly TextBox pathBox = new TextBox();
    private readonly Button libraryButton = new UmiButton();
    private readonly Button browseButton = new UmiButton();
    private readonly Button scanButton = new UmiButton();
    private readonly Label[] captionLabels = new Label[5];
    private readonly Label gameValue = new Label();
    private readonly Label appIdValue = new Label();
    private readonly Label dllValue = new Label();
    private readonly Label architectureValue = new Label();
    private readonly Label interfaceValue = new Label();
    private readonly TextBox logBox = new TextBox();
    private readonly ThemedScrollBar logScrollBar = new ThemedScrollBar();
    private readonly Panel logViewport = new Panel();
    private readonly Button logToggleButton = new UmiButton();
    private readonly Button copyLogButton = new UmiButton();
    private readonly Label statusLabel = new Label();
    private readonly Label achievementTitleLabel = new Label();
    private readonly Label achievementHintLabel = new Label();
    private readonly Label achievementDescriptionLabel = new Label();
    private readonly AchievementGrid achievementList = new AchievementGrid();
    private readonly ThemedScrollBar achievementScrollBar = new ThemedScrollBar();
    private readonly Button selectAllButton = new UmiButton();
    private readonly Button selectLockedButton = new UmiButton();
    private readonly Button clearSelectionButton = new UmiButton();
    private readonly Button selectionMenuButton = new UmiButton();
    private readonly ContextMenuStrip selectionMenu = new ContextMenuStrip();
    private readonly Button claimButton = new UmiButton();
    private readonly Label selectedCountLabel = new Label();
    private readonly Label confirmationHintLabel = new Label();
    private readonly Label visibleCountLabel = new Label();
    private readonly Label emptyLabel = new Label();
    private readonly Label[] metricValues = new Label[3];
    private readonly Label[] metricCaptions = new Label[3];
    private readonly TextBox searchBox = new TextBox();
    private readonly Label searchHintLabel = new Label();
    private readonly Label searchPlaceholderLabel = new Label();
    private readonly Button[] filterButtons = new Button[3];
    private readonly Button detailsButton = new UmiButton();
    private readonly Panel technicalPanel = new Panel();
    private readonly Panel achievementProgressTrack = new Panel();
    private readonly Panel achievementProgressFill = new Panel();
    // Native Windows ProgressBar paints an unwanted white 1px seam at the log footer.
    // A regular panel is sufficient for this non-interactive activity indicator.
    private readonly Panel progress = new Panel();
    private readonly Button languageToggleButton = new UmiButton();
    private readonly Button themeButton = new UmiButton();
    private readonly Button settingsButton = new UmiButton();
    private readonly Button preflightButton = new UmiButton();
    private readonly Button insightsButton = new UmiButton();
    private string rarityAppId;
    private bool workerReportedEmptyGame;
    private readonly Button cancelWorkerButton = new UmiButton();
    private readonly Button sortButton = new UmiButton();
    private readonly ContextMenuStrip sortMenu = new ContextMenuStrip();
    private readonly System.Windows.Forms.Timer workerTimer = new System.Windows.Forms.Timer();
    private readonly Stopwatch workerClock = new Stopwatch();
    private string workerStopReason;
    private int lastVerifiedCount = -1;
    private int lastAcceptedCount;

    private readonly ToolTip informationToolTip = new ToolTip();
    private DetectedGame detectedGame;
    private Process workerProcess;
    private bool detectingGame;
    private string extractedWorker;
    private string extractedSteamApiDirectory;
    private string selectedAchievementFile;
    private string workerOperation;
    private int lastClaimCount;
    private int currentFilter;
    private bool logExpanded;
    private bool detailsExpanded;
    private string statusKey = "ready";
    private SteamArtwork steamArtwork;
    private readonly Dictionary<string, Image> achievementImages = new Dictionary<string, Image>(StringComparer.Ordinal);
    private readonly HashSet<string> pendingArtwork = new HashSet<string>(StringComparer.Ordinal);
    private int artworkGeneration;
    private bool english = true;
    private bool English { get { return english; } }

    public ClaimerForm()
    {
        string appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        Text = "Umi Achievement Claimer  v" + appVersion;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1220, 840);
        MinimumSize = new Size(1040, 760);
        Font = UiTheme.Font(9F);
        BackColor = UiTheme.Canvas;
        ForeColor = UiTheme.Text;
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;
        KeyPreview = true;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch { }
        BuildHeader();
        BuildPage();
        Controls.Add(pageLayout);
        Controls.Add(headerPanel);
        ApplyLanguage();
        languageToggleButton.Click += delegate { english = !english; ApplyLanguage(); };
        themeButton.Click += OpenSettings;
        settingsButton.Click += OpenSettings;
        UiTheme.Changed += ThemeChanged;
        SizeChanged += delegate
        {
            if (logExpanded && WindowState != FormWindowState.Minimized && IsHandleCreated)
                SetLogExpanded(true);
        };
        workerTimer.Interval = 1000;
        workerTimer.Tick += delegate
        {
            if (workerProcess != null && workerClock.Elapsed.TotalSeconds > (workerOperation == "selected" ? 120 : 90))
                StopWorker(true);
        };

        AppendLog(L("Sẵn sàng. Hãy chọn thư mục cài game Steam.", "Ready. Select a Steam game installation folder."));
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F) && searchBox.Enabled)
        {
            searchBox.Focus();
            searchBox.SelectAll();
            return true;
        }
        if (keyData == (Keys.Control | Keys.L) && libraryButton.Enabled)
        {
            OpenSteamLibrary(this, EventArgs.Empty);
            return true;
        }
        if (keyData == Keys.F5 && scanButton.Enabled)
        {
            DetectSelectedGame();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void BuildHeader()
    {
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Height = 86;
        headerPanel.BackColor = UiTheme.Canvas;
        headerPanel.Padding = new Padding(24, 12, 24, 12);
        brandLogo.Location = new Point(24, 14);
        brandLogo.Size = new Size(56, 56);
        brandLogo.SizeMode = PictureBoxSizeMode.Zoom;
        brandLogo.Image = LoadBrandLogo();
        titleLabel.Font = UiTheme.DisplayFont(18F, FontStyle.Bold);
        titleLabel.AutoSize = true;
        titleLabel.Location = new Point(96, 27);
        versionLabel.Text = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        versionLabel.AccessibleName = "Application version";
        versionLabel.TextAlign = ContentAlignment.MiddleRight;
        versionLabel.ForeColor = UiTheme.MutedText;
        versionLabel.Font = UiTheme.DisplayFont(9F, FontStyle.Bold);
        versionLabel.Height = 17;
        versionLabel.Dock = DockStyle.Bottom;
        subtitleLabel.Visible = false;
        languageToggleButton.Text = "EN";
        languageToggleButton.AccessibleName = "Language / Ngôn ngữ";
        languageToggleButton.Width = 44;
        languageToggleButton.Height = 32;
        ConfigureButton(languageToggleButton, UmiButtonStyle.Ghost);
        languageToggleButton.Padding = new Padding(2, 0, 2, 0);
        languageToggleButton.Visible = true;
        languageToggleButton.Location = new Point(-1000, -1000);
        languageToggleButton.Size = new Size(1, 1);
        themeButton.Text = "THEME";
        themeButton.AccessibleName = "Choose theme";
        themeButton.Width = 72;
        themeButton.Height = 32;
        ConfigureButton(themeButton, UmiButtonStyle.Ghost);
        themeButton.Padding = new Padding(3, 0, 3, 0);
        themeButton.Visible = true;
        themeButton.Location = new Point(-1000, -1000);
        themeButton.Size = new Size(1, 1);
        settingsButton.Text = "SETTINGS";
        settingsButton.AccessibleName = "Open settings";
        settingsButton.Width = 92;
        settingsButton.Height = 32;
        ConfigureButton(insightsButton, UmiButtonStyle.Secondary);
        insightsButton.Text = "INSIGHTS";
        insightsButton.AccessibleName = "View local achievement dashboard and recovery";
        insightsButton.Width = 94;
        insightsButton.Height = 32;
        insightsButton.Click += OpenInsights;
        ConfigureButton(preflightButton, UmiButtonStyle.Secondary);
        preflightButton.Text = "PREFLIGHT";
        preflightButton.Width = 96;
        preflightButton.Height = 32;
        preflightButton.Click += ShowPreflight;
        preflightButton.AccessibleName = "Check Steam compatibility and prerequisites";
        ConfigureButton(cancelWorkerButton, UmiButtonStyle.Secondary);
        cancelWorkerButton.Text = "CANCEL";
        cancelWorkerButton.Width = 84;
        cancelWorkerButton.Height = 32;
        cancelWorkerButton.Visible = false;
        cancelWorkerButton.Click += delegate { StopWorker(false); };

        ConfigureButton(settingsButton, UmiButtonStyle.Ghost);
        settingsButton.Padding = new Padding(4, 0, 4, 0);
        steamStatusLabel.Dock = DockStyle.Fill;
        steamStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
        steamStatusLabel.Font = UiTheme.DisplayFont(8.5F, FontStyle.Bold);
        FlowLayoutPanel actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 418,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
            Margin = new Padding(0)
        };
        // A dedicated vertical stack ensures that the version is visible
        // underneath Settings, rather than being covered by the docked action bar.
        versionHost.Size = new Size(98, 52);
        versionHost.Margin = new Padding(0);
        versionHost.BackColor = UiTheme.Canvas;
        settingsButton.Dock = DockStyle.Top;
        versionHost.Controls.Add(settingsButton);
        versionHost.Controls.Add(versionLabel);
        headerActions = actions;
        actions.Controls.Add(versionHost);
        actions.Controls.Add(cancelWorkerButton);
        actions.Controls.Add(preflightButton);
        actions.Controls.Add(insightsButton);
        Panel steamStatusHost = new Panel { Width = 108, Height = 32, Margin = new Padding(0, 0, 6, 0) };
        steamStatusHost.Controls.Add(steamStatusLabel);
        actions.Controls.Add(steamStatusHost);
        headerPanel.Controls.AddRange(new Control[] { brandLogo, titleLabel, subtitleLabel, actions, languageToggleButton, themeButton });
        headerPanel.Paint += delegate(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(UiTheme.Border))
                e.Graphics.DrawLine(pen, 24, headerPanel.Height - 1, headerPanel.Width - 24, headerPanel.Height - 1);
        };
    }

    private void BuildPage()
    {
        pageLayout.Dock = DockStyle.Fill;
        pageLayout.Padding = new Padding(18, 14, 18, 18);
        pageLayout.ColumnCount = 1;
        pageLayout.RowCount = 4;
        pageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 206F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
        BuildInformationCard();
        BuildAchievementCard();
        BuildLogCard();
        progress.Dock = DockStyle.Fill;
        progress.Margin = new Padding(0);
        progress.BackColor = UiTheme.Cyan;
        // Critical: a visible native ProgressBar occupies one pixel even when its
        // TableLayoutPanel row height is zero, creating the white line below Activity log.
        progress.Visible = false;
        pageLayout.Controls.Add(informationCard, 0, 0);
        pageLayout.Controls.Add(achievementCard, 0, 1);
        pageLayout.Controls.Add(logCard, 0, 2);
        pageLayout.Controls.Add(progress, 0, 3);
    }

    private void BuildGameCard()
    {
        gameCard.Dock = DockStyle.Fill;
        gameCard.Margin = new Padding(0);
        TableLayoutPanel pathLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Margin = new Padding(0) };
        pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        pathLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        pathLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pathInputFrame.Dock = DockStyle.Fill;
        pathInputFrame.BackColor = UiTheme.Canvas;
        pathInputFrame.Margin = new Padding(0, 0, 0, 7);
        pathInputFrame.Padding = new Padding(0);
        pathBox.Dock = DockStyle.None;
        pathBox.BorderStyle = BorderStyle.None;
        pathBox.BackColor = UiTheme.Canvas;
        pathBox.ForeColor = UiTheme.Text;
        pathBox.Font = UiTheme.MonoFont(9.5F);
        pathBox.Margin = new Padding(0);
        pathBox.ReadOnly = true;
        pathBox.TabStop = false;
        pathBox.Cursor = Cursors.Arrow;
        pathBox.MouseDown += delegate { browseButton.Focus(); };
        pathBox.KeyDown += delegate(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && scanButton.Enabled)
            {
                e.SuppressKeyPress = true;
                DetectSelectedGame();
            }
        };
        pathCueLabel.Dock = DockStyle.Fill;
        pathCueLabel.Padding = new Padding(10, 0, 8, 0);
        pathCueLabel.ForeColor = UiTheme.MutedText;
        pathCueLabel.TextAlign = ContentAlignment.MiddleLeft;
        pathInputFrame.Controls.Add(pathBox);
        pathInputFrame.Controls.Add(pathCueLabel);
        pathBox.TextChanged += delegate { pathCueLabel.Visible = string.IsNullOrWhiteSpace(pathBox.Text); };
        pathInputFrame.Resize += delegate { LayoutPathBox(); };
        LayoutPathBox();
        pathLayout.Controls.Add(pathInputFrame, 0, 0);
        pathLayout.SetColumnSpan(pathInputFrame, 3);

        ConfigureButton(libraryButton, UmiButtonStyle.Primary);
        libraryButton.Dock = DockStyle.Fill;
        libraryButton.Margin = new Padding(0, 0, 4, 0);
        libraryButton.Click += OpenSteamLibrary;
        ConfigureButton(browseButton, UmiButtonStyle.Secondary);
        browseButton.Dock = DockStyle.Fill;
        browseButton.Margin = new Padding(4, 0, 4, 0);
        browseButton.Click += BrowseForGame;
        ConfigureButton(scanButton, UmiButtonStyle.Secondary);
        scanButton.Dock = DockStyle.Fill;
        scanButton.Margin = new Padding(4, 0, 0, 0);
        scanButton.Click += delegate { DetectSelectedGame(); };
        scanButton.Enabled = false;
        pathBox.TextChanged += delegate { scanButton.Enabled = workerProcess == null && Directory.Exists(pathBox.Text); };
        pathLayout.Controls.Add(libraryButton, 0, 1);
        pathLayout.Controls.Add(browseButton, 1, 1);
        pathLayout.Controls.Add(scanButton, 2, 1);
        gameCard.Controls.Add(pathLayout);
        gameFolderTitleLabel.Visible = false;
    }

    private void LayoutPathBox()
    {
        int inset = 10;
        pathBox.Left = inset;
        pathBox.Width = Math.Max(20, pathInputFrame.ClientSize.Width - inset * 2);
        pathBox.Top = Math.Max(1, (pathInputFrame.ClientSize.Height - pathBox.Height) / 2);
    }

    private void UpdateAchievementScrollBar()
    {
        int total = achievementList.Rows.Count;
        if (total == 0)
        {
            achievementScrollBar.SetRange(0, 0, 1, 0);
            LayoutAchievementList();
            return;
        }

        int visible = achievementList.VisibleRows();

        int first = achievementList.FirstDisplayedScrollingRowIndex;
        if (first < 0) first = 0;
        achievementScrollBar.SetRange(0, Math.Max(0, total - 1), visible, first);
        LayoutAchievementList();
    }

    private void LayoutAchievementList()
    {
        int barWidth = achievementScrollBar.Visible ? 12 : 0;
        if (barWidth > 0) achievementScrollBar.Width = barWidth;
        int width = Math.Max(1, achievementListHost.ClientSize.Width - barWidth);
        int height = Math.Max(1, achievementListHost.ClientSize.Height);
        achievementList.Bounds = new Rectangle(0, 0, width, height);
        emptyLabel.Bounds = new Rectangle(0, 0, width, height);
        // Opening diagnostics reduces the list viewport; keep both lines of
        // the first-run/empty-state guidance legible instead of clipping text
        // inside the old fixed 20px vertical padding.
        emptyLabel.Padding = height < 145
            ? new Padding(10, 2, 10, 2)
            : new Padding(20);
        achievementScrollBar.Bounds = new Rectangle(width, 0, barWidth, height);
        achievementList.Visible = !emptyLabel.Visible;
        if (emptyLabel.Visible) emptyLabel.BringToFront();
        achievementScrollBar.BringToFront();
    }

    private void BuildInformationCard()
    {
        informationCard.Dock = DockStyle.Fill;
        informationCard.Margin = new Padding(0, 0, 0, 12);
        informationCard.Padding = new Padding(16);
        TableLayoutPanel summary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 328F));
        summary.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        Panel coverHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 16, 0), BackColor = UiTheme.SurfaceMuted };
        gameCover.Dock = DockStyle.Fill;
        gameCover.SizeMode = PictureBoxSizeMode.Zoom;
        gameCover.Visible = false;
        coverFallback.Dock = DockStyle.Fill;
        coverFallback.ForeColor = UiTheme.MutedText;
        coverFallback.TextAlign = ContentAlignment.MiddleCenter;
        coverHost.Controls.Add(coverFallback);
        coverHost.Controls.Add(gameCover);
        summary.Controls.Add(coverHost, 0, 0);

        TableLayoutPanel identity = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Margin = new Padding(0, 0, 14, 0) };
        identity.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        foreach (float height in new float[] { 32, 18, 45, 44, 7 })
            identity.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        identity.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        gameValue.Dock = DockStyle.Fill;
        gameValue.Font = UiTheme.DisplayFont(17F, FontStyle.Bold);
        gameValue.AutoEllipsis = true;
        gameValue.TextAlign = ContentAlignment.MiddleLeft;
        identity.Controls.Add(gameValue, 0, 0);
        detectionStatusLabel.Dock = DockStyle.Fill;
        detectionStatusLabel.Font = UiTheme.DisplayFont(9F, FontStyle.Bold);
        detectionStatusLabel.ForeColor = UiTheme.Success;
        detectionStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        identity.Controls.Add(detectionStatusLabel, 0, 1);

        TableLayoutPanel metadata = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        metadata.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        metadata.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
        metadata.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
        metadata.Controls.Add(CreateInformationCell(1, appIdValue), 0, 0);
        metadata.Controls.Add(CreateInformationCell(3, architectureValue), 1, 0);
        identity.Controls.Add(metadata, 0, 2);

        TableLayoutPanel metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 4, 0, 0) };
        metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 3; i++)
        {
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            Panel cell = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            metricCaptions[i] = new Label { Dock = DockStyle.Top, Height = 17, ForeColor = UiTheme.MutedText, Font = UiTheme.Font(8.4F) };
            metricValues[i] = new Label { Dock = DockStyle.Fill, Font = UiTheme.DisplayFont(14.5F, FontStyle.Bold),
                ForeColor = i == 1 ? UiTheme.Success : i == 2 ? UiTheme.Warning : UiTheme.Text };
            cell.Controls.Add(metricValues[i]);
            cell.Controls.Add(metricCaptions[i]);
            metrics.Controls.Add(cell, i, 0);
        }
        identity.Controls.Add(metrics, 0, 3);
        achievementProgressTrack.Dock = DockStyle.Fill;
        achievementProgressTrack.Margin = new Padding(0, 1, 16, 1);
        achievementProgressTrack.BackColor = UiTheme.Border;
        achievementProgressFill.BackColor = UiTheme.Success;
        achievementProgressTrack.Controls.Add(achievementProgressFill);
        achievementProgressTrack.Resize += delegate { UpdateAchievementProgress(); };
        identity.Controls.Add(achievementProgressTrack, 0, 4);
        safeModeLabel.Dock = DockStyle.Fill;
        safeModeLabel.ForeColor = UiTheme.MutedText;
        safeModeLabel.Font = UiTheme.Font(8F);
        safeModeLabel.TextAlign = ContentAlignment.MiddleLeft;
        identity.Controls.Add(safeModeLabel, 0, 5);
        summary.Controls.Add(identity, 1, 0);

        TableLayoutPanel controls = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        controls.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
        detectionTitleLabel.Dock = DockStyle.Fill;
        detectionTitleLabel.Font = UiTheme.DisplayFont(8.5F, FontStyle.Bold);
        detectionTitleLabel.ForeColor = UiTheme.MutedText;
        controls.Controls.Add(detectionTitleLabel, 0, 0);
        Panel gameActionsHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 9) };
        BuildGameCard();
        gameActionsHost.Controls.Add(gameCard);
        technicalPanel.Dock = DockStyle.Fill;
        technicalPanel.BackColor = UiTheme.Surface;
        technicalPanel.Visible = false;
        TableLayoutPanel technicalRows = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        technicalRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        technicalRows.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        technicalRows.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        technicalRows.Controls.Add(CreateInformationCell(2, dllValue), 0, 0);
        technicalRows.Controls.Add(CreateInformationCell(4, interfaceValue), 0, 1);
        technicalPanel.Controls.Add(technicalRows);
        gameActionsHost.Controls.Add(technicalPanel);
        controls.Controls.Add(gameActionsHost, 0, 1);
        ConfigureButton(detailsButton, UmiButtonStyle.Ghost);
        detailsButton.Dock = DockStyle.Fill;
        detailsButton.Margin = new Padding(0);
        detailsButton.ForeColor = UiTheme.Cyan;
        detailsButton.Click += delegate
        {
            detailsExpanded = !detailsExpanded;
            gameCard.Visible = !detailsExpanded;
            technicalPanel.Visible = detailsExpanded;
            if (detailsExpanded) technicalPanel.BringToFront();
            UpdateDetailsLabel();
        };
        controls.Controls.Add(detailsButton, 0, 2);
        summary.Controls.Add(controls, 2, 0);
        informationCard.Controls.Add(summary);
    }


    private void BuildAchievementCard()
    {
        achievementCard.Dock = DockStyle.Fill;
        achievementCard.Margin = new Padding(0);
        achievementCard.Padding = new Padding(18, 14, 18, 12);
        achievementCard.MouseDown += delegate { ClearMouseFocus(); };
        TableLayoutPanel workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Margin = new Padding(0) };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 49F));
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F));
        Panel heading = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        achievementTitleLabel.Dock = DockStyle.Top;
        achievementTitleLabel.Height = 30;
        achievementTitleLabel.Font = UiTheme.DisplayFont(18F, FontStyle.Bold);
        achievementHintLabel.Dock = DockStyle.Bottom;
        achievementHintLabel.Height = 19;
        achievementHintLabel.ForeColor = UiTheme.MutedText;
        achievementHintLabel.Font = UiTheme.Font(9F);
        heading.Controls.Add(achievementTitleLabel);
        heading.Controls.Add(achievementHintLabel);
        ConfigureButton(sortButton, UmiButtonStyle.Ghost);
        sortButton.Size = new Size(154, 29);
        sortButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        sortButton.Location = new Point(heading.Width - 154, 0);
        sortButton.Click += ShowSortMenu;
        sortMenu.Renderer = new UmiToolStripRenderer();
        sortMenu.ShowImageMargin = false;
        workspace.Controls.Add(heading, 0, 0);

        TableLayoutPanel toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0, 4, 0, 9) };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270F));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 154F));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142F));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        searchHost.Dock = DockStyle.Fill;
        searchHost.BackColor = UiTheme.Canvas;
        searchHost.Padding = new Padding(11, 5, 8, 4);
        searchHost.Margin = new Padding(0, 0, 9, 0);
        searchHintLabel.Dock = DockStyle.Left;
        searchHintLabel.Width = 26;
        searchHintLabel.ForeColor = UiTheme.MutedText;
        searchHintLabel.Font = UiTheme.DisplayFont(12F);
        searchBox.Dock = DockStyle.Fill;
        searchBox.BorderStyle = BorderStyle.None;
        searchBox.BackColor = UiTheme.Canvas;
        searchBox.ForeColor = UiTheme.Text;
        searchBox.Font = UiTheme.Font(10F);
        searchPlaceholderLabel.Dock = DockStyle.None;
        searchPlaceholderLabel.ForeColor = UiTheme.MutedText;
        searchPlaceholderLabel.Font = UiTheme.Font(9.5F);
        searchPlaceholderLabel.TextAlign = ContentAlignment.MiddleLeft;
        searchPlaceholderLabel.Cursor = Cursors.IBeam;
        searchPlaceholderLabel.Click += delegate { searchBox.Focus(); };
        searchPlaceholderLabel.Visible = false;
        searchBox.HandleCreated += delegate { SetSearchCue(); };
        searchBox.Enter += delegate { searchPlaceholderLabel.Visible = false; };
        searchBox.Leave += delegate { searchPlaceholderLabel.Visible = false; };
        searchHost.Resize += delegate { LayoutSearch(); };
        searchHost.MouseDown += delegate { searchBox.Focus(); };
        searchBox.TextChanged += delegate
        {
            searchPlaceholderLabel.Visible = false;
            ApplyAchievementFilter();
        };
        searchHost.Controls.Add(searchBox);
        searchHost.Controls.Add(searchPlaceholderLabel);
        searchHost.Controls.Add(searchHintLabel);
        LayoutSearch();
        toolbar.Controls.Add(searchHost, 0, 0);

        TableLayoutPanel filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 0, 8, 0), BackColor = UiTheme.SurfaceMuted };
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37.5F));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37.5F));
        for (int i = 0; i < 3; i++)
        {
            int filter = i;
            filterButtons[i] = new UmiButton { Dock = DockStyle.Fill, Margin = new Padding(2), Padding = new Padding(1, 0, 1, 0) };
            ConfigureButton(filterButtons[i], UmiButtonStyle.Ghost);
            filterButtons[i].Font = UiTheme.Font(9F);
            filterButtons[i].Click += delegate { currentFilter = filter; ApplyAchievementFilter(); ClearMouseFocus(); };
            filters.Controls.Add(filterButtons[i], i, 0);
        }
        toolbar.Controls.Add(filters, 1, 0);

        Button[] bulkButtons = { selectAllButton, selectLockedButton, clearSelectionButton };
        foreach (Button button in bulkButtons)
        {
            ConfigureButton(button, UmiButtonStyle.Ghost);
            button.Visible = true;
            button.Enabled = false;
            button.Dock = DockStyle.None;
            button.Location = new Point(-1000, -1000);
            button.Size = new Size(1, 1);
            button.TabStop = false;
        }
        ConfigureButton(selectionMenuButton, UmiButtonStyle.Secondary);
        selectionMenuButton.Font = UiTheme.Font(9F, FontStyle.Bold);
        selectionMenuButton.Dock = DockStyle.Fill;
        selectionMenuButton.Margin = new Padding(0);
        selectionMenuButton.Click += ShowSelectionMenu;
        selectionMenu.Renderer = new UmiToolStripRenderer();
        selectionMenu.ShowImageMargin = false;
        sortButton.Dock = DockStyle.Fill;
        sortButton.Margin = new Padding(0, 0, 8, 0);
        toolbar.Controls.Add(sortButton, 2, 0);
        toolbar.Controls.Add(selectionMenuButton, 3, 0);
        selectAllButton.Click += delegate { SetAchievementChecks(delegate { return true; }); };
        selectLockedButton.Click += delegate { SetAchievementChecks(item => !item.Achieved); };
        clearSelectionButton.Click += delegate { achievementList.ClearChecks(); };
        workspace.Controls.Add(toolbar, 0, 1);
        visibleCountLabel.Visible = false;
        Panel listHost = achievementListHost;
        listHost.Dock = DockStyle.Fill;
        listHost.Margin = new Padding(0);
        listHost.BackColor = UiTheme.Surface;
        achievementList.Dock = DockStyle.None;
        achievementList.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        achievementList.ArtworkProvider = GetAchievementImage;
        achievementList.ChecksChanged += delegate { UpdateClaimButtonState(); };
        achievementList.SelectedItemChanged += delegate { UpdateAchievementDescription(); };
        achievementList.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || !achievementList.Enabled) return;
            AchievementItem selected = achievementList.Rows[e.RowIndex].Tag as AchievementItem;
            if (selected == null) return;
            using (AchievementInspector details = new AchievementInspector(selected, GetAchievementImage(selected), English))
                details.ShowDialog(this);
        };
        achievementList.Scroll += delegate { UpdateAchievementScrollBar(); QueueVisibleArtwork(); };
        achievementList.Resize += delegate { UpdateAchievementScrollBar(); QueueVisibleArtwork(); };
        achievementList.RowsAdded += delegate { UpdateAchievementScrollBar(); };
        achievementList.RowsRemoved += delegate { UpdateAchievementScrollBar(); };
        achievementList.MouseWheel += delegate(object sender, MouseEventArgs e) { achievementList.ScrollBy(e.Delta); UpdateAchievementScrollBar(); QueueVisibleArtwork(); };
        emptyLabel.Dock = DockStyle.None;
        emptyLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        emptyLabel.ForeColor = UiTheme.MutedText;
        emptyLabel.BackColor = UiTheme.Surface;
        emptyLabel.Font = UiTheme.DisplayFont(11F);
        emptyLabel.Padding = new Padding(20);
        achievementScrollBar.Dock = DockStyle.None;
        achievementScrollBar.Width = 12;
        achievementScrollBar.Margin = new Padding(0);
        achievementScrollBar.TabStop = true;
        achievementScrollBar.Visible = false;
        achievementScrollBar.ValueChanged += delegate
        {
            if (achievementList.Rows.Count == 0)
                return;
            try { achievementList.FirstDisplayedScrollingRowIndex = achievementScrollBar.Value; }
            catch (ArgumentOutOfRangeException) { }
            QueueVisibleArtwork();
        };
        listHost.Controls.Add(achievementList);
        listHost.Controls.Add(emptyLabel);
        listHost.Controls.Add(achievementScrollBar);
        listHost.Resize += delegate { LayoutAchievementList(); UpdateAchievementScrollBar(); };
        LayoutAchievementList();
        achievementScrollBar.BringToFront();
        workspace.Controls.Add(listHost, 0, 2);
        achievementDescriptionLabel.Dock = DockStyle.Fill;
        achievementDescriptionLabel.Font = UiTheme.Font(8.5F);
        achievementDescriptionLabel.ForeColor = UiTheme.MutedText;
        achievementDescriptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        achievementDescriptionLabel.AutoEllipsis = true;
        workspace.Controls.Add(achievementDescriptionLabel, 0, 3);
        Panel footer = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(0, 12, 0, 0) };
        ConfigureButton(claimButton, UmiButtonStyle.Accent);
        claimButton.Width = 194;
        claimButton.Click += ClaimAchievements;
        claimButton.EnabledChanged += delegate { RefreshClaimButtonStyle(); };
        Panel claimHost = new Panel { Dock = DockStyle.Right, Width = 194, Padding = new Padding(0, 0, 0, 0), Margin = new Padding(0) };
        claimButton.Dock = DockStyle.Fill;
        claimHost.Controls.Add(claimButton);
        Panel selection = new Panel { Dock = DockStyle.Fill };
        selectedCountLabel.Dock = DockStyle.Top;
        selectedCountLabel.Height = 24;
        selectedCountLabel.Font = UiTheme.DisplayFont(11F, FontStyle.Bold);
        confirmationHintLabel.Dock = DockStyle.Fill;
        confirmationHintLabel.ForeColor = UiTheme.MutedText;
        confirmationHintLabel.Font = UiTheme.Font(8.5F);
        selection.Controls.Add(confirmationHintLabel);
        selection.Controls.Add(selectedCountLabel);
        footer.Controls.Add(selection);
        footer.Controls.Add(claimHost);
        footer.Paint += delegate(object sender, PaintEventArgs e) { using (Pen pen = new Pen(UiTheme.Border)) e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0); };
        workspace.Controls.Add(footer, 0, 4);
        achievementCard.Controls.Add(workspace);
        achievementCard.Controls.Add(selectAllButton);
        achievementCard.Controls.Add(selectLockedButton);
        achievementCard.Controls.Add(clearSelectionButton);
        SetAchievementControlsEnabled(false);
        claimButton.Enabled = false;
    }

    private void LayoutSearch()
    {
        int left = searchHost.Padding.Left + searchHintLabel.Width + 4;
        searchBox.Top = Math.Max(1, (searchHost.ClientSize.Height - searchBox.Height) / 2);
        searchPlaceholderLabel.Bounds = new Rectangle(left, searchBox.Top,
            Math.Max(10, searchHost.ClientSize.Width - left - 8), searchBox.Height);
        searchPlaceholderLabel.SendToBack();
    }

    private void ClearMouseFocus()
    {
        if (IsDisposed || Disposing) return;
        ActiveControl = null;
    }

    private void BuildLogCard()
    {
        logCard.Dock = DockStyle.Fill;
        logCard.Margin = new Padding(0, 10, 0, 0);
        logCard.Padding = new Padding(10, 4, 10, 4);
        Panel logHeader = new Panel { Dock = DockStyle.Top, Height = 34 };
        ConfigureButton(logToggleButton, UmiButtonStyle.Ghost);
        logToggleButton.Dock = DockStyle.Left;
        logToggleButton.Width = 210;
        logToggleButton.Click += delegate { SetLogExpanded(!logExpanded); ClearMouseFocus(); };
        ConfigureButton(copyLogButton, UmiButtonStyle.Ghost);
        copyLogButton.Dock = DockStyle.Right;
        copyLogButton.Width = 106;
        copyLogButton.Click += delegate
        {
            try { if (logBox.TextLength > 0) Clipboard.SetText(logBox.Text); }
            catch (System.Runtime.InteropServices.ExternalException) { AppendLog(L("Clipboard đang bận. Hãy thử sao chép lại.", "Clipboard is busy. Try copying again.")); }
        };
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleRight;
        statusLabel.ForeColor = UiTheme.Success;
        logHeader.Controls.Add(statusLabel);
        logHeader.Controls.Add(logToggleButton);
        logHeader.Controls.Add(copyLogButton);
        logBox.Dock = DockStyle.None;
        logBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        logBox.ReadOnly = true;
        logBox.Multiline = true;
        logBox.ScrollBars = ScrollBars.None;
        logBox.WordWrap = false;
        logBox.BorderStyle = BorderStyle.None;
        logBox.BackColor = UiTheme.SurfaceMuted;
        logBox.ForeColor = UiTheme.Text; // Must apply BEFORE first expansion, not only after theme changes.
        logBox.Font = new Font("Consolas", 10.0F);
        logViewport.Dock = DockStyle.Fill;
        logViewport.Margin = new Padding(0);
        logViewport.Padding = new Padding(10, 8, 7, 8);
        logViewport.BackColor = UiTheme.SurfaceMuted;
        logScrollBar.Width = 12;
        logScrollBar.Visible = false;
        logScrollBar.TabStop = true;
        logScrollBar.AccessibleName = "Activity log scrollbar";
        logScrollBar.ValueChanged += delegate
        {
            if (!logBox.IsHandleCreated) return;
            int line = Math.Max(0, Math.Min(logBox.Lines.Length - 1, logScrollBar.Value));
            logBox.SelectionStart = logBox.GetFirstCharIndexFromLine(line);
            logBox.SelectionLength = 0;
            logBox.ScrollToCaret();
        };
        logBox.MouseWheel += delegate(object sender, MouseEventArgs e)
        {
            logScrollBar.Value = logScrollBar.Value - Math.Sign(e.Delta) * 3;
        };
        logViewport.Resize += delegate { LayoutLog(); };
        logViewport.Controls.Add(logBox);
        logViewport.Controls.Add(logScrollBar);
        logCard.Controls.Add(logViewport);
        logViewport.Visible = false;
        logCard.Controls.Add(logHeader);
    }

    private void LayoutLog()
    {
        int width = Math.Max(1, logViewport.ClientSize.Width - logViewport.Padding.Horizontal);
        int height = Math.Max(1, logViewport.ClientSize.Height - logViewport.Padding.Vertical);
        int lines = Math.Max(0, logBox.Lines.Length);
        int visible = Math.Max(1, height / Math.Max(1, logBox.Font.Height));
        logScrollBar.SetRange(0, Math.Max(0, lines - 1), visible, Math.Min(logScrollBar.Value, Math.Max(0, lines - visible)));
        int bar = logScrollBar.Visible ? 12 : 0;
        logBox.Bounds = new Rectangle(logViewport.Padding.Left, logViewport.Padding.Top, Math.Max(1, width - bar - 4), height);
        logScrollBar.Bounds = new Rectangle(logViewport.Padding.Left + width - bar, logViewport.Padding.Top, bar, height);
        logScrollBar.BringToFront();
    }

    private static Image LoadBrandLogo()
    {
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UmiBrandLogo"))
        {
            if (stream == null) return null;
            using (Image image = Image.FromStream(stream)) return new Bitmap(image);
        }
    }

    private void OpenSettings(object sender, EventArgs e)
    {
        using (SettingsDialog dialog = new SettingsDialog(English, delegate(bool value)
        {
            english = value;
            ApplyLanguage();
        }))
            dialog.ShowDialog(this);
    }

    private void ShowSelectionMenu(object sender, EventArgs e)
    {
        selectionMenu.Items.Clear();
        selectionMenu.BackColor = UiTheme.Surface;
        selectionMenu.ForeColor = UiTheme.Text;
        AddSelectionMenuItem(L("Chọn đang hiện", "Select visible"), delegate { SetAchievementChecks(delegate { return true; }); });
        AddSelectionMenuItem(L("Chọn chưa mở", "Select locked"), delegate { SetAchievementChecks(item => !item.Achieved); });
        AddSelectionMenuItem(L("Bỏ chọn hết", "Clear all"), delegate { achievementList.ClearChecks(); });
        selectionMenu.Show(selectionMenuButton, new Point(0, selectionMenuButton.Height));
        ClearMouseFocus();
    }

    private void ShowSortMenu(object sender, EventArgs e)
    {
        sortMenu.Items.Clear();
        string[] descriptions = English
            ? new[] { "Default order", "Name A-Z", "Locked first", "Unlocked first" }
            : new[] { "Thứ tự gốc", "Tên A-Z", "Chưa mở trước", "Đã mở trước" };
        for (int index = 0; index < descriptions.Length; index++)
        {
            int chosen = index;
            ToolStripMenuItem item = new ToolStripMenuItem(descriptions[index]);
            item.Checked = achievementList.SortMode == index;
            item.ForeColor = UiTheme.Text;
            item.BackColor = UiTheme.Surface;
            item.Click += delegate { achievementList.SortMode = chosen; UpdateSortButton(); };
            sortMenu.Items.Add(item);
        }
        sortMenu.Show(sortButton, new Point(0, sortButton.Height));
    }

    private void UpdateSortButton()
    {
        string[] modes = English
            ? new[] { "Default", "Name A-Z", "Locked first", "Unlocked first" }
            : new[] { "Mặc định", "Tên A-Z", "Chưa mở", "Đã mở" };
        sortButton.Text = L("Sắp xếp: ", "Sort: ") + modes[achievementList.SortMode] + " ▾";
    }

    private void ShowPreflight(object sender, EventArgs e)
    {
        bool steamRunning = Process.GetProcessesByName("steam").Length > 0;
        var details = SteamPreflight.Inspect(detectedGame, steamRunning);
        MessageBox.Show(this, string.Join(Environment.NewLine + Environment.NewLine, details),
            L("Kiểm tra khả năng tương thích", "Compatibility preflight"), MessageBoxButtons.OK,
            details.Any(line => line.StartsWith("[!]")) ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    private void AddSelectionMenuItem(string text, EventHandler action)
    {
        ToolStripItem item = selectionMenu.Items.Add(text);
        item.ForeColor = UiTheme.Text;
        item.BackColor = UiTheme.Surface;
        item.Click += action;
    }

    private void ConfigureButton(Button button, UmiButtonStyle style) { UiTheme.StyleButton(button, style); button.Height = 30; }

    private Control CreateInformationCell(int captionIndex, Label value)
    {
        Panel cell = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 0) };
        Label caption = new Label { Dock = DockStyle.Top, Height = 22, Font = UiTheme.Font(8F), ForeColor = UiTheme.MutedText };
        captionLabels[captionIndex] = caption;
        value.Dock = DockStyle.Fill;
        value.Font = UiTheme.Font(10F, FontStyle.Bold);
        value.AutoEllipsis = true;
        cell.Controls.Add(value);
        cell.Controls.Add(caption);
        return cell;
    }

    private string L(string vietnamese, string english) { return English ? english : vietnamese; }

    private void ApplyLanguage()
    {
        Text = "Umi Achievement Claimer  v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        titleLabel.Text = "Umi Achievement Claimer";
        subtitleLabel.Text = string.Empty;
        subtitleLabel.Visible = false;
        gameFolderTitleLabel.Text = L("Thư mục cài game Steam", "Steam game installation folder");
        pathCueLabel.Text = L("CHỌN THƯ MỤC GAME", "SELECT GAME FOLDER");
        libraryButton.Text = L("Thư viện", "My games");
        browseButton.Text = L("Thư mục", "Browse");
        libraryButton.AccessibleName = L("Chọn game từ thư viện Steam", "Choose an installed Steam game");
        informationToolTip.SetToolTip(libraryButton, libraryButton.AccessibleName + " (Ctrl+L)");
        informationToolTip.SetToolTip(scanButton, L("Quét lại game hiện tại (F5)", "Rescan the current game (F5)"));
        informationToolTip.SetToolTip(searchBox, L("Tìm achievement (Ctrl+F)", "Search achievements (Ctrl+F)"));
        scanButton.Text = L("Quét lại", "Rescan");
        pathBox.AccessibleName = gameFolderTitleLabel.Text;
        detectionTitleLabel.Text = L("THƯ MỤC GAME STEAM", "STEAM GAME FOLDER");
        captionLabels[1].Text = "STEAM APP ID";
        captionLabels[2].Text = "STEAM API";
        captionLabels[3].Text = L("KIẾN TRÚC", "ARCHITECTURE");
        captionLabels[4].Text = "USER STATS";
        safeModeLabel.Text = L("Không có thay đổi nào trước khi xác nhận", "Nothing changes until confirmed");
        coverFallback.Text = detectedGame == null ? L("Chọn một game Steam", "Choose a Steam game") : L("Không có ảnh game", "Game artwork unavailable");
        achievementTitleLabel.Text = "Achievements";
        achievementHintLabel.Text = L("Tìm, lọc và lựa chọn thành tích cần mở", "Search, filter and select the achievements you want");
        selectAllButton.Text = L("Chọn đang hiện", "Select visible");
        selectLockedButton.Text = L("Chọn chưa mở", "Select locked");
        clearSelectionButton.Text = L("Bỏ chọn hết", "Clear all");
        selectionMenuButton.Text = L("Chọn nhanh  ▾", "Quick select  ▾");
        claimButton.Text = L("Claim đã chọn", "Claim selected");
        copyLogButton.Text = L("Sao chép log", "Copy log");
        metricCaptions[0].Text = L("Tổng cộng", "Total");
        metricCaptions[1].Text = L("Đã mở", "Unlocked");
        metricCaptions[2].Text = L("Chưa mở", "Locked");
        filterButtons[0].Text = L("Tất cả", "All");
        filterButtons[1].Text = L("Chưa mở", "Locked");
        filterButtons[2].Text = L("Đã mở", "Unlocked");
        searchHintLabel.Text = "⌕";
        searchPlaceholderLabel.Text = L("Tìm achievement...", "Search achievements...");
        SetSearchCue();
        searchBox.AccessibleName = L("Tìm tên, mô tả hoặc API ID", "Search name, description or API ID");
        informationToolTip.SetToolTip(searchBox, searchBox.AccessibleName);
        languageToggleButton.Text = English ? "EN" : "VI";
        languageToggleButton.AccessibleDescription = English ? "Switch to Vietnamese" : "Chuyển sang English";
        settingsButton.Text = L("CÀI ĐẶT", "SETTINGS");
        preflightButton.Text = L("KIỂM TRA", "PREFLIGHT");
        insightsButton.Text = L("THỐNG KÊ", "INSIGHTS");
        cancelWorkerButton.Text = L("HỦY", "CANCEL");
        UpdateSortButton();
        settingsButton.AccessibleDescription = L("Mở cài đặt giao diện và ngôn ngữ", "Open language and theme settings");
        informationToolTip.SetToolTip(settingsButton, settingsButton.AccessibleDescription);
        informationToolTip.SetToolTip(themeButton, L("Đổi theme giao diện", "Change interface theme") + "  /  " + UiTheme.ActiveName);
        informationToolTip.SetToolTip(selectAllButton, L("Chọn tất cả mục đang hiện. Mục ẩn giữ nguyên.", "Select all visible results. Hidden selections are kept."));
        informationToolTip.SetToolTip(selectLockedButton, L("Chỉ chọn mục chưa mở trong kết quả đang hiện. Mục ẩn giữ nguyên.", "Select locked achievements in visible results. Hidden selections are kept."));
        achievementList.English = English;
        foreach (AchievementItem item in achievementList.Items) item.English = English;
        SetInformation(detectedGame);
        UpdateDetailsLabel();
        UpdateLogLabel();
        ApplyAchievementFilter();
        UpdateStatus(statusKey);
    }

    private void ThemeChanged(object sender, EventArgs e)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(ApplyTheme));
            return;
        }
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        BackColor = UiTheme.Canvas;
        ForeColor = UiTheme.Text;
        headerPanel.BackColor = UiTheme.Canvas;
        versionHost.BackColor = UiTheme.Canvas;
        pageLayout.BackColor = UiTheme.Canvas;
        gameCard.BackColor = UiTheme.Surface;
        pathInputFrame.BackColor = UiTheme.SurfaceMuted;
        pathBox.BackColor = UiTheme.SurfaceMuted;
        pathBox.ForeColor = UiTheme.Text;
        pathCueLabel.ForeColor = UiTheme.MutedText;
        searchHost.BackColor = UiTheme.Canvas;
        searchBox.BackColor = UiTheme.Canvas;
        searchBox.ForeColor = UiTheme.Text;
        achievementListHost.BackColor = UiTheme.Surface;
        informationCard.BackColor = UiTheme.Surface;
        achievementCard.BackColor = UiTheme.Surface;
        logCard.BackColor = UiTheme.Surface;
        gameCover.BackColor = UiTheme.SurfaceMuted;
        coverFallback.ForeColor = UiTheme.MutedText;
        titleLabel.ForeColor = UiTheme.Text;
        versionLabel.ForeColor = UiTheme.Cyan;
        steamStatusLabel.ForeColor = statusKey == "failed" ? UiTheme.Warning : UiTheme.Success;
        detectionTitleLabel.ForeColor = UiTheme.Primary;
        detectionStatusLabel.ForeColor = detectedGame == null ? UiTheme.MutedText : UiTheme.Success;
        gameValue.ForeColor = UiTheme.Text;
        safeModeLabel.ForeColor = UiTheme.Success;
        achievementTitleLabel.ForeColor = UiTheme.Text;
        achievementHintLabel.ForeColor = UiTheme.MutedText;
        achievementDescriptionLabel.ForeColor = UiTheme.MutedText;
        searchHintLabel.ForeColor = UiTheme.MutedText;
        visibleCountLabel.ForeColor = UiTheme.MutedText;
        selectedCountLabel.ForeColor = UiTheme.Text;
        confirmationHintLabel.ForeColor = UiTheme.MutedText;
        emptyLabel.ForeColor = UiTheme.MutedText;
        emptyLabel.BackColor = UiTheme.Surface;
        for (int i = 0; i < metricValues.Length; i++)
            metricValues[i].ForeColor = i == 1 ? UiTheme.Success : i == 2 ? UiTheme.Warning : UiTheme.Text;
        for (int i = 0; i < captionLabels.Length; i++)
            if (captionLabels[i] != null) captionLabels[i].ForeColor = UiTheme.MutedText;

        UiTheme.StyleButton(libraryButton, UmiButtonStyle.Primary);
        UiTheme.StyleButton(browseButton, UmiButtonStyle.Secondary);
        UiTheme.StyleButton(scanButton, UmiButtonStyle.Secondary);
        UiTheme.StyleButton(detailsButton, UmiButtonStyle.Ghost);
        UiTheme.StyleButton(selectAllButton, UmiButtonStyle.Ghost);
        UiTheme.StyleButton(selectLockedButton, UmiButtonStyle.Ghost);
        UiTheme.StyleButton(clearSelectionButton, UmiButtonStyle.Ghost);
        UiTheme.StyleButton(selectionMenuButton, UmiButtonStyle.Ghost);
        UiTheme.StyleButton(claimButton, UmiButtonStyle.Accent);
        UiTheme.StyleButton(logToggleButton, UmiButtonStyle.Ghost);
        UiTheme.StyleButton(copyLogButton, UmiButtonStyle.Ghost);
        UiTheme.StyleButton(languageToggleButton, UmiButtonStyle.Ghost);
        UiTheme.StyleButton(themeButton, UmiButtonStyle.Ghost);
        UiTheme.StyleButton(settingsButton, UmiButtonStyle.Ghost);
        UiTheme.StyleButton(preflightButton, UmiButtonStyle.Secondary);
        UiTheme.StyleButton(insightsButton, UmiButtonStyle.Secondary);
        UiTheme.StyleButton(cancelWorkerButton, UmiButtonStyle.Secondary);
        UiTheme.StyleButton(sortButton, UmiButtonStyle.Ghost);
        foreach (Button filter in filterButtons)
            if (filter != null) UiTheme.StyleButton(filter, UmiButtonStyle.Ghost);
        logBox.BackColor = UiTheme.SurfaceMuted;
        logViewport.BackColor = UiTheme.SurfaceMuted;
        progress.BackColor = UiTheme.Cyan;
        logScrollBar.RefreshTheme();
        logBox.ForeColor = UiTheme.Text;
        statusLabel.ForeColor = statusKey == "failed" ? UiTheme.Warning : statusKey == "scanning" || statusKey == "claiming" ? UiTheme.Cyan : UiTheme.Success;
        achievementList.RefreshTheme();
        achievementScrollBar.RefreshTheme();
        achievementProgressTrack.BackColor = UiTheme.Border;
        achievementProgressFill.BackColor = UiTheme.Success;
        searchPlaceholderLabel.ForeColor = UiTheme.MutedText;
        ApplyAchievementFilter();
        RefreshClaimButtonStyle();
        UpdateStatus(statusKey);
        UpdateLogLabel();
        ApplyLanguage();
    }

    private void UpdateDetailsLabel() { detailsButton.Text = detailsExpanded ? L("Ẩn chi tiết kỹ thuật", "Hide technical details") : L("Chi tiết kỹ thuật", "View technical details"); }
    private void UpdateLogLabel() { logToggleButton.Text = (logExpanded ? "-  " : "+  ") + L("Nhật ký hoạt động", "Activity log"); }
    private void SetLogExpanded(bool expanded)
    {
        logExpanded = expanded;
        float scale = Font.Height / 15F;
        float desired = (ClientSize.Height / scale < 720 ? 100F : ClientSize.Height / scale < 810 ? 156F : 205F) * scale;
        // Reserve the full toolbar/footer and at least one achievement row before expanding diagnostics.
        float minimumAchievementHeight = achievementCard.Padding.Vertical + 49F + 54F + 30F + 65F +
            Math.Max(54, achievementList.RowTemplate.Height);
        float availableLogHeight = pageLayout.ClientSize.Height - pageLayout.Padding.Vertical - 206F -
            minimumAchievementHeight;
        // A single synchronous layout pass avoids the stale 1st-click geometry that
        // occurs when BeginInvoke(LayoutLog) runs after the initial paint.
        pageLayout.SuspendLayout();
        try
        {
            pageLayout.RowStyles[2].Height = expanded
                ? Math.Max(56F, Math.Min(desired, availableLogHeight))
                : 56F * scale;
            logBox.ForeColor = UiTheme.Text;
            logBox.BackColor = UiTheme.SurfaceMuted;
            logViewport.Visible = expanded;
            logBox.Visible = expanded;
        }
        finally { pageLayout.ResumeLayout(true); }
        logCard.PerformLayout();
        if (expanded)
        {
            logViewport.PerformLayout();
            LayoutLog();
        }
        UpdateLogLabel();
    }

    private void ApplyAchievementFilter()
    {
        achievementList.ApplyFilter(searchBox.Text, currentFilter);
        for (int i = 0; i < filterButtons.Length; i++)
        {
            filterButtons[i].BackColor = i == currentFilter ? UiTheme.Border : UiTheme.SurfaceMuted;
            filterButtons[i].ForeColor = i == currentFilter ? UiTheme.Cyan : UiTheme.MutedText;
            filterButtons[i].AccessibleDescription = i == currentFilter ? L("Bộ lọc hiện tại", "Current filter") : string.Empty;
        }
        UpdateSelectionSummary();
        UpdateAchievementDescription();
        QueueVisibleArtwork();
    }

    private void UpdateStatus(string key)
    {
        statusKey = key;
        statusLabel.Text = key == "scanning" ? L("Đang quét (chỉ đọc)", "Scanning (read-only)")
            : key == "claiming" ? L("Đang claim...", "Claiming...")
            : key == "complete" ? L("Hoàn tất", "Completed")
            : key == "empty-game" ? L("0 thành tích", "No achievements")
            : key == "interrupted" ? L("Đã dừng - kiểm tra lại", "Interrupted - verify state")
            : key == "failed" ? L("Có lỗi - mở nhật ký", "Failed - open activity log") : L("Sẵn sàng", "Ready");
        statusLabel.ForeColor = key == "failed" || key == "interrupted" ? UiTheme.Warning : key == "scanning" || key == "claiming" ? UiTheme.Cyan : UiTheme.Success;
        bool steamRunning = Process.GetProcessesByName("steam").Length > 0;
        steamStatusLabel.Text = steamRunning ? L("Steam đang chạy", "Steam running") : L("Steam chưa mở", "Steam not running");
        steamStatusLabel.ForeColor = steamRunning ? UiTheme.Success : UiTheme.MutedText;
        UpdateSelectionSummary();
    }

    private void SetBusy(bool busy)
    {
        progress.Visible = busy;
        pageLayout.RowStyles[3].Height = busy ? Math.Max(3F, 3F * Font.Height / 15F) : 0F;
        browseButton.Enabled = libraryButton.Enabled = pathBox.Enabled = languageToggleButton.Enabled = themeButton.Enabled = settingsButton.Enabled = preflightButton.Enabled = insightsButton.Enabled = !busy;
        cancelWorkerButton.Visible = busy && workerProcess != null;
        if (headerActions != null) headerActions.Width = cancelWorkerButton.Visible ? 514 : 418;
        cancelWorkerButton.Enabled = workerProcess != null && string.IsNullOrEmpty(workerStopReason);
        sortButton.Enabled = !busy;
        scanButton.Enabled = !busy && Directory.Exists(pathBox.Text);
        searchBox.Enabled = !busy;
        foreach (Button button in filterButtons) button.Enabled = !busy;
        SetAchievementControlsEnabled(!busy && achievementList.Items.Count > 0);
        claimButton.Enabled = !busy && detectedGame != null && achievementList.CheckedItems.Any();
    }

    private void ClearArtwork()
    {
        artworkGeneration++;
        gameCover.Image = null;
        gameCover.Visible = false;
        if (steamArtwork != null) { steamArtwork.Dispose(); steamArtwork = null; }
        foreach (Image image in achievementImages.Values) image.Dispose();
        achievementImages.Clear();
        pendingArtwork.Clear();
        coverFallback.Text = L("Đang tải ảnh game...", "Loading game artwork...");
    }

    private async void LoadGameArtwork(string appId)
    {
        int generation = artworkGeneration;
        SteamArtwork loaded;
        try { loaded = await SteamArtwork.LoadAsync(appId); }
        catch (Exception)
        {
            if (!IsDisposed && generation == artworkGeneration)
                coverFallback.Text = L("Không có ảnh game", "Game artwork unavailable");
            return; // Optional artwork must never interrupt the achievement workflow.
        }
        if (IsDisposed || Disposing || generation != artworkGeneration) { loaded.Dispose(); return; }
        steamArtwork = loaded;
        gameCover.Image = loaded.Cover;
        gameCover.Visible = loaded.Cover != null;
        coverFallback.Text = L("Không có ảnh game", "Game artwork unavailable");
        QueueVisibleArtwork();
        achievementList.Invalidate();
    }

    private string GetAchievementIconUrl(AchievementItem item)
    {
        if (steamArtwork == null) return null;
        Dictionary<string, string> urls = item.Achieved ? steamArtwork.IconUrls : steamArtwork.LockedIconUrls;
        string url;
        if (!string.IsNullOrEmpty(item.Id) && urls.TryGetValue("id:" + item.Id, out url)) return url;
        if (!string.IsNullOrEmpty(item.Name) && urls.TryGetValue("name:" + item.Name, out url)) return url;
        if (!item.Achieved)
        {
            if (!string.IsNullOrEmpty(item.Id) && steamArtwork.IconUrls.TryGetValue("id:" + item.Id, out url)) return url;
            if (!string.IsNullOrEmpty(item.Name) && steamArtwork.IconUrls.TryGetValue("name:" + item.Name, out url)) return url;
        }
        return null;
    }

    private Image GetAchievementImage(AchievementItem item)
    {
        string url = GetAchievementIconUrl(item);
        Image image;
        return url != null && achievementImages.TryGetValue(url, out image) ? image : null;
    }

    private void QueueVisibleArtwork()
    {
        if (steamArtwork == null || achievementList.Rows.Count == 0 || IsDisposed || Disposing) return;
        int first = Math.Max(0, achievementList.FirstDisplayedScrollingRowIndex);
        int count = Math.Max(1, achievementList.DisplayedRowCount(true));
        for (int index = first; index < Math.Min(achievementList.Rows.Count, first + count + 1); index++)
        {
            AchievementItem item = achievementList.Rows[index].Tag as AchievementItem;
            if (item == null) continue;
            string url = GetAchievementIconUrl(item);
            if (url != null && !achievementImages.ContainsKey(url) && pendingArtwork.Add(url))
                LoadAchievementIcon(url, artworkGeneration);
        }
    }

    private async void LoadAchievementIcon(string url, int generation)
    {
        Image image;
        try { image = await SteamArtwork.LoadImageAsync(url); }
        catch (Exception) { return; }
        if (IsDisposed || Disposing || generation != artworkGeneration)
        {
            if (image != null) image.Dispose();
            return;
        }
        // Keep unsuccessful requests in the pending set for this scan, avoiding repeated offline retries.
        if (image == null) return;
        pendingArtwork.Remove(url);
        achievementImages[url] = image;
        achievementList.Invalidate();
    }

    private void OpenInsights(object sender, EventArgs e)
    {
        if (workerProcess != null || detectingGame) return;
        using (InsightsDialog dashboard = new InsightsDialog(
            detectedGame == null ? null : detectedGame.AppId, achievementList.Items, English))
        {
            if (dashboard.ShowDialog(this) == DialogResult.Retry && detectedGame != null)
                LoadAchievements(); // Explicit read-only re-scan, never an automatic retry of Claim.
        }
    }

    private async void LoadGlobalRarity(string appId)
    {
        rarityAppId = appId;
        Dictionary<string, double> data = await SteamRarity.FetchAsync(appId);
        if (IsDisposed || Disposing || rarityAppId != appId || detectedGame == null || detectedGame.AppId != appId) return;
        foreach (AchievementItem item in achievementList.Items)
        {
            double percent;
            item.GlobalPercent = data.TryGetValue(item.Id, out percent) ? percent : -1;
        }
        achievementList.Invalidate();
        AppendLog(L("Độ hiếm: ", "Rarity: ") + data.Count + L(" mục có dữ liệu công khai.", " public data points."));
    }

    private void OpenSteamLibrary(object sender, EventArgs e)
    {
        if (workerProcess != null || detectingGame) return;
        using (SteamLibraryDialog dialog = new SteamLibraryDialog(English))
        {
            DialogResult result = dialog.ShowDialog(this);
            if (result == DialogResult.Retry) { BrowseForGame(sender, e); return; }
            if (result == DialogResult.OK && Directory.Exists(dialog.SelectedPath))
            {
                pathBox.Text = dialog.SelectedPath;
                DetectSelectedGame();
            }
        }
    }

    private void BrowseForGame(object sender, EventArgs e)
    {
        if (workerProcess != null) return;
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = L("Chọn thư mục cài đặt của game Steam", "Select the Steam game installation folder");
            dialog.ShowNewFolderButton = false;
            if (Directory.Exists(pathBox.Text))
                dialog.SelectedPath = pathBox.Text;
            else if (Directory.Exists(@"D:\SteamLibrary\steamapps\common"))
                dialog.SelectedPath = @"D:\SteamLibrary\steamapps\common";

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                pathBox.Text = dialog.SelectedPath;
                DetectSelectedGame();
            }
        }
    }

    private async void DetectSelectedGame()
    {
        if (workerProcess != null) return;
        if (detectingGame) return;
        detectingGame = true;
        string scanPath = pathBox.Text;
        bool scanEnglish = English;
        SetBusy(true);
        UpdateStatus("scanning");
        claimButton.Enabled = false;
        ClearArtwork();
        rarityAppId = null;
        achievementList.ClearItems();
        SetAchievementControlsEnabled(false);
        detectedGame = null;
        SetInformation(null);
        AppendLog(L("Đang quét: ", "Scanning: ") + pathBox.Text);
        try
        {
            detectedGame = await Task.Run(() => SteamGameDetector.Detect(scanPath, scanEnglish));
            if (IsDisposed || Disposing) return;
            detectingGame = false;
            SetBusy(false);
            SetInformation(detectedGame);
            LoadGameArtwork(detectedGame.AppId);
            AppendLog(L("Nhận diện thành công: ", "Detected successfully: ") + detectedGame.Name + " (AppID " + detectedGame.AppId + ", " + detectedGame.Architecture + ")");
            if (detectedGame.UsesCompatibilityProvider)
                AppendLog(L("Đang dùng Steam API compatibility provider: ", "Using Steam API compatibility provider: ") + detectedGame.SteamApiPath);
            LoadAchievements();
        }
        catch (Exception error)
        {
            if (IsDisposed || Disposing) return;
            detectingGame = false;
            SetBusy(false);
            UpdateStatus("failed");
            AppendLog(L("LỖI NHẬN DIỆN: ", "DETECTION ERROR: ") + error.Message);
            MessageBox.Show(error.Message, L("Không nhận diện được game", "Could not detect the game"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadAchievements()
    {
        if (detectedGame == null || workerProcess != null)
            return;
        if (Process.GetProcessesByName("steam").Length == 0)
        {
            UpdateStatus("failed");
            AppendLog(L("Không thể tải achievement vì Steam chưa chạy.", "Cannot load achievements because Steam is not running."));
            MessageBox.Show(
                L("Hãy mở Steam ở chế độ Online rồi bấm Quét lại để tải danh sách achievement.", "Start Steam in Online mode, then click Scan again to load the achievement list."),
                L("Steam chưa mở", "Steam is not running"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        achievementList.ClearItems();
        achievementDescriptionLabel.Text = string.Empty;
        SetAchievementControlsEnabled(false);
        AppendLog(L("Đang tải danh sách achievement - thao tác này chỉ đọc, không unlock.", "Loading achievements - this is read-only and does not unlock anything."));
        StartWorker("list", null);
    }

    private void StartWorker(string operation, string selectionPath)
    {
        bool workerStarted = false;
        workerStopReason = null;
        workerReportedEmptyGame = false;
        lastVerifiedCount = -1;
        lastAcceptedCount = 0;
        try
        {
            extractedWorker = ExtractWorker(detectedGame.Architecture);
            string workerSteamApiPath = PrepareSteamApi(detectedGame);
            string arguments = Quote(detectedGame.AppId) + " " + Quote(detectedGame.Name) + " "
                + Quote(workerSteamApiPath) + " " + Quote(detectedGame.InterfaceVersion) + " "
                + Quote(detectedGame.RootDirectory) + " " + Quote(English ? "en" : "vi") + " " + Quote(operation);
            if (!string.IsNullOrEmpty(selectionPath))
                arguments += " " + Quote(selectionPath);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = extractedWorker,
                Arguments = arguments,
                WorkingDirectory = detectedGame.RootDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            workerOperation = operation;
            workerProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            workerProcess.OutputDataReceived += WorkerOutput;
            workerProcess.ErrorDataReceived += WorkerOutput;
            workerProcess.Exited += WorkerExited;
            SetBusy(true);
            UpdateStatus(operation == "list" ? "scanning" : "claiming");
            progress.BackColor = UiTheme.Cyan;
            workerStarted = workerProcess.Start();
            if (!workerStarted) throw new InvalidOperationException("Worker did not start.");
            workerProcess.BeginOutputReadLine();
            workerProcess.BeginErrorReadLine();
            workerClock.Restart();
            workerTimer.Start();
            cancelWorkerButton.Visible = true;
        }
        catch (Exception error)
        {
            AppendLog(L("LỖI KHỞI CHẠY WORKER: ", "WORKER START ERROR: ") + error.Message);
            // A started worker retains its context until WorkerExited completes on the UI thread.
            if (workerStarted) return;
            progress.BackColor = UiTheme.Cyan;
            if (workerProcess != null) workerProcess.Dispose();
            workerProcess = null;
            workerTimer.Stop();
            workerClock.Reset();
            SetBusy(false);
            UpdateStatus("failed");
            CleanupWorker();
            SetAchievementControlsEnabled(achievementList.Items.Count > 0);
            UpdateClaimButtonState();
        }
    }

    private void StopWorker(bool timeout)
    {
        if (workerProcess == null || !string.IsNullOrEmpty(workerStopReason)) return;
        try
        {
            if (workerProcess.HasExited) return;
            workerStopReason = timeout ? "timeout" : "cancelled";
            cancelWorkerButton.Enabled = false;
            AppendLog(timeout
                ? L("Worker quá thời gian cho phép, đang dừng. Nếu đang Claim, kết quả chưa chắc chắn.",
                    "Worker timed out and is stopping. Any in-progress claim may have an uncertain outcome.")
                : L("Đã yêu cầu hủy worker. Nếu đang Claim, hãy kiểm tra lại trạng thái trong Steam.",
                    "Worker cancellation requested. If claiming, verify the state again in Steam."));
            workerProcess.Kill();
        }
        catch (InvalidOperationException) { workerStopReason = null; }
        catch (Exception error) { AppendLog(error.Message); }
    }

    private void SetInformation(DetectedGame game)
    {
        gameValue.Text = game == null ? L("Chưa chọn game", "No game selected") : game.Name;
        detectionStatusLabel.Text = game == null ? L("CHƯA CÓ GAME", "NO GAME") : L("ĐÃ NHẬN DIỆN", "DETECTED");
        detectionStatusLabel.ForeColor = game == null ? UiTheme.MutedText : UiTheme.Success;
        appIdValue.Text = game == null ? "-" : game.AppId;
        dllValue.Text = game == null ? "-" : game.SteamApiPath
            + (string.IsNullOrEmpty(game.SteamApiArchiveEntry) ? string.Empty : " :: " + game.SteamApiArchiveEntry)
            + (game.UsesCompatibilityProvider ? " [COMPATIBILITY PROVIDER]" : string.Empty);
        architectureValue.Text = game == null ? "-" : game.Architecture;
        interfaceValue.Text = game == null ? "-" : game.InterfaceVersion;
        foreach (Label value in new Label[] { gameValue, appIdValue, dllValue, architectureValue, interfaceValue })
            informationToolTip.SetToolTip(value, value.Text);
    }

    private void SetAchievementControlsEnabled(bool enabled)
    {
        achievementList.Enabled = enabled;
        achievementScrollBar.Enabled = enabled;
        selectAllButton.Enabled = enabled;
        selectLockedButton.Enabled = enabled;
        clearSelectionButton.Enabled = enabled;
        selectionMenuButton.Enabled = enabled;
    }

    private void SetAchievementChecks(Func<AchievementItem, bool> shouldCheck)
    {
        if (workerProcess != null) return;
        achievementList.SetChecks(shouldCheck);
        UpdateClaimButtonState();
    }

    private void UpdateClaimButtonState()
    {
        bool ready = detectedGame != null && workerProcess == null && achievementList.CheckedItems.Count() > 0;
        claimButton.Enabled = ready;
        UpdateSelectionSummary();
    }

    private void RefreshClaimButtonStyle()
    {
        claimButton.BackColor = claimButton.Enabled ? UiTheme.Primary : UiTheme.Disabled;
        claimButton.ForeColor = claimButton.Enabled ? UiTheme.Canvas : UiTheme.MutedText;
    }

    private void UpdateSelectionSummary()
    {
        int total = achievementList.Items.Count;
        int unlocked = achievementList.Items.Count(item => item.Achieved);
        int selected = achievementList.CheckedItems.Count();
        HashSet<AchievementItem> visible = new HashSet<AchievementItem>(achievementList.Rows.Cast<DataGridViewRow>().Select(row => (AchievementItem)row.Tag));
        int hidden = achievementList.CheckedItems.Count(item => !visible.Contains(item));
        metricValues[0].Text = total.ToString();
        metricValues[1].Text = unlocked.ToString();
        metricValues[2].Text = (total - unlocked).ToString();
        selectedCountLabel.Text = selected + L(" đã chọn", " selected") + (hidden > 0 ? " (" + hidden + L(" ẩn)", " hidden)") : string.Empty);
        confirmationHintLabel.Text = L("Chỉ thay đổi sau khi xác nhận", "Nothing changes until confirmed");
        searchPlaceholderLabel.Visible = false;
        visibleCountLabel.Text = achievementList.VisibleCount + "/" + total + L(" mục", " shown");
        UpdateAchievementProgress();
        emptyLabel.Visible = achievementList.VisibleCount == 0;
        achievementList.Visible = !emptyLabel.Visible;
        if (emptyLabel.Visible) emptyLabel.BringToFront();
        achievementScrollBar.BringToFront();
        UpdateAchievementScrollBar();
        emptyLabel.Text = statusKey == "scanning" ? L("Đang đọc danh sách achievement...\nThao tác quét không claim achievement.", "Reading achievements...\nScanning does not claim achievements.")
            : total > 0 ? L("Không có kết quả phù hợp.\nThử từ khóa hoặc bộ lọc khác.", "No matching achievements.\nTry another search or filter.")
            : statusKey == "empty-game" ? L("GAME KHÔNG CÓ ACHIEVEMENT\nSteam báo game này có 0 thành tích. Bạn vẫn có thể chọn game khác.",
                "NO ACHIEVEMENTS AVAILABLE\nSteam reports zero achievements for this game. Choose another game to continue.")
            : statusKey == "failed" ? L("Chưa tải được achievement.\nMở nhật ký để xem chi tiết và thử quét lại.", "Achievements could not be loaded.\nOpen the activity log for details, then scan again.")
            : L("BẮT ĐẦU VỚI GAME STEAM\nChọn 'Thư viện' để tìm game đã cài, hoặc chọn thư mục thủ công.", "GET STARTED WITH STEAM\nChoose 'My games' to find installed games, or browse a folder.");
        // A native DataGridView draws its own HWND, so the original painted overlay
        // could obscure the multi-line empty state. Use one real foreground label.
        achievementList.EmptyStateMessage = null;
        achievementList.AccessibleDescription = emptyLabel.Visible ? emptyLabel.Text : L("Danh sách thành tích", "Achievement list");
        achievementList.Invalidate();
    }


    private void UpdateAchievementProgress()
    {
        int total = achievementList.Items.Count;
        int unlocked = achievementList.Items.Count(item => item.Achieved);
        int width = achievementProgressTrack.ClientSize.Width;
        int filled = total == 0 ? 0 : (int)Math.Round(width * unlocked / (double)total);
        achievementProgressFill.Bounds = new Rectangle(0, 0, Math.Max(0, Math.Min(width, filled)), achievementProgressTrack.ClientSize.Height);
    }

    private void UpdateAchievementDescription()
    {
        AchievementItem item = achievementList.SelectedItem;
        achievementDescriptionLabel.Text = item == null ? L("Chọn một dòng để xem API ID và mô tả đầy đủ.", "Select a row to inspect its API ID and full description.") : "API ID: " + item.Id;
        informationToolTip.SetToolTip(achievementDescriptionLabel, item == null ? achievementDescriptionLabel.Text : item.Id + Environment.NewLine + item.Description);
    }

    private AchievementItem ParseAchievement(string line)
    {
        string[] fields = line.Substring("ACHIEVEMENT=".Length).Split('|');
        if (fields.Length != 5 && fields.Length != 6)
            throw new FormatException(L("Dòng achievement không hợp lệ.", "Invalid achievement row."));
        return new AchievementItem
        {
            Achieved = fields[1] == "1",
            Id = Encoding.UTF8.GetString(Convert.FromBase64String(fields[2])),
            Name = Encoding.UTF8.GetString(Convert.FromBase64String(fields[3])),
            Description = Encoding.UTF8.GetString(Convert.FromBase64String(fields[4])),
            English = English,
            UnlockUnix = fields.Length == 6 ? ParseUnlockTime(fields[5]) : 0
        };
    }

    private static long ParseUnlockTime(string raw)
    {
        long result;
        return long.TryParse(raw, out result) && result >= 0 && result < 4102444800L ? result : 0;
    }

    private string CreateSelectionFile(IEnumerable<AchievementItem> selected)
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "UmiAchievementClaimer");
        Directory.CreateDirectory(tempDirectory);
        selectedAchievementFile = Path.Combine(tempDirectory, "selected_" + Process.GetCurrentProcess().Id + "_" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllLines(
            selectedAchievementFile,
            selected.Select(item => Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Id))),
            new UTF8Encoding(false));
        return selectedAchievementFile;
    }

    private void ClaimAchievements(object sender, EventArgs e)
    {
        if (detectedGame == null || workerProcess != null)
            return;
        List<AchievementItem> selected = achievementList.CheckedItems.Cast<AchievementItem>().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(
                L("Hãy tick ít nhất một achievement cần claim.", "Select at least one achievement to claim."),
                L("Chưa chọn achievement", "No achievements selected"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }
        if (Process.GetProcessesByName("steam").Length == 0)
        {
            MessageBox.Show(
                L("Steam chưa chạy. Hãy mở Steam Online rồi thử lại.", "Steam is not running. Start Steam in Online mode and try again."),
                L("Steam chưa mở", "Steam is not running"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using (ClaimConfirmationDialog confirmation = new ClaimConfirmationDialog(detectedGame, selected.Count, achievementList.Items.Count, English))
        {
            if (confirmation.ShowDialog(this) != DialogResult.OK) return;
        }

        try
        {
            lastClaimCount = selected.Count;
            try
            {
                AchievementHistory.SavePending(detectedGame.AppId, achievementList.Items, selected);
                AppendLog(L("Đã lưu dấu mốc trước Claim để đối chiếu chỉ đọc.", "Saved pre-claim state for read-only recovery."));
            }
            catch (Exception historyError)
            {
                AppendLog(L("Không lưu được dấu mốc: ", "Pre-claim snapshot unavailable: ") + historyError.Message);
            }
            string selectionPath = CreateSelectionFile(selected);
            AppendLog(L("Bắt đầu claim ", "Starting claim for ") + selected.Count + L(" achievement đã chọn...", " selected achievement(s)..."));
            StartWorker("selected", selectionPath);
        }
        catch (Exception error)
        {
            AppendLog(L("LỖI KHỞI CHẠY WORKER: ", "WORKER START ERROR: ") + error.Message);
            UpdateStatus("failed");
            progress.BackColor = UiTheme.Cyan;
            CleanupWorker();
            UpdateClaimButtonState();
        }
    }

    private void WorkerOutput(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;

        if (e.Data.StartsWith("ACHIEVEMENT=", StringComparison.Ordinal))
        {
            try
            {
                AchievementItem item = ParseAchievement(e.Data);
                BeginInvoke(new Action(delegate
                {
                    achievementList.AddItem(item);
                    UpdateSelectionSummary();
                    QueueVisibleArtwork();
                }));
            }
            catch (Exception error)
            {
                BeginInvoke(new Action<string>(AppendLog), L("Không đọc được achievement: ", "Could not parse achievement: ") + error.Message);
            }
            return;
        }

        if (e.Data == "RESULT=LIST_EMPTY")
            workerReportedEmptyGame = true;
        if (e.Data.StartsWith("VERIFIED_COUNT=", StringComparison.Ordinal))
        {
            int count;
            if (int.TryParse(e.Data.Substring("VERIFIED_COUNT=".Length), out count))
                System.Threading.Interlocked.Exchange(ref lastVerifiedCount, count);
        }
        if (e.Data.StartsWith("[OK]", StringComparison.Ordinal))
            System.Threading.Interlocked.Increment(ref lastAcceptedCount);
        BeginInvoke(new Action<string>(AppendLog), e.Data);
    }

    private void WorkerExited(object sender, EventArgs e)
    {
        Process completedProcess = (Process)sender;
        completedProcess.WaitForExit();
        int exitCode = completedProcess.ExitCode;
        string completedOperation = workerOperation;
        BeginInvoke(new Action(delegate
        {
            workerTimer.Stop();
            workerClock.Reset();
            string interrupted = workerStopReason;
            workerStopReason = null;
            AppendLog(L("Worker kết thúc với exit code ", "Worker finished with exit code ") + exitCode + ".");
            progress.BackColor = UiTheme.Cyan;
            completedProcess.Dispose();
            workerProcess = null;
            workerOperation = null;
            SetBusy(false);
            UpdateStatus(!string.IsNullOrEmpty(interrupted) ? "interrupted" : exitCode == 0 ? "complete" : "failed");
            CleanupWorker();

            if (completedOperation == "list")
            {
                bool emptyGame = exitCode == 0 && workerReportedEmptyGame && achievementList.Items.Count == 0;
                bool loaded = exitCode == 0 && (achievementList.Items.Count > 0 || emptyGame);
                SetAchievementControlsEnabled(loaded && !emptyGame);
                UpdateClaimButtonState();
                UpdateStatus(!string.IsNullOrEmpty(interrupted) ? "interrupted"
                    : emptyGame ? "empty-game" : loaded ? "complete" : "failed");
                if (loaded && detectedGame != null)
                {
                    try
                    {
                        bool changed = AchievementHistory.Save(
                            AchievementHistory.Create(detectedGame.AppId, detectedGame.Name, achievementList.Items));
                        AppendLog(changed ? L("Đã lưu snapshot mới trên máy.", "New local snapshot saved.")
                            : L("Snapshot trùng trạng thái trước; không lưu lại.", "Snapshot unchanged; duplicate skipped."));
                    }
                    catch (Exception historyError)
                    { AppendLog(L("Không lưu được snapshot: ", "Snapshot not saved: ") + historyError.Message); }
                    LoadGlobalRarity(detectedGame.AppId);
                }
                AppendLog(emptyGame
                    ? L("Steam trả về 0 achievement. Đây là kết quả hợp lệ, không phải lỗi.",
                        "Steam returned zero achievements. This is a valid empty game, not a scan failure.")
                    : loaded
                    ? L("Đã tải ", "Loaded ") + achievementList.Items.Count + L(" achievement. Mặc định chưa chọn mục nào.", " achievements. Nothing is selected by default.")
                    : L("Không tải được danh sách achievement. Xem log phía trên.", "Could not load the achievement list. Check the log above."));
                return;
            }

            SetAchievementControlsEnabled(achievementList.Items.Count > 0);
            UpdateClaimButtonState();
            int verified = System.Threading.Interlocked.CompareExchange(ref lastVerifiedCount, 0, 0);
            int accepted = System.Threading.Interlocked.CompareExchange(ref lastAcceptedCount, 0, 0);
            bool verifiedAll = string.IsNullOrEmpty(interrupted) && exitCode == 0 &&
                verified >= lastClaimCount && lastClaimCount > 0;
            string details = !string.IsNullOrEmpty(interrupted)
                ? L("Worker bị hủy hoặc quá thời gian. Trạng thái achievement có thể đã thay đổi một phần.",
                    "Worker cancelled or timed out. Achievement state may have changed partially.")
                : L("Steam API xác nhận cục bộ ", "Steam API locally verified ") +
                    Math.Max(0, verified) + "/" + lastClaimCount +
                    L(" achievement. Đã chấp nhận: ", " achievement(s). Accepted: ") + accepted + "/" + lastClaimCount + ".";
            AppendLog(details);
            MessageBox.Show(this,
                details + Environment.NewLine + Environment.NewLine +
                L("Đây là kết quả đọc lại qua Steam API, chưa xác minh đồng bộ máy chủ.",
                  "This is a Steam API readback, not independent confirmation of server synchronization."),
                verifiedAll ? L("Đã xác minh cục bộ", "Locally verified") : L("Cần kiểm tra lại", "Review required"),
                MessageBoxButtons.OK, verifiedAll ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (string.IsNullOrEmpty(interrupted) && Process.GetProcessesByName("steam").Length > 0)
                LoadAchievements();
        }));
    }

    private string ExtractWorker(string architecture)
    {
        string resourceName = architecture == "x64" ? "AchievementWorker64" : "AchievementWorker32";
        string tempDirectory = Path.Combine(Path.GetTempPath(), "UmiAchievementClaimer");
        Directory.CreateDirectory(tempDirectory);
        string path = Path.Combine(tempDirectory, "worker_" + architecture + "_" + Process.GetCurrentProcess().Id + ".exe");
        using (Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
            if (source == null)
                throw new InvalidOperationException(L("Thiếu worker nhúng: ", "Missing embedded worker: ") + resourceName);
            using (FileStream destination = File.Create(path))
                source.CopyTo(destination);
        }
        return path;
    }

    private string PrepareSteamApi(DetectedGame game)
    {
        if (string.IsNullOrEmpty(game.SteamApiArchiveEntry))
            return game.SteamApiPath;

        string tempRoot = Path.Combine(Path.GetTempPath(), "UmiAchievementClaimer");
        extractedSteamApiDirectory = Path.Combine(tempRoot, "api_" + Process.GetCurrentProcess().Id + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extractedSteamApiDirectory);
        string destinationPath = Path.Combine(extractedSteamApiDirectory, Path.GetFileName(game.SteamApiArchiveEntry));
        if (string.Equals(game.SteamApiArchiveFormat, "cab", StringComparison.OrdinalIgnoreCase))
        {
            string cabinetPath = Path.Combine(extractedSteamApiDirectory, "embedded-steam-api.cab");
            CopyFileRange(game.SteamApiPath, cabinetPath, game.SteamApiArchiveOffset, game.SteamApiArchiveLength);
            string expand = Path.Combine(Environment.SystemDirectory, "expand.exe");
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = expand,
                Arguments = "-F:" + Quote(Path.GetFileName(game.SteamApiArchiveEntry)) + " " + Quote(cabinetPath) + " " + Quote(extractedSteamApiDirectory),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process process = Process.Start(startInfo))
            {
                if (!process.WaitForExit(60000))
                {
                    try { process.Kill(); }
                    catch { }
                    throw new InvalidOperationException(L("Quá thời gian tách Steam API từ CAB.", "Timed out extracting Steam API from CAB."));
                }
                if (process.ExitCode != 0 || !File.Exists(destinationPath))
                {
                    string detail = process.StandardError.ReadToEnd();
                    throw new InvalidOperationException(L("Không thể tách Steam API từ CAB. ", "Could not extract Steam API from CAB. ") + detail);
                }
            }
            return destinationPath;
        }

        using (ZipArchive archive = ZipFile.OpenRead(game.SteamApiPath))
        {
            ZipArchiveEntry entry = archive.Entries.FirstOrDefault(item =>
                string.Equals(item.FullName, game.SteamApiArchiveEntry, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
                throw new InvalidOperationException(L("Không tìm thấy Steam API entry trong archive: ", "Steam API entry is missing from the archive: ") + game.SteamApiArchiveEntry);
            using (Stream source = entry.Open())
            using (FileStream destination = File.Create(destinationPath))
                source.CopyTo(destination);
        }
        return destinationPath;
    }

    private static void CopyFileRange(string sourcePath, string destinationPath, long offset, long length)
    {
        if (offset < 0 || length <= 0)
            throw new InvalidOperationException("Invalid embedded archive range.");
        using (FileStream source = File.OpenRead(sourcePath))
        using (FileStream destination = File.Create(destinationPath))
        {
            if (offset + length > source.Length)
                throw new InvalidOperationException("Embedded archive range exceeds source file length.");
            source.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = new byte[1024 * 1024];
            long remaining = length;
            while (remaining > 0)
            {
                int read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (read <= 0)
                    throw new EndOfStreamException("Unexpected end of embedded archive.");
                destination.Write(buffer, 0, read);
                remaining -= read;
            }
        }
    }

    private void CleanupWorker()
    {
        if (!string.IsNullOrEmpty(extractedWorker))
        {
            try { File.Delete(extractedWorker); }
            catch { }
            extractedWorker = null;
        }
        if (!string.IsNullOrEmpty(extractedSteamApiDirectory))
        {
            try { Directory.Delete(extractedSteamApiDirectory, true); }
            catch { }
            extractedSteamApiDirectory = null;
        }
        if (!string.IsNullOrEmpty(selectedAchievementFile))
        {
            try { File.Delete(selectedAchievementFile); }
            catch { }
            selectedAchievementFile = null;
        }
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private void AppendLog(string text)
    {
        if (logViewport.Visible && IsHandleCreated) BeginInvoke(new Action(LayoutLog));
        logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
        logBox.SelectionStart = logBox.TextLength;
        logBox.ScrollToCaret();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowTheme.Apply(this);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        UiTheme.Changed -= ThemeChanged;
        rarityAppId = null;
        ClearArtwork();
        workerTimer.Stop();
        workerTimer.Dispose();
        sortMenu.Dispose();
        selectionMenu.Dispose();
        if (brandLogo.Image != null) brandLogo.Image.Dispose();
        informationToolTip.Dispose();
        base.OnFormClosed(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (workerProcess != null)
        {
            MessageBox.Show(
                L("Worker đang tải hoặc claim achievement. Hãy chờ hoàn tất trước khi đóng app.", "The worker is loading or claiming achievements. Wait for it to finish before closing the app."),
                L("Đang xử lý", "Operation in progress"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            e.Cancel = true;
            return;
        }
        CleanupWorker();
        base.OnFormClosing(e);
    }
}
}
