using System.ComponentModel;

namespace Eval3.WinForms;

/// <summary>函数清单行。</summary>
public sealed class FunctionRow
{
    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>双击插入到公式编辑框的片段。</summary>
    public string InsertText { get; set; } = string.Empty;
}

/// <summary>函数库页面：勾选启用的函数库，浏览可用函数并插入公式。</summary>
internal sealed class LibraryView : ViewBase
{
    private readonly CheckedListBox _libraries = new()
    {
        Dock = DockStyle.Fill,
        CheckOnClick = true,
        IntegralHeight = false,
        Font = Theme.UiFont,
        BorderStyle = BorderStyle.FixedSingle,
    };

    private readonly TextBox _search = new()
    {
        Dock = DockStyle.Fill,
        PlaceholderText = "搜索函数名 / 签名 / 说明…",
        Font = Theme.UiFont,
    };

    private readonly DataGridView _grid = UiFactory.Grid();
    private readonly BindingList<FunctionRow> _rows = [];
    private readonly Label _summary = new() { Dock = DockStyle.Fill, ForeColor = Theme.GrayText, TextAlign = ContentAlignment.MiddleLeft };
    private readonly List<LibraryEntry> _entries = [];
    private bool _suppressCheckEvents;

    public LibraryView(EvaluationSession session)
        : base(session)
    {
        var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104F));
        leftLayout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "启用的函数库",
            Font = Theme.TitleFont,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);
        leftLayout.Controls.Add(_libraries, 0, 1);
        leftLayout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Theme.GrayText,
            Text = "注册方式：\r\n· 静态库传入 typeof(库类型)\r\n· 实例库直接传入对象\r\n· 实现 IVariableBag 的对象可提供公式变量",
        }, 0, 2);

        var rightLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        rightLayout.Controls.Add(_search, 0, 0);
        rightLayout.Controls.Add(_grid, 0, 1);
        rightLayout.Controls.Add(_summary, 0, 2);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
        };
        split.Panel1.Controls.Add(leftLayout);
        split.Panel2.Controls.Add(rightLayout);
        SetSplitterDistance(split, 300, panel1MinSize: 200, panel2MinSize: 260);

        Controls.Add(split);
        Dock = DockStyle.Fill;
        Padding = new Padding(4);

        _grid.Columns.AddRange(
        [
            UiFactory.Column("函数", nameof(FunctionRow.Name), 110, readOnly: true),
            UiFactory.Column("签名", nameof(FunctionRow.Signature), 250, readOnly: true),
            UiFactory.FillColumn("说明", nameof(FunctionRow.Description)),
        ]);
        _grid.DataSource = _rows;
        _grid.DataError += (_, e) => e.ThrowException = false;
        _grid.CellDoubleClick += OnFunctionDoubleClick;
        _search.TextChanged += (_, _) => RefreshFunctions();

        LoadLibraries();
    }

    /// <summary>请求把函数片段插入到验算页。</summary>
    public event EventHandler<string>? FunctionInsertRequested;

    public override void OnActivated()
    {
        LoadLibraries();
        RefreshFunctions();
    }

    private void LoadLibraries()
    {
        _suppressCheckEvents = true;
        _libraries.Items.Clear();
        _entries.Clear();
        foreach (LibraryEntry entry in Session.Libraries)
        {
            _entries.Add(entry);
            _libraries.Items.Add(entry, entry.Enabled);
        }

        _suppressCheckEvents = false;
        _libraries.ItemCheck += OnLibraryItemCheck;
        RefreshFunctions();
    }

    private void OnLibraryItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_suppressCheckEvents || e.Index < 0 || e.Index >= _entries.Count)
        {
            return;
        }

        LibraryEntry entry = _entries[e.Index];
        bool enabled = e.NewValue == CheckState.Checked;
        BeginInvoke((Action)(() =>
        {
            Session.SetLibraryEnabled(entry.Name, enabled);
            RefreshFunctions();
        }));
    }

    private void RefreshFunctions()
    {
        string keyword = _search.Text.Trim();
        _rows.RaiseListChangedEvents = false;
        _rows.Clear();
        int total = 0;
        foreach (LibraryEntry entry in _entries)
        {
            if (!entry.Enabled)
            {
                continue;
            }

            foreach (FunctionDescriptor descriptor in FunctionCatalog.Describe(entry.Target))
            {
                total++;
                if (keyword.Length > 0
                    && !descriptor.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    && !descriptor.Signature.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    && !descriptor.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _rows.Add(new FunctionRow
                {
                    Name = descriptor.Name,
                    Category = entry.Name,
                    Signature = descriptor.Signature,
                    Description = descriptor.Description,
                    InsertText = descriptor.MemberType == System.Reflection.MemberTypes.Method
                        ? descriptor.Name + "()"
                        : descriptor.Name,
                });
            }
        }

        _rows.RaiseListChangedEvents = true;
        _rows.ResetBindings();
        _summary.Text = keyword.Length == 0
            ? $"共 {total} 个可用函数 / 属性，双击可插入到验算页。"
            : $"匹配 {_rows.Count} / {total} 个成员，双击可插入到验算页。";
    }

    private void OnFunctionDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].DataBoundItem is not FunctionRow row)
        {
            return;
        }

        FunctionInsertRequested?.Invoke(this, row.InsertText);
    }
}
