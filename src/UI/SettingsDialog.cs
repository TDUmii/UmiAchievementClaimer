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
    private readonly Label languageLabel = new Label();
    private readonly Label themeLabel = new Label();
    private readonly Label activeThemeLabel = new Label();
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
        ClientSize = new Size(460, 360);
        Padding = new Padding(24);
        BackColor = UiTheme.Canvas;
        ForeColor = UiTheme.Text;
        Font = UiTheme.Font(9F);

        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

        headingLabel.Dock = DockStyle.Fill;
        headingLabel.Font = UiTheme.DisplayFont(16F, FontStyle.Bold);
        headingLabel.ForeColor = UiTheme.Text;
        headingLabel.Margin = new Padding(0);
        headingLabel.TextAlign = ContentAlignment.MiddleLeft;
        layout.Controls.Add(headingLabel, 0, 0);

        Panel languagePanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        languageLabel.Dock = DockStyle.Left;
        languageLabel.Width = 120;
        languageLabel.Font = UiTheme.DisplayFont(8F, FontStyle.Bold);
        languageLabel.ForeColor = UiTheme.MutedText;
        languageLabel.TextAlign = ContentAlignment.MiddleLeft;
        languageLabel.Margin = new Padding(0);
        UiTheme.StyleButton(languageButton, UmiButtonStyle.Secondary);
        languageButton.Text = "EN";
        languageButton.Width = 78;
        languageButton.Height = 30;
        languageButton.Dock = DockStyle.Right;
        languageButton.AccessibleName = "Language / Ngôn ngữ";
        languageButton.Click += delegate
        {
            English = !English;
            ApplyLanguage();
            if (languageChanged != null) languageChanged(English);
        };
        languagePanel.Controls.Add(languageButton);
        languagePanel.Controls.Add(languageLabel);
        layout.Controls.Add(languagePanel, 0, 1);

        themeLabel.Dock = DockStyle.Fill;
        themeLabel.Font = UiTheme.DisplayFont(8F, FontStyle.Bold);
        themeLabel.ForeColor = UiTheme.MutedText;
        themeLabel.TextAlign = ContentAlignment.MiddleLeft;
        themeLabel.Margin = new Padding(0);
        layout.Controls.Add(themeLabel, 0, 2);

        TableLayoutPanel themes = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 0)
        };
        themes.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        themes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        themes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        themes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        ConfigureThemeButton(midnightButton, "MIDNIGHT", "Steam Midnight", delegate { UiTheme.ApplyPreset("Steam Midnight"); });
        ConfigureThemeButton(carbonButton, "CARBON", "Carbon", delegate { UiTheme.ApplyPreset("Carbon"); });
        ConfigureThemeButton(arcadeButton, "ARCADE", "Arcade", delegate { UiTheme.ApplyPreset("Arcade"); });
        themes.Controls.Add(midnightButton, 0, 0);
        themes.Controls.Add(carbonButton, 1, 0);
        themes.Controls.Add(arcadeButton, 2, 0);
        layout.Controls.Add(themes, 0, 3);

        Panel customPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0) };
        activeThemeLabel.Dock = DockStyle.Left;
        activeThemeLabel.Width = 210;
        activeThemeLabel.ForeColor = UiTheme.MutedText;
        activeThemeLabel.TextAlign = ContentAlignment.MiddleLeft;
        UiTheme.StyleButton(customAccentButton, UmiButtonStyle.Ghost);
        customAccentButton.Text = "CUSTOM ACCENT";
        customAccentButton.Dock = DockStyle.Right;
        customAccentButton.Width = 150;
        customAccentButton.Height = 30;
        customAccentButton.Click += ChooseCustomAccent;
        customPanel.Controls.Add(customAccentButton);
        customPanel.Controls.Add(activeThemeLabel);
        layout.Controls.Add(customPanel, 0, 4);

        UiTheme.StyleButton(closeButton, UmiButtonStyle.Primary);
        closeButton.DialogResult = DialogResult.OK;
        closeButton.Dock = DockStyle.Right;
        closeButton.Width = 116;
        closeButton.Height = 30;
        layout.Controls.Add(closeButton, 0, 5);

        Controls.Add(layout);
        AcceptButton = closeButton;
        UiTheme.Changed += ThemeChanged;
        ApplyLanguage();
        RefreshThemeState();
    }

    private void ConfigureThemeButton(Button button, string text, string preset, Action action)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Height = 34;
        button.Margin = new Padding(0, 0, 6, 0);
        button.AccessibleDescription = preset;
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
        UiTheme.StyleButton(languageButton, UmiButtonStyle.Secondary);
        UiTheme.StyleButton(customAccentButton, UmiButtonStyle.Ghost);
        UiTheme.StyleButton(midnightButton, UiTheme.ActiveName == "Steam Midnight" ? UmiButtonStyle.Primary : UmiButtonStyle.Ghost);
        UiTheme.StyleButton(carbonButton, UiTheme.ActiveName == "Carbon" ? UmiButtonStyle.Primary : UmiButtonStyle.Ghost);
        UiTheme.StyleButton(arcadeButton, UiTheme.ActiveName == "Arcade" ? UmiButtonStyle.Primary : UmiButtonStyle.Ghost);
        activeThemeLabel.Text = (English ? "ACTIVE" : "ĐANG DÙNG") + "  /  " + UiTheme.ActiveName;
        Invalidate(true);
    }

    private void ApplyLanguage()
    {
        Text = English ? "Umi settings" : "Cài đặt Umi";
        headingLabel.Text = English ? "SETTINGS" : "CÀI ĐẶT";
        languageLabel.Text = English ? "LANGUAGE" : "NGÔN NGỮ";
        themeLabel.Text = English ? "THEME" : "GIAO DIỆN";
        languageButton.Text = English ? "EN" : "VI";
        languageButton.AccessibleDescription = English ? "Switch to Vietnamese" : "Chuyển sang English";
        customAccentButton.Text = English ? "CUSTOM ACCENT" : "MÀU NHẤN RIÊNG";
        closeButton.Text = English ? "DONE" : "XONG";
        activeThemeLabel.Text = (English ? "ACTIVE" : "ĐANG DÙNG") + "  /  " + UiTheme.ActiveName;
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
