using System.Drawing;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal sealed class ClaimConfirmationDialog : Form
{
    private readonly ToolTip gameToolTip = new ToolTip();

    public ClaimConfirmationDialog(DetectedGame game, int selectedCount, int totalCount, bool english)
    {
        Text = english ? "Confirm achievement claim" : "Xác nhận claim achievement";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(490, 350);
        Padding = new Padding(24);
        BackColor = UiTheme.Canvas;
        ForeColor = UiTheme.Text;
        Font = UiTheme.Font(10F);

        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        Label heading = new Label
        {
            Text = english ? "Claim selected achievements?" : "Claim achievement đã chọn?",
            Dock = DockStyle.Fill,
            Font = UiTheme.Font(16F, FontStyle.Bold),
            Margin = new Padding(0),
            UseMnemonic = false
        };
        layout.Controls.Add(heading, 0, 0);

        TableLayoutPanel gameInfo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12, 7, 12, 7),
            Margin = new Padding(0, 0, 0, 8),
            BackColor = UiTheme.SurfaceMuted
        };
        gameInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        gameInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        gameInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 21F));
        Label gameName = new Label
        {
            Text = game.Name,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Font = UiTheme.Font(11F, FontStyle.Bold),
            Margin = new Padding(0),
            UseMnemonic = false
        };
        gameToolTip.SetToolTip(gameName, game.Name);
        gameInfo.Controls.Add(gameName, 0, 0);
        gameInfo.Controls.Add(new Label
        {
            Text = "Steam AppID: " + game.AppId,
            Dock = DockStyle.Fill,
            ForeColor = UiTheme.MutedText,
            Margin = new Padding(0),
            AutoEllipsis = true,
            UseMnemonic = false
        }, 0, 1);
        layout.Controls.Add(gameInfo, 0, 1);

        layout.Controls.Add(new Label
        {
            Text = english
                ? "Only " + selectedCount + " of " + totalCount + " achievements will be claimed."
                : "Chỉ claim " + selectedCount + " achievement đã chọn (tổng: " + totalCount + ").",
            Dock = DockStyle.Fill,
            Font = UiTheme.Font(10F, FontStyle.Bold),
            Margin = new Padding(0),
            UseMnemonic = false
        }, 0, 2);
        layout.Controls.Add(new Label
        {
            Text = english
                ? "Close the game before continuing.\n\nThis changes your Steam profile and may not be fully reversible."
                : "Hãy đóng game trước khi tiếp tục.\n\nThao tác này thay đổi hồ sơ Steam và có thể không hoàn tác đầy đủ.",
            Dock = DockStyle.Fill,
            ForeColor = UiTheme.Warning,
            Margin = new Padding(0, 0, 0, 12),
            UseMnemonic = false
        }, 0, 3);

        TableLayoutPanel actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Button cancelButton = new UmiButton();
        UiTheme.StyleButton(cancelButton, UmiButtonStyle.Secondary);
        cancelButton.Text = english ? "Cancel" : "Hủy";
        cancelButton.Dock = DockStyle.Fill;
        cancelButton.Margin = new Padding(0, 0, 6, 0);
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.TabIndex = 0;

        Button claimButton = new UmiButton();
        UiTheme.StyleButton(claimButton, UmiButtonStyle.Primary);
        claimButton.Text = english ? "Claim " + selectedCount + " selected" : "Claim " + selectedCount + " đã chọn";
        claimButton.Dock = DockStyle.Fill;
        claimButton.Margin = new Padding(6, 0, 0, 0);
        claimButton.DialogResult = DialogResult.OK;
        claimButton.TabIndex = 1;
        actions.Controls.Add(cancelButton, 0, 0);
        actions.Controls.Add(claimButton, 1, 0);
        layout.Controls.Add(actions, 0, 4);
        Controls.Add(layout);

        CancelButton = cancelButton;
        AcceptButton = cancelButton;
        Shown += delegate { cancelButton.Focus(); };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            gameToolTip.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnHandleCreated(System.EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowTheme.Apply(this);
    }
}
}
