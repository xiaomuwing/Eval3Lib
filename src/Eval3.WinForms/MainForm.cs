namespace Eval3.WinForms;

/// <summary>验算主窗口：单条验算、批量验算、变量、函数库、性能测试。</summary>
public partial class MainForm : Form
{
    private readonly EvaluationSession _session = new();
    private readonly VerifyView _verifyView;
    private readonly BatchVerifyView _batchView;
    private readonly VariableView _variableView;
    private readonly LibraryView _libraryView;
    private readonly BenchmarkView _benchmarkView;

    public MainForm()
    {
        InitializeComponent();
        _session.WarmUp();

        _verifyView = new VerifyView(_session) { Dock = DockStyle.Fill };
        _batchView = new BatchVerifyView(_session) { Dock = DockStyle.Fill };
        _variableView = new VariableView(_session) { Dock = DockStyle.Fill };
        _libraryView = new LibraryView(_session) { Dock = DockStyle.Fill };
        _benchmarkView = new BenchmarkView(_session) { Dock = DockStyle.Fill };

        tabVerify.Controls.Add(_verifyView);
        tabBatch.Controls.Add(_batchView);
        tabVariables.Controls.Add(_variableView);
        tabLibrary.Controls.Add(_libraryView);
        tabBenchmark.Controls.Add(_benchmarkView);

        _libraryView.FunctionInsertRequested += OnFunctionInsertRequested;
        _variableView.VariablesChanged += (_, _) => _verifyView.Recalculate();
        _session.EngineChanged += (_, _) => UpdateStatus();

        tabControl.SelectedIndexChanged += (_, _) => CurrentView()?.OnActivated();
        UpdateStatus();
    }

    /// <summary>切换到指定页签（自动化截图使用）。</summary>
    public void SelectTab(int index) =>
        tabControl.SelectedIndex = Math.Clamp(index, 0, tabControl.TabPages.Count - 1);

    /// <summary>在验算页填入公式。</summary>
    public void SetFormula(string formula) => _verifyView.SetFormula(formula);

    private ViewBase? CurrentView() => tabControl.SelectedTab?.Controls.Count > 0
        ? tabControl.SelectedTab.Controls[0] as ViewBase
        : null;

    private void OnFunctionInsertRequested(object? sender, string snippet)
    {
        SelectTab(0);
        _verifyView.InsertIntoFormula(snippet);
    }

    private void UpdateStatus()
    {
        lblEngine.Text = _session.Summary;
        _ = _session.Libraries;
    }

    private void OnExitClick(object? sender, EventArgs e) => Close();

    private void OnClearCacheClick(object? sender, EventArgs e)
    {
        _session.Evaluator.ClearCache();
        UpdateStatus();
        _verifyView.Recalculate();
    }

    private void OnAboutClick(object? sender, EventArgs e)
    {
        using var dialog = new AboutDialog();
        dialog.ShowDialog(this);
    }
}
