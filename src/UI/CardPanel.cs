using System.Drawing;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal sealed class CardPanel : Panel
{
    public Color AccentColor { get; set; }

    public CardPanel()
    {
        BackColor = UiTheme.Surface;
        Padding = new Padding(18);
        Margin = new Padding(0, 0, 0, 12);
        AccentColor = Color.Transparent;
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using (Pen pen = new Pen(UiTheme.Border))
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        if (AccentColor != Color.Transparent)
        {
            using (SolidBrush brush = new SolidBrush(AccentColor))
                e.Graphics.FillRectangle(brush, 0, 0, 4, Height);
        }
    }
}
}
