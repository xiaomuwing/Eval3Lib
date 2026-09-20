using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Eval3.WinForms;

/// <summary>批量验算行。</summary>
public sealed class BatchRow : INotifyPropertyChanged
{
    public int Index { get; set; }

    public string Formula { get; set; } = string.Empty;

    public string Expected { get; set; } = string.Empty;

    public string Actual { get; set; } = string.Empty;

    public string Status { get; set; } = "待验算";

    public string ElapsedText { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>通知界面刷新该行。</summary>
    public void RaiseAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}

/// <summary>批量验算页面：逐行公式 + 期望值，支持并行验算与结果导出。</summary>
internal sealed class BatchVerifyView : ViewBase
{
    private readonly BindingList<BatchRow> _rows = [];
    private readonly DataGridView _grid = UiFactory.Grid();
    private readonly FlowLayoutPanel _toolbar = UiFactory.Toolbar();
    private readonly Label _summary = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Right, Width = 260, Minimum = 0, Maximum = 100 };
    private readonly ComboBox _parallelism = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90, Margin = new Padding(0, 5, 12, 3) };
    private readonly Button _startButton;
    private readonly Button _stopButton;
    private CancellationTokenSource? _cancellation;
    private bool _running;
    private bool _autoRan;

    public BatchVerifyView(EvaluationSession session)
        : base(session)
    {
        _startButton = UiFactory.Button("开始验算", 100, async (_, _) => await RunAsync());
        _stopButton = UiFactory.Button("停止", 72, (_, _) => _cancellation?.Cancel());
        _stopButton.Enabled = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

        _toolbar.Controls.AddRange(
        [
            UiFactory.Button("添加行", 80, (_, _) => AddRow(string.Empty, string.Empty)),
            UiFactory.Button("删除选中行", 100, OnDeleteRows),
            UiFactory.Button("清空", 72, OnClearRows),
            UiFactory.Button("导入剪贴板", 100, OnImportClipboard),
            _startButton,
            _stopButton,
            UiFactory.Button("导出 CSV", 92, OnExportCsv),
            UiFactory.Caption("并行度："),
            _parallelism,
        ]);

        _parallelism.Items.AddRange(["自动", "1", "2", "4", "8", "16"]);
        _parallelism.SelectedIndex = 0;

        var summaryPanel = new Panel { Dock = DockStyle.Fill };
        summaryPanel.Controls.Add(_summary);
        summaryPanel.Controls.Add(_progress);

        layout.Controls.Add(_toolbar, 0, 0);
        layout.Controls.Add(_grid, 0, 1);
        layout.Controls.Add(summaryPanel, 0, 2);

        Controls.Add(layout);
        Dock = DockStyle.Fill;
        Padding = new Padding(4);

        ConfigureGrid();
        SeedSampleRows();
        UpdateSummary(0, 0, 0, 0, TimeSpan.Zero);
    }

    private void ConfigureGrid()
    {
        _grid.Columns.AddRange(
        [
            UiFactory.Column("序号", nameof(BatchRow.Index), 56, readOnly: true, DataGridViewContentAlignment.MiddleCenter),
            UiFactory.Column("公式", nameof(BatchRow.Formula), 250, readOnly: false),
            UiFactory.Column("期望值", nameof(BatchRow.Expected), 90, readOnly: false),
            UiFactory.Column("实际值", nameof(BatchRow.Actual), 100, readOnly: true),
            UiFactory.Column("结果", nameof(BatchRow.Status), 64, readOnly: true, DataGridViewContentAlignment.MiddleCenter),
            UiFactory.Column("耗时(µs)", nameof(BatchRow.ElapsedText), 86, readOnly: true, DataGridViewContentAlignment.MiddleRight),
            UiFactory.FillColumn("备注", nameof(BatchRow.Note)),
        ]);

        _grid.DataSource = _rows;
        _grid.CellFormatting += OnCellFormatting;
        _grid.DataError += (_, e) => e.ThrowException = false;
    }

    /// <summary>首次进入本页时自动验算一遍示例数据。</summary>
    public override async void OnActivated()
    {
        if (_autoRan || _running || _rows.Count == 0)
        {
            return;
        }

        _autoRan = true;
        await RunAsync();
    }

    private void SeedSampleRows()
    {
        AddRow("Round(单价 * 数量 * (1 + 税率), 2)", "113");
        AddRow("单价 * 数量", "100");
        AddRow("单价 * 数量 + 1", "100");
        AddRow("if(数量 > 5, \"批量\", \"零散\")", "批量");
        AddRow("单价 * 数量 * 1.13 - 税额", string.Empty);
    }

    private void AddRow(string formula, string expected)
    {
        _rows.Add(new BatchRow { Index = _rows.Count + 1, Formula = formula, Expected = expected });
        Renumber();
    }

    private void Renumber()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            _rows[i].Index = i + 1;
        }

        _rows.ResetBindings();
    }

    private void OnDeleteRows(object? sender, EventArgs e)
    {
        foreach (DataGridViewRow row in _grid.SelectedRows.Cast<DataGridViewRow>().OrderByDescending(r => r.Index).ToArray())
        {
            if (row.DataBoundItem is BatchRow item)
            {
                _rows.Remove(item);
            }
        }

        Renumber();
    }

    private void OnClearRows(object? sender, EventArgs e)
    {
        _rows.Clear();
        Renumber();
        _grid.Refresh();
    }

    private void OnImportClipboard(object? sender, EventArgs e)
    {
        if (!Clipboard.ContainsText())
        {
            return;
        }

        string[] lines = Clipboard.GetText().Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string formula = line;
            string expected = string.Empty;
            int separator = line.Contains('\t') ? line.IndexOf('\t') : line.IndexOf('|');
            if (separator > 0)
            {
                formula = line[..separator].Trim();
                expected = line[(separator + 1)..].Trim();
            }

            AddRow(formula, expected);
        }
    }

    private void OnExportCsv(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            FileName = $"验算结果_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("序号,公式,期望值,实际值,结果,耗时(µs),备注");
        foreach (BatchRow row in _rows)
        {
            builder
                .Append(row.Index).Append(',')
                .Append(Escape(row.Formula)).Append(',')
                .Append(Escape(row.Expected)).Append(',')
                .Append(Escape(row.Actual)).Append(',')
                .Append(row.Status).Append(',')
                .Append(row.ElapsedText).Append(',')
                .Append(Escape(row.Note))
                .AppendLine();
        }

        File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        MessageBox.Show(this, "已导出：" + dialog.FileName, "批量验算", MessageBoxButtons.OK, MessageBoxIcon.Information);

        static string Escape(string value) => value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal)
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : value;
    }

    private async Task RunAsync()
    {
        if (_running)
        {
            return;
        }

        List<BatchRow> targets = [.. _rows.Where(row => !string.IsNullOrWhiteSpace(row.Formula))];
        if (targets.Count == 0)
        {
            MessageBox.Show(this, "请先填写至少一条公式。", "批量验算", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _running = true;
        _startButton.Enabled = false;
        _stopButton.Enabled = true;
        _progress.Value = 0;
        _cancellation = new CancellationTokenSource();
        CancellationToken token = _cancellation.Token;
        int parallelism = SelectedParallelism();
        int completed = 0;

        var progress = new Progress<int>(done =>
        {
            _progress.Value = Math.Min(100, done * 100 / targets.Count);
            _summary.Text = $"验算中… {done}/{targets.Count}（并行度 {parallelism}）";
        });

        var stopwatch = Stopwatch.StartNew();
        EvaluationOutcome?[] results;
        try
        {
            results = await Task.Run(
                () =>
                {
                    var buffer = new EvaluationOutcome?[targets.Count];
                    try
                    {
                        Parallel.For(
                            0,
                            targets.Count,
                            new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = token },
                            i =>
                            {
                                buffer[i] = Session.Evaluate(targets[i].Formula);
                                int done = Interlocked.Increment(ref completed);
                                if (done % 25 == 0 || done == targets.Count)
                                {
                                    ((IProgress<int>)progress).Report(done);
                                }
                            });
                    }
                    catch (OperationCanceledException)
                    {
                        // 保留已完成的结果
                    }

                    return buffer;
                },
                token);
        }
        catch (OperationCanceledException)
        {
            results = new EvaluationOutcome?[targets.Count];
        }

        stopwatch.Stop();

        int passed = 0;
        int mismatch = 0;
        int errors = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            BatchRow row = targets[i];
            EvaluationOutcome? outcome = results[i];
            if (outcome is null)
            {
                row.Status = "已取消";
                row.RaiseAll();
                continue;
            }

            double microseconds = (outcome.ParseMilliseconds + outcome.EvaluateMilliseconds) * 1000d;
            row.ElapsedText = microseconds.ToString(microseconds < 100d ? "F1" : "F0", CultureInfo.InvariantCulture);
            if (!outcome.Success)
            {
                row.Status = "错误";
                row.Actual = string.Empty;
                row.Note = outcome.Error?.Message ?? "未知错误";
                errors++;
            }
            else
            {
                row.Actual = outcome.Result;
                row.Note = string.Empty;
                if (string.IsNullOrWhiteSpace(row.Expected))
                {
                    row.Status = "已计算";
                }
                else if (IsMatch(row.Expected, outcome))
                {
                    row.Status = "通过";
                    passed++;
                }
                else
                {
                    row.Status = "不一致";
                    row.Note = $"期望 {row.Expected}，实际 {row.Actual}";
                    mismatch++;
                }
            }

            row.RaiseAll();
        }

        _running = false;
        _startButton.Enabled = true;
        _stopButton.Enabled = false;
        _cancellation?.Dispose();
        _cancellation = null;
        _progress.Value = 100;
        UpdateSummary(targets.Count, passed, mismatch, errors, stopwatch.Elapsed);
    }

    private void UpdateSummary(int total, int passed, int mismatch, int errors, TimeSpan elapsed)
    {
        _summary.Text = total == 0
            ? "每行一条公式；可用 Tab 或 | 分隔期望值。"
            : $"共 {total} 条：通过 {passed} · 不一致 {mismatch} · 错误 {errors} · 总耗时 {elapsed.TotalMilliseconds:F1} ms" +
              (elapsed.TotalMilliseconds > 0 ? $"（{total / Math.Max(elapsed.TotalSeconds, 0.000001):N0} 条/秒）" : string.Empty);
    }

    private int SelectedParallelism() => _parallelism.SelectedIndex switch
    {
        0 => Environment.ProcessorCount,
        int index => _parallelism.Items[index] is string text
            ? int.Parse(text, CultureInfo.InvariantCulture)
            : Environment.ProcessorCount,
    };

    private static bool IsMatch(string expected, EvaluationOutcome outcome)
    {
        string actual = outcome.Result;
        if (double.TryParse(expected, NumberStyles.Any, CultureInfo.InvariantCulture, out double expectedNumber)
            && double.TryParse(actual, NumberStyles.Any, CultureInfo.InvariantCulture, out double actualNumber))
        {
            if (double.IsNaN(expectedNumber))
            {
                return double.IsNaN(actualNumber);
            }

            return Math.Abs(expectedNumber - actualNumber) <= Math.Max(1e-9, Math.Abs(expectedNumber) * 1e-9);
        }

        return string.Equals(expected.Trim(), actual.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid.Columns[e.ColumnIndex].Name != nameof(BatchRow.Status) || e.Value is not string status)
        {
            return;
        }

        (Color color, Font font) = status switch
        {
            "通过" => (Theme.OkColor, Theme.UiFontBold),
            "不一致" => (Theme.WarnColor, Theme.UiFontBold),
            "错误" => (Theme.ErrorColor, Theme.UiFontBold),
            _ => (Theme.GrayText, Theme.UiFont),
        };

        e.CellStyle.ForeColor = color;
        e.CellStyle.Font = font;
    }
}
