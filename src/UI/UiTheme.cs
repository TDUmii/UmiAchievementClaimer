using System.Drawing;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal enum UmiButtonStyle
{
    Primary,
    Accent,
    Secondary,
    Ghost
}

internal static class UiTheme
{
    public static readonly Color Canvas = Color.FromArgb(241, 245, 250);
    public static readonly Color Surface = Color.White;
    public static readonly Color SurfaceMuted = Color.FromArgb(246, 248, 252);
    public static readonly Color Border = Color.FromArgb(211, 220, 233);
    public static readonly Color Navy = Color.FromArgb(7, 25, 78);
    public static readonly Color Blue = Color.FromArgb(24, 99, 214);
    public static readonly Color Orange = Color.FromArgb(255, 119, 0);
    public static readonly Color Text = Color.FromArgb(24, 35, 55);
    public static readonly Color MutedText = Color.FromArgb(92, 108, 133);
    public static readonly Color Success = Color.FromArgb(21, 128, 92);
    public static readonly Color SuccessSurface = Color.FromArgb(232, 248, 241);
    public static readonly Color Disabled = Color.FromArgb(232, 236, 242);
    public static readonly Color LogBackground = Color.FromArgb(13, 24, 45);
    public static readonly Color LogText = Color.FromArgb(224, 231, 242);

    public static Font Font(float size, FontStyle style = FontStyle.Regular)
    {
        return new Font("Segoe UI", size, style, GraphicsUnit.Point);
    }

    public static void StyleButton(Button button, UmiButtonStyle style)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = style == UmiButtonStyle.Secondary ? 1 : 0;
        button.FlatAppearance.BorderColor = Border;
        button.Font = Font(9F, style == UmiButtonStyle.Primary || style == UmiButtonStyle.Accent
            ? FontStyle.Bold
            : FontStyle.Regular);
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(4, 0, 0, 0);
        button.Padding = new Padding(8, 0, 8, 0);
        button.UseVisualStyleBackColor = false;

        if (style == UmiButtonStyle.Primary)
        {
            button.BackColor = Navy;
            button.ForeColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(13, 38, 106);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(3, 15, 58);
        }
        else if (style == UmiButtonStyle.Accent)
        {
            button.BackColor = Orange;
            button.ForeColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 99, 0);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(203, 82, 0);
        }
        else if (style == UmiButtonStyle.Secondary)
        {
            button.BackColor = Surface;
            button.ForeColor = Text;
            button.FlatAppearance.MouseOverBackColor = SurfaceMuted;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(232, 237, 245);
        }
        else
        {
            button.BackColor = SurfaceMuted;
            button.ForeColor = MutedText;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(237, 241, 247);
        }
    }

    public static Label SectionTitle()
    {
        return new Label
        {
            AutoSize = true,
            Font = Font(10F, FontStyle.Bold),
            ForeColor = Navy,
            Margin = new Padding(0)
        };
    }
}
}
