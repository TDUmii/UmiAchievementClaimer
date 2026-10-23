using System;
using System.Drawing;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal sealed class SettingsDialog : Form
{
    private readonly Button languageButton = new UmiButton();
    private readonly Button midnightButton = new UmiButton();
    private readonly Button carbonButton = new UmiButton();
    private readonly Button arcadeButton = new UmiButton();
    private readonly Button customAccentButton = new UmiButton();
    private readonly Button closeButton = new UmiButton();
    private readonly Label headingLabel = new Label();
    private readonly Label subtitleLabel = new Label();
    private readonly Label languageLabel = new Label();
    private readonly Label languageHintLabel = new Label();
    private readonly Label themeLabel = new Label();
    private readonly Label activeThemeLabel = new Label();
    private readonly Panel languageCard = new Panel();
    private readonly Panel themeCard = new Panel();
    private readonly Action<bool> languageChanged;

    public bool English { get; private set; }

    public SettingsDialog() : this(true, null) { }

    public SettingsDialog(bool english, Action<bool> languageChanged)
    {
        English = english;
        this.languageChanged = languageChanged;
        Text = "Umi settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(574, 468);
        Padding = new Padding(24);
        BackColor = UiTheme.Canvas;
        ForeColor = UiTheme.Text;
        Font = UiTheme.Font(10F);

        TableLayoutPanel layout = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

        Panel title = new Panel { Dock = DockStyle.Fill };
        headingLabel.Dock = DockStyle.Top;
        headingLabel.Height = 38;
        headingLabel.Font = UiTheme.DisplayFont(20F, FontStyle.Bold);
        headingLabel.ForeColor = UiTheme.Text;
        subtitleLabel.Dock = DockStyle.Fill;
        subtitleLabel.Font = UiTheme.Font(10F);
        subtitleLabel.ForeColor = UiTheme.MutedText;
        subtitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        title.Controls.Add(subtitleLabel);
        title.Controls.Add(headingLabel);
        layout.Controls.Add(title, 0, 0);

        languageCard.Dock = DockStyle.Fill;
        languageCard.BackColor = UiTheme.Surface;
        languageCard.Margin = new Padding(0, 0, 0, 12);
        languageCard.Padding = new Padding(18, 12, 18, 12);
        Panel languageText = new Panel { Dock = DockStyle.Fill };
        languageLabel.Dock = DockStyle.Top;
        languageLabel.Height = 25;
        languageLabel.Font = UiTheme.DisplayFont(11F, FontStyle.Bold);
        languageHintLabel.Dock = DockStyle.Fill;
        languageHintLabel.TextAlign = ContentAlignment.TopLeft;
        languageHintLabel.Font = UiTheme.Font(9.5F);
        languageHintLabel.ForeColor = UiTheme.MutedText;
        languageText.Controls.Add(languageHintLabel);
        languageText.Controls.Add(languageLabel);
        UiTheme.StyleButton(languageButton, UmiButtonStyle.Secondary);
        languageButton.Width = 106;
        languageButton.Dock = DockStyle.Right;
        languageButton.Margin = new Padding(14, 0, 0, 0);
        languageButton.AccessibleName = "Language / Ngôn ngữ";
        languageButton.Click += delegate
        {
            English = !English;
            ApplyLanguage();
            if (languageChanged != null) languageChanged(English);
        };
        languageCard.Controls.Add(languageText);
        languageCard.Controls.Add(languageButton);
        layout.Controls.Add(languageCard, 0, 1);

        themeCard.Dock = DockStyle.Fill;
        themeCard.BackColor = UiTheme.Surface;
        themeCard.Padding = new Padding(18, 13, 18, 14);
        themeCard.Margin = new Padding(0);
        TableLayoutPanel themeLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
        themeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        themeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        themeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        themeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        Panel themeHeading = new Panel { Dock = DockStyle.Fill };
        themeLabel.Dock = DockStyle.Top;
        themeLabel.Height = 25;
        themeLabel.Font = UiTheme.DisplayFont(11F, FontStyle.Bold);
        activeThemeLabel.Dock = DockStyle.Fill;
        activeThemeLabel.Font = UiTheme.Font(9F);
        activeThemeLabel.ForeColor = UiTheme.MutedText;
        themeHeading.Controls.Add(activeThemeLabel);
        themeHeading.Controls.Add(themeLabel);
        themeLayout.Controls.Add(themeHeading, 0, 0);

        TableLayoutPanel themes = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3,
            RowCount = 1, Margin = new Padding(0, 8, 0, 8) };
        for (int i = 0; i < 3; i++) themes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        themes.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        ConfigureThemeButton(midnightButton, "Steam Midnight", delegate { UiTheme.ApplyPreset("Steam Midnight"); });
        ConfigureThemeButton(carbonButton, "Carbon", delegate { UiTheme.ApplyPreset("Carbon"); });
        ConfigureThemeButton(arcadeButton, "Arcade", delegate { UiTheme.ApplyPreset("Arcade"); });
        themes.Controls.Add(midnightButton, 0, 0);
        themes.Controls.Add(carbonButton, 1, 0);
        themes.Controls.Add(arcadeButton, 2, 0);
        themeLayout.Controls.Add(themes, 0, 1);

        Panel customRow = new Panel { Dock = DockStyle.Fill };
        Label customNote = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Text = "Personalize your accent color", ForeColor = UiTheme.MutedText,
            Font = UiTheme.Font(9.5F) };
        UiTheme.StyleButton(customAccentButton, UmiButtonStyle.Secondary);
        customAccentButton.Dock = DockStyle.Right;
        customAccentButton.Width = 164;
        customAccentButton.Click += ChooseCustomAccent;
        customRow.Controls.Add(customNote);
        customRow.Controls.Add(customAccentButton);
        customNote.Name = "customNote";
        themeLayout.Controls.Add(customRow, 0, 2);
        themeCard.Controls.Add(themeLayout);
        layout.Controls.Add(themeCard, 0, 2);

        Panel footer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 16, 0, 0) };
        Label savedInfo = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Text = "Changes are saved automatically", ForeColor = UiTheme.MutedText, Font = UiTheme.Font(9F), Name = "savedInfo" };
        UiTheme.StyleButton(closeButton, UmiButtonStyle.Primary);
        closeButton.DialogResult = DialogResult.OK;
        closeButton.Dock = DockStyle.Right;
        closeButton.Width = 124;
        footer.Controls.Add(savedInfo);
        footer.Controls.Add(closeButton);
        layout.Controls.Add(footer, 0, 3);
        Controls.Add(layout);

        AcceptButton = closeButton;
        CancelButton = closeButton;
        UiTheme.Changed += ThemeChanged;
        ApplyLanguage();
        RefreshThemeState();
    }

    private void ConfigureThemeButton(Button button, string preset, Action action)
    {
        button.Text = preset;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0, 0, 8, 0);
        button.AccessibleName = "Theme " + preset;
        button.Click += delegate { action(); };
    }

    private void ChooseCustomAccent(object sender, EventArgs e)
    {
        using (ColorDialog colors = new ColorDialog())
        {
            colors.FullOpen = true;
            colors.Color = UiTheme.Primary;
            if (colors.ShowDialog(this) == DialogResult.OK)
                UiTheme.ApplyCustomAccent(colors.Color);
        }
    }

    private void ThemeChanged(object sender, EventArgs e)
    {
        if (IsDisposed || Disposing) return;
        RefreshThemeState();
    }

    private void RefreshThemeState()
    {
        BackColor = UiTheme.Canvas;
        ForeColor = UiTheme.Text;
        languageCard.BackColor = themeCard.BackColor = UiTheme.Surface;
        headingLabel.ForeColor = languageLabel.ForeColor = themeLabel.ForeColor = UiTheme.Text;
        subtitleLabel.ForeColor = languageHintLabel.ForeColor = activeThemeLabel.ForeColor = UiTheme.MutedText;
        UiTheme.StyleButton(languageButton, UmiButtonStyle.Secondary);
        UiTheme.StyleButton(customAccentButton, UmiButtonStyle.Secondary);
        UiTheme.StyleButton(midnightButton, UiTheme.ActiveName == "Steam Midnight" ? UmiButtonStyle.Primary : UmiButtonStyle.Secondary);
        UiTheme.StyleButton(carbonButton, UiTheme.ActiveName == "Carbon" ? UmiButtonStyle.Primary : UmiButtonStyle.Secondary);
        UiTheme.StyleButton(arcadeButton, UiTheme.ActiveName == "Arcade" ? UmiButtonStyle.Primary : UmiButtonStyle.Secondary);
        UiTheme.StyleButton(closeButton, UmiButtonStyle.Primary);
        activeThemeLabel.Text = (English ? "Active theme: " : "Đang sử dụng: ") + UiTheme.ActiveName;
        Invalidate(true);
    }

    private void ApplyLanguage()
    {
        Text = English ? "Umi settings" : "Cài đặt Umi";
        headingLabel.Text = English ? "Settings" : "Cài đặt";
        subtitleLabel.Text = English ? "Make Umi Achievement Claimer yours" : "Tùy chỉnh Umi Achievement Claimer";
        languageLabel.Text = English ? "Language" : "Ngôn ngữ";
        languageHintLabel.Text = English ? "Interface language" : "Ngôn ngữ giao diện";
        languageButton.Text = English ? "English  ▾" : "Tiếng Việt  ▾";
        languageButton.AccessibleDescription = English ? "Switch to Vietnamese" : "Chuyển sang English";
        themeLabel.Text = English ? "Appearance" : "Giao diện";
        activeThemeLabel.Text = (English ? "Active theme: " : "Đang sử dụng: ") + UiTheme.ActiveName;
        customAccentButton.Text = English ? "Custom color" : "Màu tùy chỉnh";
        closeButton.Text = English ? "Done" : "Hoàn tất";
        Panel customRow = themeCard.Controls[0].Controls[2] as Panel;
        if (customRow != null && customRow.Controls["customNote"] != null)
            customRow.Controls["customNote"].Text = English ? "Choose a personal accent" : "Chọn màu nhấn riêng";
        Control footer = Controls[0].Controls[3];
        if (footer.Controls["savedInfo"] != null)
            footer.Controls["savedInfo"].Text = English ? "Changes save automatically" : "Các thay đổi được lưu tự động";
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowTheme.Apply(this);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        UiTheme.Changed -= ThemeChanged;
        base.OnFormClosed(e);
    }
}
}
