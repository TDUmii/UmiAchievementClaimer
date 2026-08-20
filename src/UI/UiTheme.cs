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
    public static readonly Color Canvas = Color.FromArgb(244, 247, 251);
    public static readonly Color Surface = Color.White;
    public static readonly Color SurfaceMuted = Color.FromArgb(248, 250, 253);
    public static readonly Color Border = Color.FromArgb(220, 227, 237);
    public static readonly Color Navy = Color.FromArgb(5, 22, 76);
    public static readonly Color Blue = Color.FromArgb(24, 103, 220);
    public static readonly Color Orange = Color.FromArgb(255, 119, 0);
    public static readonly Color Text = Color.FromArgb(27, 38, 59);
    public static readonly Color MutedText = Color.FromArgb(101, 115, 139);
    public static readonly Color Success = Color.FromArgb(21, 128, 92);
    public static readonly Color Disabled = Color.FromArgb(232, 236, 242);
    public static readonly Color LogBackground = Color.FromArgb(16, 25, 44);
    public static readonly Color LogText = Color.FromArgb(218, 226, 238);

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
        }
        else if (style == UmiButtonStyle.Accent)
        {
            button.BackColor = Orange;
            button.ForeColor = Color.White;
        }
        else if (style == UmiButtonStyle.Secondary)
        {
            button.BackColor = Surface;
            button.ForeColor = Text;
        }
        else
        {
            button.BackColor = SurfaceMuted;
            button.ForeColor = MutedText;
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
