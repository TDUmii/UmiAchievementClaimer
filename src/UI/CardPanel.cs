using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal sealed class CardPanel : Panel
{
    public CardPanel()
    {
        BackColor = UiTheme.Surface;
        Padding = new Padding(20);
        Margin = new Padding(0);
        DoubleBuffered = true;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent == null ? UiTheme.Canvas : Parent.BackColor);
        if (Width < 2 || Height < 2) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath path = UiTheme.RoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), 14))
        using (SolidBrush brush = new SolidBrush(BackColor))
        using (Pen pen = new Pen(UiTheme.Border))
        {
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        }
    }
}
}