using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal enum UmiButtonStyle { Primary, Accent, Secondary, Ghost }

internal static class UiTheme
{
    public static readonly Color Canvas = Color.FromArgb(12, 20, 34);
    public static readonly Color Surface = Color.FromArgb(25, 40, 59);
    public static readonly Color SurfaceMuted = Color.FromArgb(19, 31, 47);
    public static readonly Color Border = Color.FromArgb(48, 65, 86);
    public static readonly Color Primary = Color.FromArgb(169, 153, 237);
    public static readonly Color Cyan = Color.FromArgb(116, 211, 232);
    public static readonly Color Text = Color.FromArgb(235, 241, 250);
    public static readonly Color MutedText = Color.FromArgb(164, 181, 203);
    public static readonly Color Success = Color.FromArgb(126, 217, 184);
    public static readonly Color SuccessSurface = Color.FromArgb(24, 55, 53);
    public static readonly Color Warning = Color.FromArgb(243, 179, 122);
    public static readonly Color Disabled = Color.FromArgb(45, 55, 75);
    public static readonly Color LogBackground = Canvas;
    public static readonly Color LogText = MutedText;

    public static Font Font(float size, FontStyle style = FontStyle.Regular)
    {
        return new Font("Segoe UI", size, style, GraphicsUnit.Point);
    }

    public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = System.Math.Max(1, System.Math.Min(radius * 2, System.Math.Min(bounds.Width, bounds.Height)));
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
        button.Font = Font(9F, primary ? FontStyle.Bold : FontStyle.Regular);
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(0);
        button.Padding = new Padding(10, 0, 10, 0);
        button.UseVisualStyleBackColor = false;
        button.BackColor = primary ? Primary : (style == UmiButtonStyle.Secondary ? SurfaceMuted : Surface);
        button.ForeColor = primary ? Canvas : Text;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(187, 173, 248) : Color.FromArgb(39, 56, 78);
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(151, 133, 222) : Border;
    }
}

internal sealed class UmiButton : Button
{
    private bool hovered;
    private bool pressed;

    public UmiButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnMouseEnter(System.EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(System.EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(System.EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

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
            if (FlatAppearance.BorderSize > 0 || Focused)
                using (Pen pen = new Pen(Focused ? UiTheme.Cyan : UiTheme.Border, Focused ? 2F : 1F))
                    e.Graphics.DrawPath(pen, path);
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(Padding.Left, 0, Width - Padding.Horizontal, Height),
            Enabled ? ForeColor : UiTheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}
}