using System.ComponentModel;
using System.Data;
using System.Globalization;

namespace Eval3.WinForms;

/// <summary>性能表格行。</summary>
public sealed class BenchmarkRow
{
    public string Name { get; set; } = string.Empty;

    public string Iterations { get; set; } = string.Empty;

    public string Time { get; set; } = string.Empty;

    public string PerSecond { get; set; } = string.Empty;

    public string Bytes { get; set; } = string.Empty;

    public string Relative { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}

/// <summary>性能测试页面：对比解析 / 缓存求值 / 数值快路径 / 旧方案的耗时与分配。</summary>
internal sealed class BenchmarkView : ViewBase
{
    private readonly BindingList<BenchmarkRow> _rows = [];
    private readonly DataGridView _grid = UiFactory.Grid();
    private readonly TextBox _formula = new() { Width = 420, Font = Theme.CodeFont, Margin = new Padding(0, 5, 12, 3) };
    private readonly NumericUpDown _iterations = new()
    {
        Minimum = 10_000,
        Maximum = 5_000_000,
        Increment = 50_000,
        Value = 200_000,
        Width = 110,
        ThousandsSeparator = true,
        Margin = new Padding(0, 5, 12, 3),
    };

    private readonly CheckBox _compareDataTable = new()
    {
        Text = "对比 DataTable.Compute（旧方案）",
        AutoSize = true,
        Checked = true,
        Margin = new Padding(0, 8, 12, 3),
    };

    private readonly Button _runButton;
    private readonly Button _stopButton;
    private readonly Label _status = new() { Dock = DockStyle.Fill, ForeColor = Theme.GrayText, TextAlign = ContentAlignment.MiddleLeft };
    private CancellationTokenSource? _cancellation;
    private bool _running;
    private bool _autoRan;

    public BenchmarkView(EvaluationSession session)
        : base(session)
    {
        _runButton = UiFactory.Button("开始测试", 100, async (_, _) => await RunAsync());
        _stopButton = UiFactory.Button("停止", 72, (_, _) => _cancellation?.Cancel());
        _stopButton.Enabled = false;

        var toolbar = UiFactory.Toolbar();
        toolbar.Controls.AddRange(
        [
            UiFactory.Caption("公式："),
            _formula,
            UiFactory.Caption("迭代次数："),
            _iterations,
            _compareDataTable,
            _runButton,
            _stopButton,
        ]);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(_grid, 0, 1);
        layout.Controls.Add(_status, 0, 2);

        Controls.Add(layout);
        Dock = DockStyle.Fill;
        Padding = new Padding(4);

        _grid.Columns.AddRange(
        [
            UiFactory.Column("测试项", nameof(BenchmarkRow.Name), 190, readOnly: true),
            UiFactory.Column("迭代", nameof(BenchmarkRow.Iterations), 84, readOnly: true, DataGridViewContentAlignment.MiddleRight),
            UiFactory.Column("每次耗时", nameof(BenchmarkRow.Time), 96, readOnly: true, DataGridViewContentAlignment.MiddleRight),
            UiFactory.Column("每秒次数", nameof(BenchmarkRow.PerSecond), 116, readOnly: true, DataGridViewContentAlignment.MiddleRight),
            UiFactory.Column("每次分配", nameof(BenchmarkRow.Bytes), 96, readOnly: true, DataGridViewContentAlignment.MiddleRight),
            UiFactory.Column("相对基准", nameof(BenchmarkRow.Relative), 84, readOnly: true, DataGridViewContentAlignment.MiddleRight),
            UiFactory.FillColumn("备注", nameof(BenchmarkRow.Note)),
        ]);
        _grid.DataSource = _rows;
        _grid.DataError += (_, e) => e.ThrowException = false;

        _formula.Text = Session.SampleFormula;
        _status.Text = "选择公式与迭代次数后开始测试；测量包含预热，数值公式会自动启用免装箱快路径。";
    }

    public override void OnActivated()
    {
        if (_rows.Count == 0)
        {
            _formula.Text = Session.SampleFormula;
        }

        if (!_autoRan && !_running)
        {
            _autoRan = true;
            _ = RunAsync();
        }
    }

    private async Task RunAsync()
    {
        if (_running)
        {
            return;
        }

        string formula = _formula.Text;
        int iterations = (int)_iterations.Value;
        bool compareDataTable = _compareDataTable.Checked;

        _running = true;
        _runButton.Enabled = false;
        _stopButton.Enabled = true;
        _rows.Clear();
        _cancellation = new CancellationTokenSource();
        CancellationToken token = _cancellation.Token;

        var progress = new Progress<string>(text => _status.Text = text);

        try
        {
            List<BenchmarkMeasurement> measurements = await Task.Run(
                () => Measure(formula, iterations, compareDataTable, (IProgress<string>)progress, token),
                token);
            FillRows(measurements);
        }
        catch (OperationCanceledException)
        {
            _status.Text = "已停止。";
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _status.Text = "测试失败：" + ex.Message;
        }
        finally
        {
            _running = false;
            _runButton.Enabled = true;
            _stopButton.Enabled = false;
            _cancellation?.Dispose();
            _cancellation = null;
        }
    }

    private List<BenchmarkMeasurement> Measure(
        string formula,
        int iterations,
        bool compareDataTable,
        IProgress<string> progress,
        CancellationToken token)
    {
        var results = new List<BenchmarkMeasurement>(6);
        int slowIterations = Math.Max(1_000, iterations / 10);

        Session.Evaluator.Parse(formula).Evaluate();

        progress.Report("测量：解析 + 求值（未缓存）…");
        token.ThrowIfCancellationRequested();
        results.Add(BenchmarkRunner.Measure(
            "解析 + 求值（未缓存）",
            slowIterations,
            () => Session.Evaluator.Parse(formula).Evaluate()));

        CompiledFormula compiled = Session.Evaluator.Compile(formula);
        progress.Report("测量：缓存公式求值（装箱）…");
        token.ThrowIfCancellationRequested();
        results.Add(BenchmarkRunner.Measure("缓存公式求值（装箱）", iterations, () => compiled.Evaluate()));

        if (compiled.HasNumericFastPath)
        {
            progress.Report("测量：数值快路径（免装箱）…");
            token.ThrowIfCancellationRequested();
            results.Add(BenchmarkRunner.Measure("数值快路径（免装箱）", iterations, () => compiled.EvaluateNumber()));
        }
        else
        {
            results.Add(new BenchmarkMeasurement("数值快路径（免装箱）", 0, 0d, 0d, 0d, "该公式含函数 / 变量，未生成纯数值委托"));
        }

        if (compareDataTable)
        {
            progress.Report("测量：DataTable.Compute（旧方案）…");
            token.ThrowIfCancellationRequested();
            try
            {
                var table = new DataTable();
                _ = table.Compute(formula, string.Empty);
                results.Add(BenchmarkRunner.Measure(
                    "DataTable.Compute（旧方案）",
                    slowIterations,
                    () => table.Compute(formula, string.Empty)));
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                results.Add(new BenchmarkMeasurement("DataTable.Compute（旧方案）", 0, 0d, 0d, 0d, "不支持该公式：" + ex.Message));
            }
        }

        progress.Report("测量：并行吞吐…");
        token.ThrowIfCancellationRequested();
        results.Add(BenchmarkRunner.MeasureParallel("并行求值吞吐", iterations, () => compiled.EvaluateNumber()));
        return results;
    }

    private void FillRows(List<BenchmarkMeasurement> measurements)
    {
        BenchmarkMeasurement? baseline = measurements
            .Where(static m => m.Iterations > 0 && m.Name != "并行求值吞吐")
            .OrderBy(static m => m.NanosecondsPerOperation)
            .FirstOrDefault();

        _rows.RaiseListChangedEvents = false;
        _rows.Clear();
        foreach (BenchmarkMeasurement measurement in measurements)
        {
            bool measured = measurement.Iterations > 0;
            _rows.Add(new BenchmarkRow
            {
                Name = measurement.Name,
                Iterations = measured ? measurement.Iterations.ToString("N0", CultureInfo.InvariantCulture) : "-",
                Time = measured ? FormatTime(measurement.NanosecondsPerOperation) : "-",
                PerSecond = measured ? measurement.OperationsPerSecond.ToString("N0", CultureInfo.InvariantCulture) : "-",
                Bytes = measured ? measurement.BytesPerOperation.ToString("N1", CultureInfo.InvariantCulture) + " B" : "-",
                Relative = measured && baseline is not null
                    ? (measurement.NanosecondsPerOperation / baseline.NanosecondsPerOperation).ToString("F1", CultureInfo.InvariantCulture) + "×"
                    : "-",
                Note = measurement.Note,
            });
        }

        _rows.RaiseListChangedEvents = true;
        _rows.ResetBindings();

        BenchmarkMeasurement? dataTable = measurements.FirstOrDefault(static m => m.Name.StartsWith("DataTable", StringComparison.Ordinal));
        BenchmarkMeasurement? fast = measurements.FirstOrDefault(static m => m.Name.StartsWith("数值快路径", StringComparison.Ordinal) && m.Iterations > 0);
        BenchmarkMeasurement? cached = measurements.FirstOrDefault(static m => m.Name.StartsWith("缓存公式求值", StringComparison.Ordinal) && m.Iterations > 0);
        BenchmarkMeasurement? effective = fast ?? cached;

        if (effective is not null && dataTable is { Iterations: > 0 })
        {
            double ratio = dataTable.NanosecondsPerOperation / effective.NanosecondsPerOperation;
            _status.Text = $"完成：{effective.Name} 比 DataTable.Compute 快 {ratio:F1} 倍（{FormatTime(effective.NanosecondsPerOperation)} vs {FormatTime(dataTable.NanosecondsPerOperation)}）。";
        }
        else if (effective is not null)
        {
            _status.Text = $"完成：{effective.Name} 每次 {FormatTime(effective.NanosecondsPerOperation)}，每秒 {effective.OperationsPerSecond:N0} 次。";
        }
        else
        {
            _status.Text = "测试完成。";
        }
    }

    private static string FormatTime(double nanoseconds) => nanoseconds switch
    {
        < 1_000d => $"{nanoseconds:F1} ns",
        < 1_000_000d => $"{nanoseconds / 1_000d:F2} µs",
        _ => $"{nanoseconds / 1_000_000d:F2} ms",
    };
}
