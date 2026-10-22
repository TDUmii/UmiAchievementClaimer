using System;
using System.Drawing;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal sealed class ThemePickerDialog : Form
{
    private readonly Label activeLabel = new Label();
    private readonly Panel preview = new Panel();

    public ThemePickerDialog()
    {
        Text = "Umi theme";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(430, 300);
        Padding = new Padding(22);
        BackColor = UiTheme.Canvas;
        ForeColor = UiTheme.Text;
        Font = UiTheme.Font(9F);

        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        Label heading = new Label
        {
            Text = "THEME",
            Dock = DockStyle.Fill,
            Font = UiTheme.DisplayFont(15F, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            Margin = new Padding(0),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(heading, 0, 0);

        activeLabel.Dock = DockStyle.Fill;
        activeLabel.ForeColor = UiTheme.MutedText;
        activeLabel.TextAlign = ContentAlignment.MiddleLeft;
        activeLabel.Margin = new Padding(0);
        layout.Controls.Add(activeLabel, 0, 1);

        TableLayoutPanel choices = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 0)
        };
        choices.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        choices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        choices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        choices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        choices.Controls.Add(CreateChoice("MIDNIGHT", "Steam Midnight", "Steam Midnight"), 0, 0);
        choices.Controls.Add(CreateChoice("CARBON", "Carbon", "Carbon"), 1, 0);
        choices.Controls.Add(CreateChoice("ARCADE", "Arcade", "Arcade"), 2, 0);
        layout.Controls.Add(choices, 0, 2);

        TableLayoutPanel bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 10, 0, 0)
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        preview.Dock = DockStyle.Fill;
        preview.Height = 1;
        preview.MinimumSize = new Size(0, 0);
        preview.Margin = new Padding(0, 0, 12, 0);
        preview.Paint += PaintPreview;
        bottom.Controls.Add(preview, 0, 0);
        Button custom = new UmiButton();
        UiTheme.StyleButton(custom, UmiButtonStyle.Secondary);
        custom.Text = "CUSTOM ACCENT";
        custom.Dock = DockStyle.Fill;
        custom.Height = 1;
        custom.MinimumSize = new Size(0, 0);
        custom.Click += ChooseCustomAccent;
        bottom.Controls.Add(custom, 1, 0);
        layout.Controls.Add(bottom, 0, 3);

        Controls.Add(layout);
        Button cancelButton = CreateCancelButton();
        CancelButton = cancelButton;
        Controls.Add(cancelButton);
        UpdateActive();
    }

    private Button CreateChoice(string label, string hint, string preset)
    {
        Button button = new UmiButton();
        UiTheme.StyleButton(button, UiTheme.ActiveName == preset ? UmiButtonStyle.Primary : UmiButtonStyle.Ghost);
        button.Text = label;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0, 0, 6, 0);
        button.AccessibleDescription = hint;
        button.Click += delegate
        {
            UiTheme.ApplyPreset(preset);
            UpdateActive();
            DialogResult = DialogResult.OK;
            Close();
        };
        return button;
    }

    private Button CreateCancelButton()
    {
        Button cancel = new UmiButton();
        cancel.Text = "CLOSE";
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Size = new Size(1, 1);
        cancel.Location = new Point(-20, -20);
        return cancel;
    }

    private void ChooseCustomAccent(object sender, EventArgs e)
    {
        using (ColorDialog colors = new ColorDialog())
        {
            colors.FullOpen = true;
            colors.Color = UiTheme.Primary;
            if (colors.ShowDialog(this) != DialogResult.OK) return;
            UiTheme.ApplyCustomAccent(colors.Color);
            UpdateActive();
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void UpdateActive()
    {
        activeLabel.Text = "ACTIVE  /  " + UiTheme.ActiveName;
        preview.Invalidate();
    }

    private void PaintPreview(object sender, PaintEventArgs e)
    {
        e.Graphics.Clear(UiTheme.Canvas);
        using (SolidBrush surface = new SolidBrush(UiTheme.Surface))
            e.Graphics.FillRectangle(surface, 0, 0, preview.Width, preview.Height);
        using (SolidBrush accent = new SolidBrush(UiTheme.Primary))
            e.Graphics.FillRectangle(accent, 12, 12, 96, 14);
        using (SolidBrush cyan = new SolidBrush(UiTheme.Cyan))
            e.Graphics.FillRectangle(cyan, 12, 38, 58, 8);
        using (SolidBrush text = new SolidBrush(UiTheme.Text))
            e.Graphics.FillRectangle(text, 82, 38, Math.Max(20, preview.Width - 98), 8);
        using (Pen border = new Pen(UiTheme.Border))
            e.Graphics.DrawRectangle(border, 0, 0, preview.Width - 1, preview.Height - 1);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowTheme.Apply(this);
    }
}
}
