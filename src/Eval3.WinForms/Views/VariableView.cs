using System.ComponentModel;
using System.Globalization;

namespace Eval3.WinForms;

/// <summary>变量表格行。</summary>
public sealed class VariableRow : INotifyPropertyChanged
{
    public string Name { get; set; } = string.Empty;

    public string TypeName { get; set; } = "数值";

    public string Value { get; set; } = string.Empty;

    public string EvalTypeText { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>对应变量实例（用于原地改值）。</summary>
    public EvalVariable? Variable { get; set; }

    /// <summary>重命名检测用的原名。</summary>
    public string OriginalName { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RaiseAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}

/// <summary>变量管理页面。</summary>
internal sealed class VariableView : ViewBase
{
    private static readonly string[] TypeNames = ["数值", "文本", "日期", "布尔"];

    private readonly BindingList<VariableRow> _rows = [];
    private readonly DataGridView _grid = UiFactory.Grid();

    public VariableView(EvaluationSession session)
        : base(session)
    {
        var toolbar = UiFactory.Toolbar();
        toolbar.Controls.AddRange(
        [
            UiFactory.Button("新增变量", 92, (_, _) => AddVariable()),
            UiFactory.Button("删除选中", 92, OnDeleteSelected),
            UiFactory.Button("全部清空", 92, OnClearAll),
            UiFactory.Button("恢复示例变量", 116, (_, _) =>
            {
                Session.ResetSampleVariables();
                Reload();
                RaiseChanged();
            }),
        ]);

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Theme.GrayText,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "提示：变量名支持中文；修改类型会重建计算组件并清空公式缓存，公式中引用的变量名大小写由“单条验算”页的大小写选项决定。",
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(_grid, 0, 1);
        layout.Controls.Add(hint, 0, 2);

        Controls.Add(layout);
        Dock = DockStyle.Fill;
        Padding = new Padding(4);

        var typeColumn = new DataGridViewComboBoxColumn
        {
            HeaderText = "类型",
            DataPropertyName = nameof(VariableRow.TypeName),
            Name = nameof(VariableRow.TypeName),
            Width = 90,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
        };
        typeColumn.Items.AddRange(TypeNames);

        _grid.Columns.AddRange(
        [
            UiFactory.Column("变量名", nameof(VariableRow.Name), 130),
            typeColumn,
            UiFactory.Column("值", nameof(VariableRow.Value), 170),
            UiFactory.Column("逻辑类型", nameof(VariableRow.EvalTypeText), 100, readOnly: true, DataGridViewContentAlignment.MiddleCenter),
            UiFactory.FillColumn("说明", nameof(VariableRow.Description), readOnly: false),
        ]);

        _grid.DataSource = _rows;
        _grid.DataError += (_, e) => e.ThrowException = false;
        _grid.CellEndEdit += OnCellEndEdit;
        _grid.EditingControlShowing += OnEditingControlShowing;

        Reload();
    }

    /// <summary>变量发生变化。</summary>
    public event EventHandler? VariablesChanged;

    public override void OnActivated() => Reload();

    private void Reload()
    {
        _rows.RaiseListChangedEvents = false;
        _rows.Clear();
        foreach (EvalVariable variable in Session.Variables.OrderBy(static v => v.Name, StringComparer.CurrentCulture))
        {
            _rows.Add(new VariableRow
            {
                Name = variable.Name,
                OriginalName = variable.Name,
                TypeName = TypeNameOf(variable),
                Value = FormatValue(variable),
                EvalTypeText = variable.EvalType.ToString(),
                Description = variable.Description,
                Variable = variable,
            });
        }

        _rows.RaiseListChangedEvents = true;
        _rows.ResetBindings();
    }

    private static string TypeNameOf(EvalVariable variable) => variable.EvalType switch
    {
        EvalType.Number => "数值",
        EvalType.Boolean => "布尔",
        EvalType.Date => "日期",
        EvalType.String => "文本",
        _ => "文本",
    };

    private static string FormatValue(EvalVariable variable) => variable.Value switch
    {
        null => string.Empty,
        double number => number.ToString("G", CultureInfo.InvariantCulture),
        DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).TrimEnd('0', ':', ' '),
        bool flag => flag ? "True" : "False",
        string text => text,
        object value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    private void AddVariable()
    {
        int index = 1;
        string name;
        do
        {
            name = $"变量{index++}";
        }
        while (Session.Variables.Find(name) is not null);

        Session.Variables.Set(name, 0d, string.Empty);
        Session.NotifyVariablesChanged();
        RaiseChanged();
        Reload();

        int rowIndex = _rows.ToList().FindIndex(row => row.Name == name);
        if (rowIndex >= 0)
        {
            _grid.CurrentCell = _grid.Rows[rowIndex].Cells[nameof(VariableRow.Name)];
            _grid.BeginEdit(selectAll: true);
        }
    }

    private void OnDeleteSelected(object? sender, EventArgs e)
    {
        string[] names = [.. _grid.SelectedRows.Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem as VariableRow)
            .Where(static row => row is not null)
            .Select(static row => row!.OriginalName)];

        foreach (string name in names)
        {
            Session.Variables.Remove(name);
        }

        if (names.Length > 0)
        {
            Session.NotifyVariablesChanged();
            RaiseChanged();
            Reload();
        }
    }

    private void OnClearAll(object? sender, EventArgs e)
    {
        Session.Variables.Clear();
        Session.NotifyVariablesChanged();
        RaiseChanged();
        Reload();
    }

    private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count)
        {
            return;
        }

        if (_grid.Rows[e.RowIndex].DataBoundItem is VariableRow row)
        {
            ApplyRow(row);
        }
    }

    private void OnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (e.Control is ComboBox combo)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
        }
    }

    private void ApplyRow(VariableRow row)
    {
        string name = row.Name.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show(this, "变量名不能为空。", "变量", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Reload();
            return;
        }

        if (Session.Variables.Find(name) is { } conflict && conflict != row.Variable)
        {
            MessageBox.Show(this, $"变量名 {name} 已存在。", "变量", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Reload();
            return;
        }

        if (!TryParseValue(row.TypeName, row.Value, out object? value, out string? error))
        {
            MessageBox.Show(this, error!, "变量", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Reload();
            return;
        }

        EvalVariable? existing = row.Variable is not null && row.OriginalName == row.Variable.Name
            ? row.Variable
            : Session.Variables.Find(row.OriginalName);

        if (existing is null || !string.Equals(existing.Name, name, StringComparison.Ordinal))
        {
            if (existing is not null)
            {
                Session.Variables.Remove(existing.Name);
            }

            row.Variable = Session.Variables.Set(name, value, row.Description);
        }
        else
        {
            existing.Value = value;
            existing.Description = row.Description;
            row.Variable = existing;
        }

        Session.NotifyVariablesChanged();
        RaiseChanged();
        Reload();
    }

    private static bool TryParseValue(string typeName, string text, out object? value, out string? error)
    {
        error = null;
        switch (typeName)
        {
            case "数值":
                if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out double number)
                    || double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
                {
                    value = number;
                    return true;
                }

                value = null;
                error = $"“{text}” 不是有效数值。";
                return false;

            case "日期":
                if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime date)
                    || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    value = date;
                    return true;
                }

                value = null;
                error = $"“{text}” 不是有效日期（示例：2024-03-01）。";
                return false;

            case "布尔":
                if (bool.TryParse(text, out bool flag)
                    || text.Trim() is "1" or "是" or "真" or "Y" or "y")
                {
                    value = text.Trim() switch
                    {
                        "1" => true,
                        "是" => true,
                        "真" => true,
                        "Y" => true,
                        "y" => true,
                        _ => flag,
                    };
                    return true;
                }

                if (text.Trim() is "0" or "否" or "假" or "N" or "n")
                {
                    value = false;
                    return true;
                }

                value = null;
                error = $"“{text}” 不是有效布尔值（True/False）。";
                return false;

            default:
                value = text;
                return true;
        }
    }

    private void RaiseChanged() => VariablesChanged?.Invoke(this, EventArgs.Empty);
}
