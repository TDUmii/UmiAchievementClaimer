using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal enum UmiButtonStyle { Primary, Accent, Secondary, Ghost }

internal static class UiTheme
{
    public static Color Canvas { get; private set; }
    public static Color Surface { get; private set; }
    public static Color SurfaceMuted { get; private set; }
    public static Color Border { get; private set; }
    public static Color Primary { get; private set; }
    public static Color PrimaryText { get; private set; }
    public static Color PrimaryHover { get; private set; }
    public static Color PrimaryPressed { get; private set; }
    public static Color Cyan { get; private set; }
    public static Color Text { get; private set; }
    public static Color MutedText { get; private set; }
    public static Color Success { get; private set; }
    public static Color SuccessSurface { get; private set; }
    public static Color Warning { get; private set; }
    public static Color Disabled { get; private set; }
    public static Color LogBackground { get; private set; }
    public static Color LogText { get; private set; }
    public static string ActiveName { get; private set; }

    public static event EventHandler Changed;

    static UiTheme()
    {
        ApplyPalette("Steam Midnight", Color.FromArgb(169, 153, 237),
            Color.FromArgb(12, 20, 34), Color.FromArgb(25, 40, 59), Color.FromArgb(19, 31, 47),
            Color.FromArgb(48, 65, 86), Color.FromArgb(116, 211, 232), Color.FromArgb(235, 241, 250),
            Color.FromArgb(164, 181, 203), Color.FromArgb(126, 217, 184), Color.FromArgb(24, 55, 53),
            Color.FromArgb(243, 179, 122), Color.FromArgb(45, 55, 75), false);
        LoadSavedTheme();
    }

    public static Font Font(float size, FontStyle style = FontStyle.Regular)
    {
        return new Font("Segoe UI", size, style, GraphicsUnit.Point);
    }

    public static Font DisplayFont(float size, FontStyle style = FontStyle.Regular)
    {
        try { return new Font("Bahnschrift", size, style, GraphicsUnit.Point); }
        catch { return Font(size, style); }
    }

    public static Font MonoFont(float size, FontStyle style = FontStyle.Regular)
    {
        return new Font("Consolas", size, style, GraphicsUnit.Point);
    }

    public static void ApplyPreset(string name)
    {
        if (string.Equals(name, "Carbon", StringComparison.OrdinalIgnoreCase))
        {
            ApplyPalette("Carbon", Color.FromArgb(222, 237, 247),
                Color.FromArgb(11, 13, 16), Color.FromArgb(23, 27, 32), Color.FromArgb(17, 21, 26),
                Color.FromArgb(42, 49, 58), Color.FromArgb(134, 212, 255), Color.FromArgb(244, 247, 250),
                Color.FromArgb(162, 172, 184), Color.FromArgb(128, 217, 181), Color.FromArgb(24, 55, 53),
                Color.FromArgb(242, 181, 110), Color.FromArgb(53, 59, 68), true);
            return;
        }
        if (string.Equals(name, "Arcade", StringComparison.OrdinalIgnoreCase))
        {
            ApplyPalette("Arcade", Color.FromArgb(255, 180, 94),
                Color.FromArgb(18, 13, 32), Color.FromArgb(34, 22, 59), Color.FromArgb(26, 16, 48),
                Color.FromArgb(74, 45, 105), Color.FromArgb(101, 232, 255), Color.FromArgb(255, 245, 232),
                Color.FromArgb(185, 167, 216), Color.FromArgb(117, 242, 194), Color.FromArgb(31, 61, 65),
                Color.FromArgb(255, 142, 158), Color.FromArgb(64, 54, 85), true);
            return;
        }
        ApplyPalette("Steam Midnight", Color.FromArgb(169, 153, 237),
            Color.FromArgb(12, 20, 34), Color.FromArgb(25, 40, 59), Color.FromArgb(19, 31, 47),
            Color.FromArgb(48, 65, 86), Color.FromArgb(116, 211, 232), Color.FromArgb(235, 241, 250),
            Color.FromArgb(164, 181, 203), Color.FromArgb(126, 217, 184), Color.FromArgb(24, 55, 53),
            Color.FromArgb(243, 179, 122), Color.FromArgb(45, 55, 75), true);
    }

    public static void ApplyCustomAccent(Color accent)
    {
        if (accent.A != 255) accent = Color.FromArgb(255, accent.R, accent.G, accent.B);
        Color cyan = Adjust(accent, 44);
        ApplyPalette("Custom", accent, Canvas, Surface, SurfaceMuted, Border, cyan, Text, MutedText,
            Success, SuccessSurface, Warning, Disabled, true);
    }

    private static void ApplyPalette(string name, Color primary, Color canvas, Color surface, Color surfaceMuted,
        Color border, Color cyan, Color text, Color mutedText, Color success, Color successSurface,
        Color warning, Color disabled, bool persist)
    {
        Canvas = canvas;
        Surface = surface;
        SurfaceMuted = surfaceMuted;
        Border = border;
        Primary = primary;
        PrimaryText = ContrastText(primary);
        PrimaryHover = Adjust(primary, 18);
        PrimaryPressed = Adjust(primary, -18);
        Cyan = cyan;
        Text = text;
        MutedText = mutedText;
        Success = success;
        SuccessSurface = successSurface;
        Warning = warning;
        Disabled = disabled;
        LogBackground = canvas;
        LogText = mutedText;
        ActiveName = name;
        if (persist) SaveTheme();
        if (Changed != null) Changed(null, EventArgs.Empty);
    }

    private static Color Adjust(Color color, int amount)
    {
        return Color.FromArgb(255,
            Clamp(color.R + amount), Clamp(color.G + amount), Clamp(color.B + amount));
    }

    private static int Clamp(int value) { return Math.Max(0, Math.Min(255, value)); }

    private static Color ContrastText(Color background)
    {
        int luminance = (background.R * 299 + background.G * 587 + background.B * 114) / 1000;
        return luminance >= 160 ? Color.FromArgb(12, 20, 34) : Color.FromArgb(250, 252, 255);
    }

    private static string ThemePath
    {
        get
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UmiAchievementClaimer");
            return Path.Combine(folder, "theme.cfg");
        }
    }

    private static void LoadSavedTheme()
    {
        try
        {
            if (!File.Exists(ThemePath)) return;
            string[] lines = File.ReadAllLines(ThemePath);
            string preset = lines.Length > 0 ? lines[0].Trim() : string.Empty;
            if (string.Equals(preset, "Carbon", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(preset, "Arcade", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(preset, "Steam Midnight", StringComparison.OrdinalIgnoreCase))
            {
                ApplyPreset(preset);
                return;
            }
            if (string.Equals(preset, "Custom", StringComparison.OrdinalIgnoreCase) && lines.Length > 1)
            {
                Color accent;
                if (TryParseColor(lines[1], out accent)) ApplyCustomAccent(accent);
            }
        }
        catch { }
    }

    private static bool TryParseColor(string value, out Color color)
    {
        color = Color.Empty;
        try
        {
            string raw = value.Trim().TrimStart('#');
            if (raw.Length != 6) return false;
            color = Color.FromArgb(Convert.ToInt32(raw.Substring(0, 2), 16), Convert.ToInt32(raw.Substring(2, 2), 16), Convert.ToInt32(raw.Substring(4, 2), 16));
            return true;
        }
        catch { return false; }
    }

    private static void SaveTheme()
    {
        try
        {
            string directory = Path.GetDirectoryName(ThemePath);
            Directory.CreateDirectory(directory);
            string accent = string.Format("#{0:X2}{1:X2}{2:X2}", Primary.R, Primary.G, Primary.B);
            File.WriteAllLines(ThemePath, new string[] { ActiveName, accent });
        }
        catch { }
    }

    public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = Math.Max(1, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
        GraphicsPath path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void StyleButton(Button button, UmiButtonStyle style)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = style == UmiButtonStyle.Secondary ? 1 : 0;
        button.FlatAppearance.BorderColor = Border;
        bool primary = style == UmiButtonStyle.Primary || style == UmiButtonStyle.Accent;
        button.Font = DisplayFont(9.1F, primary ? FontStyle.Bold : FontStyle.Regular);
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(0);
        button.Padding = new Padding(10, 0, 10, 0);
        button.UseVisualStyleBackColor = false;
        button.BackColor = primary ? Primary : (style == UmiButtonStyle.Secondary ? SurfaceMuted : Surface);
        button.ForeColor = primary ? PrimaryText : Text;
        button.FlatAppearance.MouseOverBackColor = primary ? PrimaryHover : Adjust(Surface, 14);
        button.FlatAppearance.MouseDownBackColor = primary ? PrimaryPressed : Border;
        UmiButton custom = button as UmiButton;
        if (custom != null) custom.StyleKind = style;
    }
}

internal sealed class UmiButton : Button
{
    private bool hovered;
    private bool pressed;
    private bool keyboardFocus;

    public UmiButtonStyle StyleKind { get; internal set; }

    public UmiButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { keyboardFocus = false; pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { keyboardFocus = true; Invalidate(); base.OnKeyDown(e); }
    protected override void OnGotFocus(EventArgs e) { keyboardFocus = (Control.MouseButtons == MouseButtons.None); Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { keyboardFocus = false; Invalidate(); base.OnLostFocus(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 2 || Height < 2) return;
        e.Graphics.Clear(Parent == null ? UiTheme.Surface : Parent.BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color fill = !Enabled ? UiTheme.Disabled : pressed ? FlatAppearance.MouseDownBackColor : hovered ? FlatAppearance.MouseOverBackColor : BackColor;
        using (GraphicsPath path = UiTheme.RoundedRectangle(new Rectangle(1, 1, Width - 2, Height - 2), 8))
        using (SolidBrush brush = new SolidBrush(fill))
        {
            e.Graphics.FillPath(brush, path);
            if (FlatAppearance.BorderSize > 0 || (Focused && keyboardFocus))
                using (Pen pen = new Pen(Focused && keyboardFocus ? UiTheme.Cyan : UiTheme.Border, Focused && keyboardFocus ? 2F : 1F))
                    e.Graphics.DrawPath(pen, path);
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(Padding.Left, 0, Width - Padding.Horizontal, Height),
            Enabled ? ForeColor : UiTheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}

internal sealed class UmiToolStripRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using (SolidBrush brush = new SolidBrush(UiTheme.Surface))
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using (Pen pen = new Pen(UiTheme.Border))
            e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1));
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        Color fill = e.Item.Selected ? UiTheme.Border : UiTheme.Surface;
        using (SolidBrush brush = new SolidBrush(fill))
            e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? UiTheme.Text : UiTheme.MutedText;
        base.OnRenderItemText(e);
    }
}
}
