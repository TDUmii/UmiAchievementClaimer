using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal sealed class ClaimerForm : Form
{
    private readonly Panel headerPanel = new Panel();
    private readonly TableLayoutPanel pageLayout = new TableLayoutPanel();
    private readonly CardPanel gameCard = new CardPanel();
    private readonly CardPanel informationCard = new CardPanel();
    private readonly CardPanel achievementCard = new CardPanel();
    private readonly CardPanel logCard = new CardPanel();
    private readonly PictureBox brandLogo = new PictureBox();
    private readonly Label titleLabel = new Label();
    private readonly Label subtitleLabel = new Label();
    private readonly Label gameFolderTitleLabel = new Label();
    private readonly Label detectionTitleLabel = new Label();
    private readonly Label safeModeLabel = new Label();
    private readonly TextBox pathBox = new TextBox();
    private readonly Button browseButton = new Button();
    private readonly Button scanButton = new Button();
    private readonly Label[] captionLabels = new Label[5];
    private readonly Label gameValue = new Label();
    private readonly Label appIdValue = new Label();
    private readonly Label dllValue = new Label();
    private readonly Label architectureValue = new Label();
    private readonly Label interfaceValue = new Label();
    private readonly RichTextBox logBox = new RichTextBox();
    private readonly Label logTitleLabel = new Label();
    private readonly Label achievementTitleLabel = new Label();
    private readonly Label achievementDescriptionLabel = new Label();
    private readonly CheckedListBox achievementList = new CheckedListBox();
    private readonly Button selectAllButton = new Button();
    private readonly Button selectLockedButton = new Button();
    private readonly Button clearSelectionButton = new Button();
    private readonly Button claimButton = new Button();
    private readonly ProgressBar progress = new ProgressBar();
    private readonly ComboBox languageBox = new ComboBox();
    private DetectedGame detectedGame;
    private Process workerProcess;
    private string extractedWorker;
    private string extractedSteamApiDirectory;
    private string selectedAchievementFile;
    private string workerOperation;
    private int lastClaimCount;
    private bool English { get { return languageBox.SelectedIndex == 0; } }

    public ClaimerForm()
    {
        Text = "Umi Achievement Claimer";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1120, 840);
        MinimumSize = new Size(920, 800);
        Font = UiTheme.Font(9F);
        BackColor = UiTheme.Canvas;
        ForeColor = UiTheme.Text;
        AutoScaleMode = AutoScaleMode.Dpi;

        try
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            // The embedded EXE icon remains available even if Windows cannot extract it here.
        }

        BuildHeader();
        BuildPage();
        Controls.Add(pageLayout);
        Controls.Add(headerPanel);
        ApplyLanguage();
        languageBox.SelectedIndexChanged += delegate { ApplyLanguage(); };
        AppendLog(L("Sẵn sàng. Hãy chọn thư mục cài game Steam.", "Ready. Select a Steam game installation folder."));
    }

    private void BuildHeader()
    {
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Height = 88;
        headerPanel.BackColor = UiTheme.Surface;
        headerPanel.Padding = new Padding(24, 12, 24, 10);

        brandLogo.Location = new Point(24, 12);
        brandLogo.Size = new Size(62, 62);
        brandLogo.SizeMode = PictureBoxSizeMode.Zoom;
        brandLogo.Image = LoadBrandLogo();

        titleLabel.Font = UiTheme.Font(19F, FontStyle.Bold);
        titleLabel.ForeColor = UiTheme.Navy;
        titleLabel.AutoSize = true;
        titleLabel.Location = new Point(98, 14);

        subtitleLabel.Font = UiTheme.Font(9.25F);
        subtitleLabel.ForeColor = UiTheme.MutedText;
        subtitleLabel.AutoSize = true;
        subtitleLabel.Location = new Point(101, 51);

        languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
        languageBox.Items.AddRange(new object[] { "English", "Tiếng Việt" });
        languageBox.SelectedIndex = 0;
        languageBox.Font = UiTheme.Font(9.25F);
        languageBox.Dock = DockStyle.Fill;

        Panel languageHost = new Panel
        {
            Dock = DockStyle.Right,
            Width = 174,
            Padding = new Padding(0, 16, 0, 14),
            BackColor = UiTheme.Surface
        };
        languageHost.Controls.Add(languageBox);

        headerPanel.Controls.AddRange(new Control[] { brandLogo, titleLabel, subtitleLabel, languageHost });
        headerPanel.Paint += delegate(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(UiTheme.Border))
                e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
        };
    }

    private void BuildPage()
    {
        pageLayout.Dock = DockStyle.Fill;
        pageLayout.BackColor = UiTheme.Canvas;
        pageLayout.Padding = new Padding(24, 18, 24, 18);
        pageLayout.ColumnCount = 1;
        pageLayout.RowCount = 5;
        pageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 166F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 5F));

        BuildGameCard();
        BuildInformationCard();
        BuildAchievementCard();
        BuildLogCard();

        progress.Dock = DockStyle.Fill;
        progress.Margin = new Padding(0);
        progress.Style = ProgressBarStyle.Blocks;

        pageLayout.Controls.Add(gameCard, 0, 0);
        pageLayout.Controls.Add(informationCard, 0, 1);
        pageLayout.Controls.Add(achievementCard, 0, 2);
        pageLayout.Controls.Add(logCard, 0, 3);
        pageLayout.Controls.Add(progress, 0, 4);
    }

    private void BuildGameCard()
    {
        gameCard.Dock = DockStyle.Fill;
        gameCard.Padding = new Padding(18, 12, 18, 14);

        gameFolderTitleLabel.Font = UiTheme.Font(10F, FontStyle.Bold);
        gameFolderTitleLabel.ForeColor = UiTheme.Navy;
        gameFolderTitleLabel.Dock = DockStyle.Top;
        gameFolderTitleLabel.Height = 25;

        TableLayoutPanel pathLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0, 5, 0, 0)
        };
        pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142F));
        pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122F));

        pathBox.Dock = DockStyle.Fill;
        pathBox.Margin = new Padding(0, 0, 10, 0);
        pathBox.Font = UiTheme.Font(9.5F);
        pathBox.BorderStyle = BorderStyle.FixedSingle;

        ConfigureButton(browseButton, UmiButtonStyle.Secondary);
        browseButton.Dock = DockStyle.Fill;
        browseButton.Margin = new Padding(0, 0, 8, 0);
        browseButton.Click += BrowseForGame;

        ConfigureButton(scanButton, UmiButtonStyle.Primary);
        scanButton.Dock = DockStyle.Fill;
        scanButton.Margin = new Padding(0);
        scanButton.Click += delegate { DetectSelectedGame(); };

        pathLayout.Controls.Add(pathBox, 0, 0);
        pathLayout.Controls.Add(browseButton, 1, 0);
        pathLayout.Controls.Add(scanButton, 2, 0);
        gameCard.Controls.Add(pathLayout);
        gameCard.Controls.Add(gameFolderTitleLabel);
    }

    private void BuildInformationCard()
    {
        informationCard.Dock = DockStyle.Fill;
        informationCard.Padding = new Padding(18, 12, 18, 12);

        detectionTitleLabel.Font = UiTheme.Font(10F, FontStyle.Bold);
        detectionTitleLabel.ForeColor = UiTheme.Navy;
        detectionTitleLabel.Dock = DockStyle.Top;
        detectionTitleLabel.Height = 26;

        TableLayoutPanel values = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            Margin = new Padding(0)
        };
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27F));
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17F));
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));
        values.Controls.Add(CreateInformationCell(0, gameValue), 0, 0);
        values.Controls.Add(CreateInformationCell(1, appIdValue), 1, 0);
        values.Controls.Add(CreateInformationCell(3, architectureValue), 2, 0);
        values.Controls.Add(CreateInformationCell(4, interfaceValue), 3, 0);
        values.Controls.Add(CreateInformationCell(2, dllValue), 4, 0);

        informationCard.Controls.Add(values);
        informationCard.Controls.Add(detectionTitleLabel);
    }

    private void BuildAchievementCard()
    {
        achievementCard.Dock = DockStyle.Fill;
        achievementCard.Padding = new Padding(18, 12, 18, 14);

        Panel toolbar = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = UiTheme.Surface };
        achievementTitleLabel.Font = UiTheme.Font(10F, FontStyle.Bold);
        achievementTitleLabel.ForeColor = UiTheme.Navy;
        achievementTitleLabel.Dock = DockStyle.Left;
        achievementTitleLabel.Width = 230;
        achievementTitleLabel.TextAlign = ContentAlignment.MiddleLeft;

        FlowLayoutPanel actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 580,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        ConfigureButton(selectAllButton, UmiButtonStyle.Secondary);
        ConfigureButton(selectLockedButton, UmiButtonStyle.Secondary);
        ConfigureButton(clearSelectionButton, UmiButtonStyle.Ghost);
        ConfigureButton(claimButton, UmiButtonStyle.Accent);
        selectAllButton.Size = new Size(105, 32);
        selectLockedButton.Size = new Size(118, 32);
        clearSelectionButton.Size = new Size(92, 32);
        claimButton.Size = new Size(230, 32);
        selectAllButton.Enabled = false;
        selectLockedButton.Enabled = false;
        clearSelectionButton.Enabled = false;
        claimButton.Enabled = false;
        selectAllButton.Click += delegate { SetAchievementChecks(delegate { return true; }); };
        selectLockedButton.Click += delegate { SetAchievementChecks(item => !item.Achieved); };
        clearSelectionButton.Click += delegate { SetAchievementChecks(delegate { return false; }); };
        claimButton.Click += ClaimAchievements;
        claimButton.EnabledChanged += delegate { RefreshClaimButtonStyle(); };
        RefreshClaimButtonStyle();
        actions.Controls.AddRange(new Control[] { selectAllButton, selectLockedButton, clearSelectionButton, claimButton });
        toolbar.Controls.Add(actions);
        toolbar.Controls.Add(achievementTitleLabel);

        safeModeLabel.Dock = DockStyle.Top;
        safeModeLabel.Height = 30;
        safeModeLabel.Font = UiTheme.Font(8.75F);
        safeModeLabel.ForeColor = UiTheme.Success;
        safeModeLabel.BackColor = Color.FromArgb(237, 249, 244);
        safeModeLabel.Padding = new Padding(9, 7, 8, 0);

        achievementDescriptionLabel.Dock = DockStyle.Bottom;
        achievementDescriptionLabel.Height = 44;
        achievementDescriptionLabel.Padding = new Padding(10, 7, 10, 5);
        achievementDescriptionLabel.ForeColor = UiTheme.MutedText;
        achievementDescriptionLabel.BackColor = UiTheme.SurfaceMuted;
        achievementDescriptionLabel.AutoEllipsis = true;

        achievementList.Dock = DockStyle.Fill;
        achievementList.Margin = new Padding(0, 8, 0, 8);
        achievementList.BorderStyle = BorderStyle.FixedSingle;
        achievementList.BackColor = UiTheme.Surface;
        achievementList.ForeColor = UiTheme.Text;
        achievementList.Font = UiTheme.Font(9.25F);
        achievementList.CheckOnClick = true;
        achievementList.HorizontalScrollbar = true;
        achievementList.IntegralHeight = false;
        achievementList.ItemCheck += delegate { BeginInvoke(new Action(UpdateClaimButtonState)); };
        achievementList.SelectedIndexChanged += delegate { UpdateAchievementDescription(); };

        Panel listHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8), BackColor = UiTheme.Surface };
        listHost.Controls.Add(achievementList);
        achievementCard.Controls.Add(listHost);
        achievementCard.Controls.Add(achievementDescriptionLabel);
        achievementCard.Controls.Add(safeModeLabel);
        achievementCard.Controls.Add(toolbar);
    }

    private void BuildLogCard()
    {
        logCard.Dock = DockStyle.Fill;
        logCard.Padding = new Padding(18, 10, 18, 14);
        logTitleLabel.Font = UiTheme.Font(10F, FontStyle.Bold);
        logTitleLabel.ForeColor = UiTheme.Navy;
        logTitleLabel.Dock = DockStyle.Top;
        logTitleLabel.Height = 25;

        logBox.Dock = DockStyle.Fill;
        logBox.ReadOnly = true;
        logBox.BorderStyle = BorderStyle.None;
        logBox.BackColor = UiTheme.LogBackground;
        logBox.ForeColor = UiTheme.LogText;
        logBox.Font = new Font("Consolas", 9F);
        logBox.DetectUrls = false;

        logCard.Controls.Add(logBox);
        logCard.Controls.Add(logTitleLabel);
    }

    private static Image LoadBrandLogo()
    {
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UmiBrandLogo"))
        {
            if (stream == null)
                return null;

            using (Image image = Image.FromStream(stream))
                return new Bitmap(image);
        }
    }

    private void ConfigureButton(Button button, UmiButtonStyle style)
    {
        UiTheme.StyleButton(button, style);
        button.Height = 32;
    }

    private Control CreateInformationCell(int captionIndex, Label value)
    {
        Panel cell = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(0, 2, 12, 0) };
        Label caption = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Font = UiTheme.Font(8F, FontStyle.Bold),
            ForeColor = UiTheme.MutedText
        };
        captionLabels[captionIndex] = caption;
        value.Dock = DockStyle.Fill;
        value.Font = UiTheme.Font(9.25F, FontStyle.Bold);
        value.ForeColor = UiTheme.Text;
        value.AutoEllipsis = true;
        value.Text = "—";
        cell.Controls.Add(value);
        cell.Controls.Add(caption);
        return cell;
    }

    private string L(string vietnamese, string english)
    {
        return English ? english : vietnamese;
    }

    private void ApplyLanguage()
    {
        Text = L("Umi Achievement Claimer - Công cụ claim achievement Steam", "Umi Achievement Claimer");
        titleLabel.Text = "Umi Achievement Claimer";
        subtitleLabel.Text = L(
            "Quét an toàn, xem trạng thái và chỉ claim những achievement bạn chọn.",
            "Scan safely, review status, and claim only the achievements you select.");
        gameFolderTitleLabel.Text = L("01  THƯ MỤC GAME STEAM", "01  STEAM GAME FOLDER");
        browseButton.Text = L("Chọn thư mục", "Browse folder");
        scanButton.Text = L("Quét", "Scan");
        detectionTitleLabel.Text = L("02  THÔNG TIN NHẬN DIỆN", "02  DETECTED GAME");
        captionLabels[0].Text = L("GAME", "GAME");
        captionLabels[1].Text = "APP ID";
        captionLabels[2].Text = "STEAM API";
        captionLabels[3].Text = L("KIẾN TRÚC", "ARCHITECTURE");
        captionLabels[4].Text = "USER STATS";
        safeModeLabel.Text = L(
            "✓ Chế độ quét chỉ đọc — không achievement nào được tự chọn hoặc gửi lên Steam.",
            "✓ Read-only scan — no achievement is selected or submitted automatically.");
        achievementTitleLabel.Text = L("Achievement — chưa chọn mục nào", "Achievements — none selected");
        selectAllButton.Text = L("Chọn tất cả", "Select all");
        selectLockedButton.Text = L("Chọn chưa mở", "Select locked");
        clearSelectionButton.Text = L("Bỏ chọn", "Clear");
        claimButton.Text = L("CLAIM ĐÃ CHỌN", "CLAIM SELECTED");
        logTitleLabel.Text = L("NHẬT KÝ HOẠT ĐỘNG", "ACTIVITY LOG");
        foreach (AchievementItem item in achievementList.Items)
            item.English = English;
        achievementList.Refresh();
        UpdateSelectionSummary();
        UpdateAchievementDescription();
    }

    private void BrowseForGame(object sender, EventArgs e)
    {
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

    private void DetectSelectedGame()
    {
        claimButton.Enabled = false;
        achievementList.Items.Clear();
        SetAchievementControlsEnabled(false);
        detectedGame = null;
        SetInformation(null);
        AppendLog(L("Đang quét: ", "Scanning: ") + pathBox.Text);
        try
        {
            detectedGame = SteamGameDetector.Detect(pathBox.Text, English);
            SetInformation(detectedGame);
            AppendLog(L("Nhận diện thành công: ", "Detected successfully: ") + detectedGame.Name + " (AppID " + detectedGame.AppId + ", " + detectedGame.Architecture + ")");
            LoadAchievements();
        }
        catch (Exception error)
        {
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
            AppendLog(L("Không thể tải achievement vì Steam chưa chạy.", "Cannot load achievements because Steam is not running."));
            MessageBox.Show(
                L("Hãy mở Steam ở chế độ Online rồi bấm Quét lại để tải danh sách achievement.", "Start Steam in Online mode, then click Scan again to load the achievement list."),
                L("Steam chưa mở", "Steam is not running"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        achievementList.Items.Clear();
        achievementDescriptionLabel.Text = string.Empty;
        SetAchievementControlsEnabled(false);
        AppendLog(L("Đang tải danh sách achievement — thao tác này chỉ đọc, không unlock.", "Loading achievements — this is read-only and does not unlock anything."));
        StartWorker("list", null);
    }

    private void StartWorker(string operation, string selectionPath)
    {
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
            claimButton.Enabled = false;
            progress.Style = ProgressBarStyle.Marquee;
            workerProcess.Start();
            workerProcess.BeginOutputReadLine();
            workerProcess.BeginErrorReadLine();
        }
        catch (Exception error)
        {
            AppendLog(L("LỖI KHỞI CHẠY WORKER: ", "WORKER START ERROR: ") + error.Message);
            progress.Style = ProgressBarStyle.Blocks;
            workerProcess = null;
            CleanupWorker();
            SetAchievementControlsEnabled(achievementList.Items.Count > 0);
            UpdateClaimButtonState();
        }
    }

    private void SetInformation(DetectedGame game)
    {
        gameValue.Text = game == null ? "—" : game.Name;
        appIdValue.Text = game == null ? "—" : game.AppId;
        dllValue.Text = game == null ? "—" : game.SteamApiPath
            + (string.IsNullOrEmpty(game.SteamApiArchiveEntry) ? string.Empty : " :: " + game.SteamApiArchiveEntry
                + (string.IsNullOrEmpty(game.SteamApiArchiveFormat) ? string.Empty : " [" + game.SteamApiArchiveFormat.ToUpperInvariant() + "]"));
        architectureValue.Text = game == null ? "—" : game.Architecture;
        interfaceValue.Text = game == null ? "—" : game.InterfaceVersion;
    }

    private void SetAchievementControlsEnabled(bool enabled)
    {
        achievementList.Enabled = enabled;
        selectAllButton.Enabled = enabled;
        selectLockedButton.Enabled = enabled;
        clearSelectionButton.Enabled = enabled;
    }

    private void SetAchievementChecks(Func<AchievementItem, bool> shouldCheck)
    {
        for (int index = 0; index < achievementList.Items.Count; index++)
            achievementList.SetItemChecked(index, shouldCheck((AchievementItem)achievementList.Items[index]));
        BeginInvoke(new Action(UpdateClaimButtonState));
    }

    private void UpdateClaimButtonState()
    {
        bool ready = detectedGame != null && workerProcess == null && achievementList.CheckedItems.Count > 0;
        claimButton.Enabled = ready;
        UpdateSelectionSummary();
    }

    private void RefreshClaimButtonStyle()
    {
        claimButton.BackColor = claimButton.Enabled ? UiTheme.Orange : UiTheme.Disabled;
        claimButton.ForeColor = claimButton.Enabled ? Color.White : UiTheme.MutedText;
    }

    private void UpdateSelectionSummary()
    {
        int total = achievementList.Items.Count;
        int selected = achievementList.CheckedItems.Count;
        achievementTitleLabel.Text = English
            ? "Achievements — selected " + selected + "/" + total
            : "Achievement — đã chọn " + selected + "/" + total;
    }

    private void UpdateAchievementDescription()
    {
        AchievementItem item = achievementList.SelectedItem as AchievementItem;
        if (item == null)
        {
            achievementDescriptionLabel.Text = L("Chọn một dòng để xem mô tả. Tick chỉ đánh dấu; chưa có gì được gửi lên Steam.", "Select a row to view its description. Checking a box only selects it; nothing is sent to Steam yet.");
            return;
        }
        achievementDescriptionLabel.Text = string.IsNullOrWhiteSpace(item.Description)
            ? L("Không có mô tả. API ID: ", "No description. API ID: ") + item.Id
            : item.Description;
    }

    private AchievementItem ParseAchievement(string line)
    {
        string[] fields = line.Substring("ACHIEVEMENT=".Length).Split('|');
        if (fields.Length != 5)
            throw new FormatException(L("Dòng achievement không hợp lệ.", "Invalid achievement row."));
        return new AchievementItem
        {
            Achieved = fields[1] == "1",
            Id = Encoding.UTF8.GetString(Convert.FromBase64String(fields[2])),
            Name = Encoding.UTF8.GetString(Convert.FromBase64String(fields[3])),
            Description = Encoding.UTF8.GetString(Convert.FromBase64String(fields[4])),
            English = English
        };
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

        DialogResult answer = MessageBox.Show(
            "Game: " + detectedGame.Name + "\nAppID: " + detectedGame.AppId
            + L("\nĐã chọn: ", "\nSelected: ") + selected.Count + "/" + achievementList.Items.Count
            + L(
                "\n\nApp chỉ đặt những achievement đang được tick."
                + "\nHãy đóng game trước khi tiếp tục. Thao tác này ảnh hưởng hồ sơ Steam và có thể không hoàn tác đầy đủ."
                + "\n\nTiếp tục?",
                "\n\nThe app will set only the checked achievements."
                + "\nClose the game before continuing. This changes your Steam profile and may not be fully reversible."
                + "\n\nContinue?"),
            L("Xác nhận claim achievement", "Confirm achievement claim"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes)
            return;

        try
        {
            lastClaimCount = selected.Count;
            string selectionPath = CreateSelectionFile(selected);
            AppendLog(L("Bắt đầu claim ", "Starting claim for ") + selected.Count + L(" achievement đã chọn...", " selected achievement(s)..."));
            StartWorker("selected", selectionPath);
        }
        catch (Exception error)
        {
            AppendLog(L("LỖI KHỞI CHẠY WORKER: ", "WORKER START ERROR: ") + error.Message);
            progress.Style = ProgressBarStyle.Blocks;
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
                    achievementList.Items.Add(item, false);
                    UpdateSelectionSummary();
                }));
            }
            catch (Exception error)
            {
                BeginInvoke(new Action<string>(AppendLog), L("Không đọc được achievement: ", "Could not parse achievement: ") + error.Message);
            }
            return;
        }

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
            AppendLog(L("Worker kết thúc với exit code ", "Worker finished with exit code ") + exitCode + ".");
            progress.Style = ProgressBarStyle.Blocks;
            completedProcess.Dispose();
            workerProcess = null;
            workerOperation = null;
            CleanupWorker();

            if (completedOperation == "list")
            {
                bool loaded = exitCode == 0 && achievementList.Items.Count > 0;
                SetAchievementControlsEnabled(loaded);
                UpdateClaimButtonState();
                AppendLog(loaded
                    ? L("Đã tải ", "Loaded ") + achievementList.Items.Count + L(" achievement. Mặc định chưa chọn mục nào.", " achievements. Nothing is selected by default.")
                    : L("Không tải được danh sách achievement. Xem log phía trên.", "Could not load the achievement list. Check the log above."));
                return;
            }

            SetAchievementControlsEnabled(achievementList.Items.Count > 0);
            UpdateClaimButtonState();
            MessageBox.Show(
                exitCode == 0
                    ? L("Đã gửi thành công ", "Successfully submitted ") + lastClaimCount + L(" achievement đã chọn. Hãy kiểm tra Steam.", " selected achievement(s). Check Steam.")
                    : L("Quá trình chưa hoàn tất. Xem nhật ký để biết lỗi.", "The operation did not complete. Check the log for details."),
                exitCode == 0 ? L("Hoàn tất", "Complete") : L("Có lỗi", "Error"),
                MessageBoxButtons.OK,
                exitCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (exitCode == 0)
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
        logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
        logBox.SelectionStart = logBox.TextLength;
        logBox.ScrollToCaret();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (workerProcess != null && !workerProcess.HasExited)
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
