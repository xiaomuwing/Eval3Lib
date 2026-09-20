namespace Eval3.WinForms;

/// <summary>单条验算页面：输入公式 → 结果 / 耗时 / 表达式树 / 错误定位。</summary>
internal sealed partial class VerifyView : ViewBase
{
    private const int MaxHistory = 50;
    private readonly List<string> _history = [];
    private bool _suppressOptionEvents;
    private bool _loaded;

    public VerifyView(EvaluationSession session)
        : base(session)
    {
        InitializeComponent();
        InitializeOptions();
        SetSplitterDistance(splitMain, 520);
        txtFormula.Text = Session.SampleFormula;
        Load += (_, _) =>
        {
            _loaded = true;
            Recalculate(recordHistory: true);
            txtFormula.Focus();
            txtFormula.SelectionStart = txtFormula.TextLength;
        };
    }

    /// <summary>设置公式并立即验算。</summary>
    public void SetFormula(string formula)
    {
        txtFormula.Text = formula;
        Recalculate();
    }

    /// <summary>在当前光标位置插入函数调用片段。</summary>
    public void InsertIntoFormula(string snippet)
    {
        int start = txtFormula.SelectionStart;
        string text = txtFormula.Text;
        txtFormula.Text = text[..start] + snippet + text[(start + txtFormula.SelectionLength)..];

        int caret = start + snippet.Length;
        if (snippet.EndsWith("()", StringComparison.Ordinal))
        {
            caret--;
        }

        txtFormula.SelectionStart = caret;
        txtFormula.SelectionLength = 0;
        txtFormula.Focus();
        Recalculate();
    }

    /// <summary>重新验算当前公式。</summary>
    public void Recalculate(bool recordHistory = false)
    {
        string formula = txtFormula.Text;
        EvaluationOutcome outcome = Session.Evaluate(formula);
        ApplyOutcome(outcome);

        if (recordHistory && outcome.Success && !string.IsNullOrWhiteSpace(formula))
        {
            _history.Remove(formula);
            _history.Insert(0, formula);
            if (_history.Count > MaxHistory)
            {
                _history.RemoveRange(MaxHistory, _history.Count - MaxHistory);
            }

            RefreshHistory();
        }
    }

    public override void OnActivated()
    {
        if (_loaded)
        {
            Recalculate();
        }
    }

    private void InitializeOptions()
    {
        _suppressOptionEvents = true;
        cboSyntax.Items.AddRange(["C# 语法", "VB 语法"]);
        cboSyntax.SelectedIndex = Session.Syntax == ParserSyntax.CSharp ? 0 : 1;
        cboPercent.Items.AddRange(["％ 取模 (10 % 3 = 1)", "％ 百分比 (100 + 5% = 105)"]);
        cboPercent.SelectedIndex = Session.PercentMode == PercentMode.Modulo ? 0 : 1;
        chkCaseSensitive.Checked = Session.CaseSensitive;
        _suppressOptionEvents = false;

        cboSyntax.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressOptionEvents)
            {
                return;
            }

            Session.Syntax = cboSyntax.SelectedIndex == 0 ? ParserSyntax.CSharp : ParserSyntax.VisualBasic;
            Recalculate();
        };

        cboPercent.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressOptionEvents)
            {
                return;
            }

            Session.PercentMode = cboPercent.SelectedIndex == 0 ? PercentMode.Modulo : PercentMode.PercentOf;
            Recalculate();
        };

        chkCaseSensitive.CheckedChanged += (_, _) =>
        {
            if (_suppressOptionEvents)
            {
                return;
            }

            Session.CaseSensitive = chkCaseSensitive.Checked;
            Recalculate();
        };
    }

    private void ApplyOutcome(EvaluationOutcome outcome)
    {
        if (outcome.Success)
        {
            lblResult.Text = outcome.Result.Length == 0 ? "（空）" : outcome.Result;
            lblResult.ForeColor = Theme.Accent;
            lblType.Text = outcome.ResultType.ToString();
            lblSystemType.Text = outcome.SystemType?.Name ?? "-";
            lblError.ForeColor = Theme.OkColor;
            lblError.Text = "计算成功。";
            txtTree.Text = outcome.Tree;
        }
        else
        {
            lblResult.Text = "—";
            lblResult.ForeColor = Theme.ErrorColor;
            lblType.Text = "-";
            lblSystemType.Text = "-";
            lblError.ForeColor = Theme.ErrorColor;
            lblError.Text = DescribeError(outcome.Error);
            txtTree.Text = string.Empty;
        }

        lblParse.Text = outcome.CacheHit
            ? $"缓存命中（{outcome.ParseMilliseconds:F4} ms）"
            : $"{outcome.ParseMilliseconds:F4} ms";
        lblEvaluate.Text = $"{outcome.EvaluateMilliseconds:F4} ms";
        lblFastPath.Text = outcome.NumericFastPath ? "启用（免装箱数值委托）" : "未启用";
        lblCache.Text = outcome.CacheHit ? "命中" : "未命中";
    }

    private static string DescribeError(EvalException? error) => error switch
    {
        null => string.Empty,
        ParserException parser => $"[位置 {parser.Position}] {parser.RawMessage}{Environment.NewLine}定位：{MarkPosition(parser)}",
        VariableNotFoundException missing => $"找不到变量或方法：{missing.VariableName}",
        _ => error.Message,
    };

    private static string MarkPosition(ParserException error)
    {
        string formula = error.Formula;
        int position = Math.Clamp(error.Position, 0, formula.Length);
        if (position >= formula.Length)
        {
            return formula + " ⟦公式末尾⟧";
        }

        int length = Math.Clamp(error.Length <= 0 ? 1 : error.Length, 1, formula.Length - position);
        return formula[..position] + "⟦" + formula.Substring(position, length) + "⟧" + formula[(position + length)..];
    }

    private void RefreshHistory()
    {
        string? selected = lstHistory.SelectedItem as string;
        lstHistory.BeginUpdate();
        lstHistory.Items.Clear();
        foreach (string item in _history)
        {
            lstHistory.Items.Add(item);
        }

        if (selected is not null)
        {
            lstHistory.SelectedItem = selected;
        }

        lstHistory.EndUpdate();
    }

    private void OnEvaluateClick(object? sender, EventArgs e) => Recalculate(recordHistory: true);

    private void OnClearClick(object? sender, EventArgs e)
    {
        txtFormula.Text = string.Empty;
        Recalculate();
    }

    private void OnSampleClick(object? sender, EventArgs e) => SetFormula(Session.SampleFormula);

    private void OnCopyResultClick(object? sender, EventArgs e)
    {
        if (lblResult.Text.Length > 0 && lblResult.Text != "—")
        {
            Clipboard.SetText(lblResult.Text);
        }
    }

    private void OnFormulaKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5 || (e.KeyCode == Keys.Enter && e.Control) || (e.KeyCode == Keys.Enter && e.Shift))
        {
            Recalculate(recordHistory: true);
            e.SuppressKeyPress = true;
            e.Handled = true;
        }
    }

    private void OnFormulaTextChanged(object? sender, EventArgs e)
    {
        if (!_loaded || txtFormula.Text.Length == 0 || txtFormula.Text.Length > 4000)
        {
            return;
        }

        // 输入即验算：解析在微秒级，实时反馈不影响输入流畅度
        Recalculate();
    }

    private void OnHistoryDoubleClick(object? sender, EventArgs e)
    {
        if (lstHistory.SelectedItem is string formula)
        {
            SetFormula(formula);
        }
    }

    private void OnClearHistoryClick(object? sender, EventArgs e)
    {
        _history.Clear();
        RefreshHistory();
    }

}
