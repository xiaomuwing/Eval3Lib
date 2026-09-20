namespace Eval3.WinForms;

/// <summary>“关于”对话框。</summary>
internal sealed class AboutDialog : Form
{
    public AboutDialog()
    {
        Text = "关于 Eval3 验算器";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 340);

        var text = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = SystemColors.Window,
            Font = Theme.UiFont,
            ScrollBars = ScrollBars.Vertical,
            Text =
                "Eval3 公式计算组件（.NET 10 / C# 版）" + Environment.NewLine +
                Environment.NewLine +
                "· 计算内核：词法分析 + 递归下降语法分析 + 表达式树求值" + Environment.NewLine +
                "· 解析期完成类型推导、重载选择与调用委托编译，求值期无反射、无类型判断" + Environment.NewLine +
                "· 纯数值公式自动编译为免装箱委托；反射调用编译为强类型委托" + Environment.NewLine +
                Environment.NewLine +
                "支持的语法：" + Environment.NewLine +
                "  + - * / %  ( )  &  比较运算 ( = <> < <= > >= )  and / or / not" + Environment.NewLine +
                "  if(条件, 真值, 假值)    \"文本\" '文本'    #2024/01/31# 日期" + Environment.NewLine +
                "  \"合计：%[单价 * 数量]\"  字符串插值" + Environment.NewLine +
                "  obj.Method(...) / 属性 / 字段 / 数组下标 arr[0]" + Environment.NewLine +
                Environment.NewLine +
                "原始 VB 版与 C# 版源码保留在 legacy/ 目录，可对照比较。",
        };

        var button = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            Size = new Size(96, 30),
            Anchor = AnchorStyles.Right,
        };

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(12, 8, 12, 8) };
        bottom.Controls.Add(button);
        button.Location = new Point(bottom.Width - button.Width - 12, 8);
        button.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        Controls.Add(text);
        Controls.Add(bottom);
        AcceptButton = button;
        CancelButton = button;
    }
}
