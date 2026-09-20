namespace Eval3.WinForms;

/// <summary>界面控件工厂，统一表格 / 工具栏 / 按钮的样式。</summary>
internal static class UiFactory
{
    public static Button Button(string text, int width = 92, EventHandler? click = null)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(width, 30),
            Margin = new Padding(0, 3, 6, 3),
            UseVisualStyleBackColor = true,
        };

        if (click is not null)
        {
            button.Click += click;
        }

        return button;
    }

    public static Label Caption(string text, int topMargin = 8) => new()
    {
        AutoSize = true,
        Text = text,
        Margin = new Padding(3, topMargin, 2, 3),
    };

    public static FlowLayoutPanel Toolbar() => new()
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(4, 2, 4, 2),
        WrapContents = false,
        AutoScroll = true,
    };

    public static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            EditMode = DataGridViewEditMode.EditOnEnter,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowTemplate = { Height = 24 },
        };

        // DataGridView 的列宽不会随 DPI 自动缩放，这里按逻辑宽度手工换算
        grid.HandleCreated += (_, _) => ScaleColumns(grid);
        grid.DpiChangedAfterParent += (_, _) => ScaleColumns(grid);
        return grid;
    }

    public static DataGridViewTextBoxColumn Column(
        string header,
        string property,
        int width,
        bool readOnly = false,
        DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleLeft)
    {
        return new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            Name = property,
            Width = width,
            ReadOnly = readOnly,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            Tag = width,
            DefaultCellStyle = { Alignment = align },
        };
    }

    /// <summary>末列自适应剩余宽度。</summary>
    public static DataGridViewTextBoxColumn FillColumn(string header, string property, bool readOnly = true)
    {
        DataGridViewTextBoxColumn column = Column(header, property, 160, readOnly);
        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        return column;
    }

    /// <summary>把列宽从逻辑像素换算为当前 DPI 的物理像素。</summary>
    public static void ScaleColumns(DataGridView grid)
    {
        double scale = grid.DeviceDpi / 96d;
        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (column.Tag is int logical && column.AutoSizeMode == DataGridViewAutoSizeColumnMode.None)
            {
                column.Width = Math.Max(24, (int)Math.Round(logical * scale));
            }
        }
    }
}
