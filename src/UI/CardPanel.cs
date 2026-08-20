using System.Drawing;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal sealed class CardPanel : Panel
{
    public CardPanel()
    {
        BackColor = UiTheme.Surface;
        Padding = new Padding(18);
        Margin = new Padding(0, 0, 0, 12);
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using (Pen pen = new Pen(UiTheme.Border))
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
}
