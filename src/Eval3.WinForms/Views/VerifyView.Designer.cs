using System.ComponentModel;

namespace Eval3.WinForms;

partial class VerifyView
{
    private IContainer components = null;
    private TableLayoutPanel layoutRoot;
    private FlowLayoutPanel panelOptions;
    private Label lblSyntaxCaption;
    private ComboBox cboSyntax;
    private Label lblPercentCaption;
    private ComboBox cboPercent;
    private CheckBox chkCaseSensitive;
    private Button btnEvaluate;
    private Button btnClear;
    private Button btnSample;
    private Button btnCopyResult;
    private TextBox txtFormula;
    private Label lblError;
    private SplitContainer splitMain;
    private TableLayoutPanel layoutResult;
    private Label lblResultCaption;
    private Label lblResult;
    private TableLayoutPanel tableInfo;
    private Label lblTypeCaption;
    private Label lblType;
    private Label lblSystemTypeCaption;
    private Label lblSystemType;
    private Label lblParseCaption;
    private Label lblParse;
    private Label lblEvaluateCaption;
    private Label lblEvaluate;
    private Label lblFastPathCaption;
    private Label lblFastPath;
    private Label lblCacheCaption;
    private Label lblCache;
    private Label lblTreeCaption;
    private TextBox txtTree;
    private Label lblHint;
    private GroupBox grpHistory;
    private ListBox lstHistory;
    private Button btnClearHistory;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        layoutRoot = new TableLayoutPanel();
        panelOptions = new FlowLayoutPanel();
        lblSyntaxCaption = new Label();
        cboSyntax = new ComboBox();
        lblPercentCaption = new Label();
        cboPercent = new ComboBox();
        chkCaseSensitive = new CheckBox();
        btnEvaluate = new Button();
        btnClear = new Button();
        btnSample = new Button();
        btnCopyResult = new Button();
        txtFormula = new TextBox();
        lblError = new Label();
        splitMain = new SplitContainer();
        layoutResult = new TableLayoutPanel();
        lblResultCaption = new Label();
        lblResult = new Label();
        tableInfo = new TableLayoutPanel();
        lblTypeCaption = new Label();
        lblType = new Label();
        lblSystemTypeCaption = new Label();
        lblSystemType = new Label();
        lblParseCaption = new Label();
        lblParse = new Label();
        lblEvaluateCaption = new Label();
        lblEvaluate = new Label();
        lblFastPathCaption = new Label();
        lblFastPath = new Label();
        lblCacheCaption = new Label();
        lblCache = new Label();
        lblTreeCaption = new Label();
        txtTree = new TextBox();
        lblHint = new Label();
        grpHistory = new GroupBox();
        lstHistory = new ListBox();
        btnClearHistory = new Button();
        layoutRoot.SuspendLayout();
        panelOptions.SuspendLayout();
        ((ISupportInitialize)splitMain).BeginInit();
        splitMain.Panel1.SuspendLayout();
        splitMain.Panel2.SuspendLayout();
        splitMain.SuspendLayout();
        layoutResult.SuspendLayout();
        tableInfo.SuspendLayout();
        grpHistory.SuspendLayout();
        SuspendLayout();
        //
        // panelOptions
        //
        panelOptions.Controls.Add(lblSyntaxCaption);
        panelOptions.Controls.Add(cboSyntax);
        panelOptions.Controls.Add(lblPercentCaption);
        panelOptions.Controls.Add(cboPercent);
        panelOptions.Controls.Add(chkCaseSensitive);
        panelOptions.Controls.Add(btnEvaluate);
        panelOptions.Controls.Add(btnClear);
        panelOptions.Controls.Add(btnSample);
        panelOptions.Controls.Add(btnCopyResult);
        panelOptions.Dock = DockStyle.Fill;
        panelOptions.Location = new Point(4, 4);
        panelOptions.Margin = new Padding(4);
        panelOptions.Name = "panelOptions";
        panelOptions.Padding = new Padding(0, 2, 0, 2);
        panelOptions.Size = new Size(1150, 40);
        //
        // lblSyntaxCaption
        //
        lblSyntaxCaption.AutoSize = true;
        lblSyntaxCaption.Margin = new Padding(3, 8, 2, 3);
        lblSyntaxCaption.Text = "语法：";
        //
        // cboSyntax
        //
        cboSyntax.DropDownStyle = ComboBoxStyle.DropDownList;
        cboSyntax.Margin = new Padding(0, 5, 12, 3);
        cboSyntax.Name = "cboSyntax";
        cboSyntax.Size = new Size(104, 25);
        cboSyntax.TabIndex = 1;
        //
        // lblPercentCaption
        //
        lblPercentCaption.AutoSize = true;
        lblPercentCaption.Margin = new Padding(3, 8, 2, 3);
        lblPercentCaption.Text = "％ 语义：";
        //
        // cboPercent
        //
        cboPercent.DropDownStyle = ComboBoxStyle.DropDownList;
        cboPercent.Margin = new Padding(0, 5, 12, 3);
        cboPercent.Name = "cboPercent";
        cboPercent.Size = new Size(104, 25);
        cboPercent.TabIndex = 3;
        //
        // chkCaseSensitive
        //
        chkCaseSensitive.AutoSize = true;
        chkCaseSensitive.Margin = new Padding(0, 8, 12, 3);
        chkCaseSensitive.Name = "chkCaseSensitive";
        chkCaseSensitive.Text = "区分大小写";
        //
        // btnEvaluate
        //
        btnEvaluate.Font = Theme.UiFontBold;
        btnEvaluate.Margin = new Padding(12, 3, 6, 3);
        btnEvaluate.Name = "btnEvaluate";
        btnEvaluate.Size = new Size(96, 30);
        btnEvaluate.Text = "计算 (F5)";
        btnEvaluate.UseVisualStyleBackColor = true;
        btnEvaluate.Click += OnEvaluateClick;
        //
        // btnClear
        //
        btnClear.Margin = new Padding(0, 3, 6, 3);
        btnClear.Size = new Size(80, 30);
        btnClear.Text = "清空";
        btnClear.UseVisualStyleBackColor = true;
        btnClear.Click += OnClearClick;
        //
        // btnSample
        //
        btnSample.Margin = new Padding(0, 3, 6, 3);
        btnSample.Size = new Size(96, 30);
        btnSample.Text = "示例公式";
        btnSample.UseVisualStyleBackColor = true;
        btnSample.Click += OnSampleClick;
        //
        // btnCopyResult
        //
        btnCopyResult.Margin = new Padding(0, 3, 6, 3);
        btnCopyResult.Size = new Size(96, 30);
        btnCopyResult.Text = "复制结果";
        btnCopyResult.UseVisualStyleBackColor = true;
        btnCopyResult.Click += OnCopyResultClick;
        //
        // txtFormula
        //
        txtFormula.Dock = DockStyle.Fill;
        txtFormula.Font = Theme.CodeFont;
        txtFormula.Location = new Point(4, 52);
        txtFormula.Margin = new Padding(4);
        txtFormula.Multiline = true;
        txtFormula.Name = "txtFormula";
        txtFormula.ScrollBars = ScrollBars.Both;
        txtFormula.Size = new Size(1150, 70);
        txtFormula.TabIndex = 2;
        txtFormula.WordWrap = false;
        txtFormula.KeyDown += OnFormulaKeyDown;
        txtFormula.TextChanged += OnFormulaTextChanged;
        //
        // lblError
        //
        lblError.Dock = DockStyle.Fill;
        lblError.Font = Theme.UiFont;
        lblError.ForeColor = Theme.GrayText;
        lblError.Location = new Point(8, 130);
        lblError.Margin = new Padding(8, 4, 8, 4);
        lblError.Name = "lblError";
        lblError.Size = new Size(1142, 46);
        lblError.Text = "就绪";
        //
        // splitMain
        //
        splitMain.Dock = DockStyle.Fill;
        splitMain.Location = new Point(4, 184);
        splitMain.Margin = new Padding(4);
        splitMain.Name = "splitMain";
        splitMain.Panel1.Controls.Add(layoutResult);
        splitMain.Panel1MinSize = 320;
        splitMain.Panel2.Controls.Add(txtTree);
        splitMain.Panel2.Controls.Add(lblTreeCaption);
        splitMain.Panel2MinSize = 240;
        splitMain.Size = new Size(1150, 330);
        splitMain.SplitterDistance = 520;
        splitMain.SplitterWidth = 6;
        splitMain.TabIndex = 3;
        //
        // layoutResult
        //
        layoutResult.ColumnCount = 1;
        layoutResult.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutResult.Controls.Add(lblResultCaption, 0, 0);
        layoutResult.Controls.Add(lblResult, 0, 1);
        layoutResult.Controls.Add(tableInfo, 0, 2);
        layoutResult.Controls.Add(lblHint, 0, 3);
        layoutResult.Dock = DockStyle.Fill;
        layoutResult.Location = new Point(0, 0);
        layoutResult.Name = "layoutResult";
        layoutResult.RowCount = 4;
        layoutResult.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        layoutResult.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        layoutResult.RowStyles.Add(new RowStyle(SizeType.Absolute, 156F));
        layoutResult.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layoutResult.Size = new Size(520, 330);
        layoutResult.TabIndex = 0;
        //
        // lblResultCaption
        //
        lblResultCaption.Dock = DockStyle.Fill;
        lblResultCaption.Font = Theme.TitleFont;
        lblResultCaption.Text = "计算结果";
        lblResultCaption.TextAlign = ContentAlignment.MiddleLeft;
        //
        // lblResult
        //
        lblResult.AutoEllipsis = true;
        lblResult.Dock = DockStyle.Fill;
        lblResult.Font = Theme.ResultFont;
        lblResult.ForeColor = Theme.Accent;
        lblResult.Padding = new Padding(6, 0, 0, 0);
        lblResult.Text = "—";
        lblResult.TextAlign = ContentAlignment.MiddleLeft;
        //
        // tableInfo
        //
        tableInfo.ColumnCount = 4;
        tableInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        tableInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tableInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        tableInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tableInfo.Controls.Add(lblTypeCaption, 0, 0);
        tableInfo.Controls.Add(lblType, 1, 0);
        tableInfo.Controls.Add(lblSystemTypeCaption, 2, 0);
        tableInfo.Controls.Add(lblSystemType, 3, 0);
        tableInfo.Controls.Add(lblParseCaption, 0, 1);
        tableInfo.Controls.Add(lblParse, 1, 1);
        tableInfo.Controls.Add(lblEvaluateCaption, 2, 1);
        tableInfo.Controls.Add(lblEvaluate, 3, 1);
        tableInfo.Controls.Add(lblFastPathCaption, 0, 2);
        tableInfo.Controls.Add(lblFastPath, 1, 2);
        tableInfo.Controls.Add(lblCacheCaption, 2, 2);
        tableInfo.Controls.Add(lblCache, 3, 2);
        tableInfo.Dock = DockStyle.Fill;
        tableInfo.Location = new Point(3, 93);
        tableInfo.Name = "tableInfo";
        tableInfo.RowCount = 3;
        tableInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tableInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tableInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tableInfo.Size = new Size(514, 234);
        tableInfo.TabIndex = 2;
        //
        // lblHint
        //
        lblHint.Dock = DockStyle.Fill;
        lblHint.ForeColor = Theme.GrayText;
        lblHint.Padding = new Padding(4, 6, 4, 0);
        lblHint.Text =
            "使用提示" + Environment.NewLine +
            "· 算术：+ - * / % ( )      连接：&" + Environment.NewLine +
            "· 比较：= <> < <= > >=    逻辑：and / or / not" + Environment.NewLine +
            "· 条件：if(条件, 真值, 假值)   插值：\"合计：%[单价 * 数量]\"" + Environment.NewLine +
            "· 日期：#2024/01/31#      函数见“函数库”页（双击插入）" + Environment.NewLine +
            "· 变量在“变量”页维护，支持中文名；F5 计算并记入历史";
        //
        // 信息标签
        //
        ConfigureInfoLabel(lblTypeCaption, "逻辑类型：");
        ConfigureInfoLabel(lblSystemTypeCaption, ".NET 类型：");
        ConfigureInfoLabel(lblParseCaption, "解析耗时：");
        ConfigureInfoLabel(lblEvaluateCaption, "求值耗时：");
        ConfigureInfoLabel(lblFastPathCaption, "数值快路径：");
        ConfigureInfoLabel(lblCacheCaption, "公式缓存：");
        ConfigureInfoValue(lblType);
        ConfigureInfoValue(lblSystemType);
        ConfigureInfoValue(lblParse);
        ConfigureInfoValue(lblEvaluate);
        ConfigureInfoValue(lblFastPath);
        ConfigureInfoValue(lblCache);
        //
        // lblTreeCaption
        //
        lblTreeCaption.Dock = DockStyle.Top;
        lblTreeCaption.Font = Theme.TitleFont;
        lblTreeCaption.Height = 28;
        lblTreeCaption.Text = "表达式树（解析结果）";
        lblTreeCaption.TextAlign = ContentAlignment.MiddleLeft;
        //
        // txtTree
        //
        txtTree.Dock = DockStyle.Fill;
        txtTree.Font = Theme.CodeFont;
        txtTree.Location = new Point(0, 28);
        txtTree.Multiline = true;
        txtTree.Name = "txtTree";
        txtTree.ReadOnly = true;
        txtTree.ScrollBars = ScrollBars.Both;
        txtTree.Size = new Size(624, 302);
        txtTree.TabIndex = 0;
        txtTree.WordWrap = false;
        txtTree.BackColor = Theme.PanelBack;
        //
        // grpHistory
        //
        grpHistory.Controls.Add(lstHistory);
        grpHistory.Controls.Add(btnClearHistory);
        grpHistory.Dock = DockStyle.Fill;
        grpHistory.Location = new Point(4, 522);
        grpHistory.Margin = new Padding(4);
        grpHistory.Name = "grpHistory";
        grpHistory.Padding = new Padding(8);
        grpHistory.Size = new Size(1150, 126);
        grpHistory.TabIndex = 4;
        grpHistory.TabStop = false;
        grpHistory.Text = "历史记录（双击回填公式）";
        //
        // lstHistory
        //
        lstHistory.Dock = DockStyle.Fill;
        lstHistory.Font = Theme.CodeFont;
        lstHistory.IntegralHeight = false;
        lstHistory.Location = new Point(8, 24);
        lstHistory.Name = "lstHistory";
        lstHistory.Size = new Size(1030, 94);
        lstHistory.TabIndex = 0;
        lstHistory.DoubleClick += OnHistoryDoubleClick;
        //
        // btnClearHistory
        //
        btnClearHistory.Dock = DockStyle.Right;
        btnClearHistory.Location = new Point(1038, 24);
        btnClearHistory.Name = "btnClearHistory";
        btnClearHistory.Size = new Size(104, 94);
        btnClearHistory.Text = "清空历史";
        btnClearHistory.UseVisualStyleBackColor = true;
        btnClearHistory.Click += OnClearHistoryClick;
        //
        // layoutRoot
        //
        layoutRoot.ColumnCount = 1;
        layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutRoot.Controls.Add(panelOptions, 0, 0);
        layoutRoot.Controls.Add(txtFormula, 0, 1);
        layoutRoot.Controls.Add(lblError, 0, 2);
        layoutRoot.Controls.Add(splitMain, 0, 3);
        layoutRoot.Controls.Add(grpHistory, 0, 4);
        layoutRoot.Dock = DockStyle.Fill;
        layoutRoot.Location = new Point(8, 8);
        layoutRoot.Name = "layoutRoot";
        layoutRoot.RowCount = 5;
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 134F));
        layoutRoot.Size = new Size(1158, 656);
        layoutRoot.TabIndex = 0;
        //
        // VerifyView
        //
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(layoutRoot);
        Name = "VerifyView";
        Padding = new Padding(8);
        Size = new Size(1174, 672);
        layoutRoot.ResumeLayout(false);
        panelOptions.ResumeLayout(false);
        panelOptions.PerformLayout();
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        ((ISupportInitialize)splitMain).EndInit();
        splitMain.ResumeLayout(false);
        layoutResult.ResumeLayout(false);
        tableInfo.ResumeLayout(false);
        grpHistory.ResumeLayout(false);
        ResumeLayout(false);
    }

    private static void ConfigureInfoLabel(Label label, string text)
    {
        label.Dock = DockStyle.Fill;
        label.ForeColor = Theme.GrayText;
        label.Text = text;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static void ConfigureInfoValue(Label label)
    {
        label.Dock = DockStyle.Fill;
        label.Text = "—";
        label.TextAlign = ContentAlignment.MiddleLeft;
    }
}
