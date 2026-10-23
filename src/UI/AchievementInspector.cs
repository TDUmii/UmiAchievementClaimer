using System;
using System.Drawing;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
    internal sealed class AchievementInspector : Form
    {
        internal AchievementInspector(AchievementItem item, Image artwork, bool english)
        {
            Text = english ? "Achievement details" : "Chi tiết thành tích";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(550, 340);
            MinimumSize = new Size(470, 320);
            BackColor = UiTheme.Canvas;
            ForeColor = UiTheme.Text;
            Font = UiTheme.Font(10F);
            Padding = new Padding(20);
            ShowIcon = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            var banner = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(12) };
            var icon = new PictureBox { Dock = DockStyle.Left, Width = 64, Image = artwork,
                SizeMode = PictureBoxSizeMode.Zoom, BackColor = UiTheme.SurfaceMuted };
            if (artwork == null)
            {
                var symbol = new Label { Dock = DockStyle.Fill, Text = "◆", Font = UiTheme.DisplayFont(24F),
                    TextAlign = ContentAlignment.MiddleCenter, ForeColor = UiTheme.Primary };
                icon.Controls.Add(symbol);
            }
            var heading = new Label { Dock = DockStyle.Fill, Padding = new Padding(14, 0, 0, 0),
                Text = item.Name, Font = UiTheme.DisplayFont(16F, FontStyle.Bold),
                ForeColor = UiTheme.Text, TextAlign = ContentAlignment.MiddleLeft };
            banner.Controls.Add(heading);banner.Controls.Add(icon);
            layout.Controls.Add(banner, 0, 0);

            var status = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = item.Achieved ? UiTheme.Success : UiTheme.Warning,
                Font = UiTheme.DisplayFont(10F, FontStyle.Bold),
                Text = (item.Achieved ? (english ? "UNLOCKED" : "ĐÃ MỞ") : (english ? "LOCKED" : "CHƯA MỞ"))
                    + "   •   Steam API ID: " + item.Id };
            layout.Controls.Add(status, 0, 1);

            var description = new Label { Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface, ForeColor = UiTheme.Text, Padding = new Padding(16, 18, 16, 12),
                AutoEllipsis = true, Font = UiTheme.Font(10.5F), TextAlign = ContentAlignment.TopLeft,
                Text = string.IsNullOrWhiteSpace(item.Description)
                    ? (english ? "No description provided by Steam." : "Steam không cung cấp mô tả.")
                    : item.Description };
            ToolTip tooltip = new ToolTip();
            tooltip.SetToolTip(description, description.Text);
            layout.Controls.Add(description, 0, 2);

            var close = new UmiButton { Dock = DockStyle.Right, Width = 108, DialogResult = DialogResult.OK,
                Text = english ? "Close" : "Đóng" };
            UiTheme.StyleButton(close, UmiButtonStyle.Secondary);
            var footer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
            footer.Controls.Add(close);
            layout.Controls.Add(footer, 0, 3);
            Controls.Add(layout);
            AcceptButton = close;
            CancelButton = close;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            WindowTheme.Apply(this);
        }
    }
}
