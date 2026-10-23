using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal sealed class AchievementGrid : DataGridView
{
    public readonly List<AchievementItem> Items = new List<AchievementItem>();
    private readonly HashSet<AchievementItem> checkedItems = new HashSet<AchievementItem>();
    private string query = string.Empty;
    private int filter;
    private bool english = true;
    private bool updating;
    private Font titleFont;

    public event EventHandler ChecksChanged;
    public event EventHandler SelectedItemChanged;
    public Func<AchievementItem, Image> ArtworkProvider { get; set; }
    public string EmptyStateMessage { get; set; }

    public AchievementGrid()
    {
        DoubleBuffered = true;
        BackgroundColor = UiTheme.Surface;
        BorderStyle = BorderStyle.None;
        GridColor = UiTheme.Border;
        Font = UiTheme.Font(9.5F);
        ForeColor = UiTheme.Text;
        RowHeadersVisible = false;
        ColumnHeadersVisible = false;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        AllowUserToResizeColumns = false;
        AutoGenerateColumns = false;
        MultiSelect = false;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        CellBorderStyle = DataGridViewCellBorderStyle.None;
        EditMode = DataGridViewEditMode.EditOnEnter;
        // The list uses a themed companion scrollbar so it stays inside the Steam Midnight surface.
        ScrollBars = ScrollBars.None;
        EnableHeadersVisualStyles = false;
        ShowCellToolTips = true;
        AccessibleName = "Achievements";
        DefaultCellStyle.BackColor = UiTheme.Surface;
        DefaultCellStyle.ForeColor = UiTheme.Text;
        DefaultCellStyle.SelectionBackColor = UiTheme.SurfaceMuted;
        DefaultCellStyle.SelectionForeColor = UiTheme.Text;
        DefaultCellStyle.NullValue = string.Empty;

        Columns.Add(new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "Select", Width = 40 });
        Columns.Add(new DataGridViewTextBoxColumn { Name = "Symbol", HeaderText = "", Width = 44, ReadOnly = true });
        Columns.Add(new DataGridViewTextBoxColumn { Name = "Achievement", HeaderText = "Achievement", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
        Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 105, ReadOnly = true });
        foreach (DataGridViewColumn column in Columns)
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        ApplyMetrics();

        CurrentCellDirtyStateChanged += delegate
        {
            if (IsCurrentCellDirty && CurrentCell != null && CurrentCell.ColumnIndex == 0)
                CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        CellValueChanged += delegate(object sender, DataGridViewCellEventArgs e)
        {
            if (updating || e.RowIndex < 0 || e.ColumnIndex != 0)
                return;
            DataGridViewRow row = Rows[e.RowIndex];
            AchievementItem item = row.Tag as AchievementItem;
            if (item == null)
                return;
            if (Convert.ToBoolean(row.Cells[0].Value))
                checkedItems.Add(item);
            else
                checkedItems.Remove(item);
            InvalidateRow(e.RowIndex);
            RaiseChecksChanged();
        };
        SelectionChanged += delegate
        {
            if (!updating && SelectedItemChanged != null)
                SelectedItemChanged(this, EventArgs.Empty);
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Rows.Count == 0 && !string.IsNullOrEmpty(EmptyStateMessage) && ClientSize.Height >= 45)
        {
            Rectangle textArea = new Rectangle(24, 8, Math.Max(1, ClientSize.Width - 48), Math.Max(1, ClientSize.Height - 16));
            using (Font font = UiTheme.DisplayFont(11.5F))
                TextRenderer.DrawText(e.Graphics, EmptyStateMessage, font, textArea, UiTheme.MutedText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
    }

    public IEnumerable<AchievementItem> CheckedItems
    {
        get { return Items.Where(checkedItems.Contains).ToArray(); }
    }

    public AchievementItem SelectedItem
    {
        get { return CurrentRow == null ? null : CurrentRow.Tag as AchievementItem; }
    }

    public int VisibleCount { get { return Rows.Count; } }

    public void ScrollBy(int delta)
    {
        if (Rows.Count == 0)
            return;
        int visible = VisibleRows();
        int maximum = Math.Max(0, Rows.Count - visible);
        int current = FirstDisplayedScrollingRowIndex;
        if (current < 0)
            current = 0;
        int step = Math.Max(1, Math.Abs(delta) / 120);
        int next = Math.Max(0, Math.Min(maximum, current + (delta < 0 ? step : -step)));
        if (next == current)
            return;
        try { FirstDisplayedScrollingRowIndex = next; }
        catch (ArgumentOutOfRangeException) { }
        Invalidate();
    }

    public int VisibleRows()
    {
        return Math.Max(1, ClientSize.Height / Math.Max(1, RowTemplate.Height));
    }

    public void RefreshTheme()
    {
        BackgroundColor = UiTheme.Surface;
        GridColor = UiTheme.Border;
        ForeColor = UiTheme.Text;
        DefaultCellStyle.BackColor = UiTheme.Surface;
        DefaultCellStyle.ForeColor = UiTheme.Text;
        DefaultCellStyle.SelectionBackColor = UiTheme.SurfaceMuted;
        DefaultCellStyle.SelectionForeColor = UiTheme.Text;
        Font = UiTheme.Font(9.5F);
        Invalidate();
    }

    public bool English
    {
        get { return english; }
        set
        {
            english = value;
            AccessibleName = english ? "Achievements" : "Danh sách achievement";
            Columns[0].HeaderText = english ? "Select" : "Chọn";
            Columns[3].HeaderText = english ? "Status" : "Trạng thái";
            foreach (DataGridViewRow row in Rows)
                row.Cells[3].Value = Status((AchievementItem)row.Tag);
            Invalidate();
        }
    }

    public void AddItem(AchievementItem item)
    {
        if (item == null)
            return;
        Items.Add(item);
        if (Matches(item))
        {
            AddRow(item);
            if (ReferenceEquals(SelectedItem, item) && SelectedItemChanged != null)
                SelectedItemChanged(this, EventArgs.Empty);
        }
    }

    public void ClearItems()
    {
        updating = true;
        try
        {
            Items.Clear();
            checkedItems.Clear();
            Rows.Clear();
        }
        finally { updating = false; }
        RaiseChecksChanged();
        if (SelectedItemChanged != null)
            SelectedItemChanged(this, EventArgs.Empty);
    }

    public void ApplyFilter(string search, int state)
    {
        AchievementItem current = SelectedItem;
        query = (search ?? string.Empty).Trim();
        filter = state;
        updating = true;
        SuspendLayout();
        try
        {
            Rows.Clear();
            foreach (AchievementItem item in Items)
                if (Matches(item))
                    AddRow(item);
            foreach (DataGridViewRow row in Rows)
                if (ReferenceEquals(row.Tag, current))
                {
                    CurrentCell = row.Cells[2];
                    break;
                }
        }
        finally
        {
            ResumeLayout();
            updating = false;
        }
        if (SelectedItemChanged != null)
            SelectedItemChanged(this, EventArgs.Empty);
        Invalidate();
    }

    public void SetChecks(Func<AchievementItem, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException("predicate");
        updating = true;
        try
        {
            foreach (DataGridViewRow row in Rows)
            {
                AchievementItem item = (AchievementItem)row.Tag;
                bool selected = predicate(item);
                if (selected)
                    checkedItems.Add(item);
                else
                    checkedItems.Remove(item);
                row.Cells[0].Value = selected;
            }
        }
        finally { updating = false; }
        Invalidate();
        RaiseChecksChanged();
    }

    public void ClearChecks()
    {
        checkedItems.Clear();
        updating = true;
        try
        {
            foreach (DataGridViewRow row in Rows)
                row.Cells[0].Value = false;
        }
        finally { updating = false; }
        Invalidate();
        RaiseChecksChanged();
    }

    private bool Matches(AchievementItem item)
    {
        return (filter != 1 || !item.Achieved) && (filter != 2 || item.Achieved)
            && (query.Length == 0 || Contains(item.Name, query) || Contains(item.Id, query) || Contains(item.Description, query));
    }

    private static bool Contains(string text, string search)
    {
        return text != null && text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string Status(AchievementItem item)
    {
        return english ? (item.Achieved ? "Unlocked" : "Locked") : (item.Achieved ? "Đã mở" : "Chưa mở");
    }

    private void AddRow(AchievementItem item)
    {
        bool previous = updating;
        updating = true;
        try
        {
            int index = Rows.Add(checkedItems.Contains(item), string.Empty,
                (item.Name ?? item.Id) + Environment.NewLine + (item.Description ?? string.Empty), Status(item));
            DataGridViewRow row = Rows[index];
            row.Tag = item;
            string tooltip = (item.Name ?? item.Id) + Environment.NewLine + (item.Description ?? string.Empty)
                + Environment.NewLine + "API ID: " + item.Id;
            foreach (DataGridViewCell cell in row.Cells)
                cell.ToolTipText = tooltip;
        }
        finally { updating = previous; }
    }

    private void RaiseChecksChanged()
    {
        if (ChecksChanged != null)
            ChecksChanged(this, EventArgs.Empty);
    }

    protected override bool ProcessDataGridViewKey(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && e.Modifiers == Keys.None && Enabled && CurrentRow != null)
        {
            DataGridViewCell checkbox = CurrentRow.Cells[0];
            checkbox.Value = !Convert.ToBoolean(checkbox.Value);
            e.Handled = true;
            return true;
        }
        return base.ProcessDataGridViewKey(e);
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData == Keys.Space && Enabled && CurrentRow != null)
        {
            DataGridViewCell checkbox = CurrentRow.Cells[0];
            checkbox.Value = !Convert.ToBoolean(checkbox.Value);
            return true;
        }
        return base.ProcessDialogKey(keyData);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (titleFont != null)
            titleFont.Dispose();
        titleFont = UiTheme.DisplayFont(Font.SizeInPoints, FontStyle.Bold);
        if (Columns.Count == 4)
            ApplyMetrics();
    }

    private void ApplyMetrics()
    {
        float scale = Font.Height / 15F;
        RowTemplate.Height = Math.Max(54, Font.Height * 3 + 8);
        foreach (DataGridViewRow row in Rows)
            row.Height = RowTemplate.Height;
        Columns[0].Width = Math.Max(36, (int)(40 * scale));
        Columns[1].Width = Math.Max(38, (int)(40 * scale));
        Columns[3].Width = Math.Max(92, (int)(96 * scale));
    }

    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            base.OnCellPainting(e);
            return;
        }
        AchievementItem item = Rows[e.RowIndex].Tag as AchievementItem;
        if (item == null)
        {
            base.OnCellPainting(e);
            return;
        }
        Rectangle bounds = e.CellBounds;
        bool selected = (e.State & DataGridViewElementStates.Selected) != 0;
        Color background = selected ? UiTheme.SurfaceMuted : UiTheme.Surface;
        using (SolidBrush brush = new SolidBrush(background))
            e.Graphics.FillRectangle(brush, bounds);
        using (Pen border = new Pen(UiTheme.Border))
            e.Graphics.DrawLine(border, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);

        int scale = Math.Max(1, Font.Height / 15);
        if (e.ColumnIndex == 0)
        {
            int side = Math.Max(16, Font.Height);
            Rectangle box = new Rectangle(bounds.Left + (bounds.Width - side) / 2, bounds.Top + (bounds.Height - side) / 2, side, side);
            bool isChecked = checkedItems.Contains(item);
            using (SolidBrush brush = new SolidBrush(isChecked ? UiTheme.Primary : UiTheme.Canvas))
                e.Graphics.FillRectangle(brush, box);
            using (Pen pen = new Pen(isChecked ? UiTheme.Primary : UiTheme.MutedText))
                e.Graphics.DrawRectangle(pen, box);
            if (isChecked)
                using (Pen pen = new Pen(UiTheme.PrimaryText, 2F))
                {
                    e.Graphics.DrawLines(pen, new Point[] {
                        new Point(box.Left + side / 4, box.Top + side / 2),
                        new Point(box.Left + side * 9 / 20, box.Top + side * 7 / 10),
                        new Point(box.Left + side * 4 / 5, box.Top + side / 3) });
                }
            if (Focused && CurrentCell != null && CurrentCell.RowIndex == e.RowIndex && CurrentCell.ColumnIndex == 0)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(box, 4, 4), UiTheme.Primary, background);
        }
        else if (e.ColumnIndex == 1)
        {
            Image artwork = ArtworkProvider == null ? null : ArtworkProvider(item);
            if (artwork != null)
            {
                int side = Math.Min(bounds.Width - 8, Font.Height * 2);
                Rectangle target = new Rectangle(bounds.Left + (bounds.Width - side) / 2, bounds.Top + (bounds.Height - side) / 2, side, side);
                InterpolationMode previous = e.Graphics.InterpolationMode;
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(artwork, target);
                e.Graphics.InterpolationMode = previous;
                e.Handled = true;
                return;
            }
            float x = bounds.Left + bounds.Width / 2F;
            float y = bounds.Top + bounds.Height / 2F;
            float r = Math.Min(bounds.Width * .3F, Font.Height);
            SmoothingMode oldMode = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(item.Achieved ? UiTheme.Success : UiTheme.Primary, 1.6F))
            {
                e.Graphics.DrawPolygon(pen, new PointF[] { new PointF(x, y - r), new PointF(x + r, y), new PointF(x, y + r), new PointF(x - r, y) });
                e.Graphics.DrawLine(pen, x - r / 2, y, x + r / 2, y);
                e.Graphics.DrawLine(pen, x, y - r / 2, x, y + r / 2);
            }
            e.Graphics.SmoothingMode = oldMode;
        }
        else if (e.ColumnIndex == 2)
        {
            int lineHeight = Font.Height + 2;
            int top = bounds.Top + (bounds.Height - lineHeight * 2) / 2;
            Rectangle title = new Rectangle(bounds.Left + 4 * scale, top, Math.Max(0, bounds.Width - 12 * scale), lineHeight);
            Rectangle description = new Rectangle(title.Left, top + lineHeight, title.Width, lineHeight);
            TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
            TextRenderer.DrawText(e.Graphics, item.Name ?? item.Id, titleFont ?? Font, title, UiTheme.Text, flags);
            TextRenderer.DrawText(e.Graphics, string.IsNullOrWhiteSpace(item.Description)
                ? (english ? "No description available" : "Không có mô tả") : item.Description, Font, description, UiTheme.MutedText, flags);
            if (Focused && CurrentRow != null && CurrentRow.Index == e.RowIndex && CurrentCell.ColumnIndex != 0)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -3, -6), UiTheme.Primary, background);
        }
        else if (e.ColumnIndex == 3)
        {
            Rectangle textBounds = Rectangle.Inflate(bounds, -6 * scale, 0);
            TextRenderer.DrawText(e.Graphics, Status(item), Font, textBounds, item.Achieved ? UiTheme.Success : UiTheme.Warning,
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        }
        e.Handled = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && titleFont != null)
        {
            titleFont.Dispose();
            titleFont = null;
        }
        base.Dispose(disposing);
    }
}

internal sealed class ThemedScrollBar : Control
{
    private int minimum;
    private int maximum;
    private int largeChange = 1;
    private int value;
    private bool dragging;
    private int dragOffset;
    private bool hovered;

    public ThemedScrollBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.ScrollBar;
        AccessibleName = "Achievement list scrollbar";
        TabStop = true;
    }

    public event EventHandler ValueChanged;

    public int Value
    {
        get { return value; }
        set { SetValue(value, true); }
    }

    public int LargeChange
    {
        get { return largeChange; }
        private set { largeChange = Math.Max(1, value); }
    }

    public void SetRange(int minimum, int maximum, int largeChange, int value)
    {
        this.minimum = Math.Max(0, minimum);
        this.maximum = Math.Max(this.minimum, maximum);
        LargeChange = largeChange;
        SetValue(value, false);
        Visible = this.maximum > this.minimum + LargeChange - 1;
        Invalidate();
    }

    public void RefreshTheme()
    {
        Invalidate();
    }

    private int EffectiveMaximum
    {
        get { return Math.Max(minimum, maximum - LargeChange + 1); }
    }

    private int TrackTop { get { return 4; } }
    private int TrackBottom { get { return Math.Max(TrackTop + 1, Height - 4); } }
    private int TrackHeight { get { return Math.Max(1, TrackBottom - TrackTop); } }

    private Rectangle ThumbBounds
    {
        get
        {
            int content = Math.Max(1, maximum - minimum + 1);
            int thumbHeight = Math.Max(28, (int)Math.Round(TrackHeight * (LargeChange / (double)(content + LargeChange))));
            thumbHeight = Math.Min(TrackHeight, thumbHeight);
            int travel = Math.Max(0, TrackHeight - thumbHeight);
            int range = Math.Max(1, EffectiveMaximum - minimum);
            int offset = (int)Math.Round(travel * ((value - minimum) / (double)range));
            return new Rectangle((Width - 7) / 2, TrackTop + offset, 7, thumbHeight);
        }
    }

    private void SetValue(int next, bool notify)
    {
        int clamped = Math.Max(minimum, Math.Min(EffectiveMaximum, next));
        if (value == clamped)
        {
            Invalidate();
            return;
        }
        value = clamped;
        Invalidate();
        if (notify && ValueChanged != null)
            ValueChanged(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(Parent == null ? UiTheme.Surface : Parent.BackColor);
        Rectangle track = new Rectangle((Width - 5) / 2, TrackTop, 5, TrackHeight);
        using (SolidBrush brush = new SolidBrush(UiTheme.SurfaceMuted))
            e.Graphics.FillRectangle(brush, track);
        Rectangle thumb = ThumbBounds;
        Color thumbColor = dragging ? UiTheme.Primary : hovered ? UiTheme.Cyan : UiTheme.MutedText;
        using (GraphicsPath path = UiTheme.RoundedRectangle(thumb, 3))
        using (SolidBrush brush = new SolidBrush(thumbColor))
            e.Graphics.FillPath(brush, path);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hovered = false;
        if (!dragging) Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !Visible || !Enabled)
            return;
        Rectangle thumb = ThumbBounds;
        if (thumb.Contains(e.Location))
        {
            dragging = true;
            dragOffset = e.Y - thumb.Top;
            Capture = true;
            Invalidate();
            return;
        }
        SetValue(value + (e.Y < thumb.Top ? -LargeChange : LargeChange), true);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!dragging)
            return;
        int travel = Math.Max(1, TrackHeight - ThumbBounds.Height);
        int position = Math.Max(TrackTop, Math.Min(TrackBottom - ThumbBounds.Height, e.Y - dragOffset));
        int range = Math.Max(1, EffectiveMaximum - minimum);
        SetValue(minimum + (int)Math.Round((position - TrackTop) * (range / (double)travel)), true);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            dragging = false;
            Capture = false;
            Invalidate();
        }
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Up)
        {
            SetValue(value - 1, true);
            e.Handled = true;
            return;
        }
        if (e.KeyCode == Keys.Down)
        {
            SetValue(value + 1, true);
            e.Handled = true;
            return;
        }
        if (e.KeyCode == Keys.PageUp)
        {
            SetValue(value - LargeChange, true);
            e.Handled = true;
            return;
        }
        if (e.KeyCode == Keys.PageDown)
        {
            SetValue(value + LargeChange, true);
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        return key == Keys.Up || key == Keys.Down || key == Keys.PageUp || key == Keys.PageDown || base.IsInputKey(keyData);
    }
}
}
