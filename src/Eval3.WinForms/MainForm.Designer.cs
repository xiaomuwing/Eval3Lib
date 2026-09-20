using System.ComponentModel;

namespace Eval3.WinForms;

partial class MainForm
{
    private IContainer components = null;
    private MenuStrip menuStrip;
    private ToolStripMenuItem fileMenu;
    private ToolStripMenuItem exitMenuItem;
    private ToolStripMenuItem toolsMenu;
    private ToolStripMenuItem clearCacheMenuItem;
    private ToolStripMenuItem helpMenu;
    private ToolStripMenuItem aboutMenuItem;
    private TabControl tabControl;
    private TabPage tabVerify;
    private TabPage tabBatch;
    private TabPage tabVariables;
    private TabPage tabLibrary;
    private TabPage tabBenchmark;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblEngine;
    private ToolStripStatusLabel lblVersion;

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
        menuStrip = new MenuStrip();
        fileMenu = new ToolStripMenuItem();
        exitMenuItem = new ToolStripMenuItem();
        toolsMenu = new ToolStripMenuItem();
        clearCacheMenuItem = new ToolStripMenuItem();
        helpMenu = new ToolStripMenuItem();
        aboutMenuItem = new ToolStripMenuItem();
        statusStrip = new StatusStrip();
        lblEngine = new ToolStripStatusLabel();
        lblVersion = new ToolStripStatusLabel();
        tabControl = new TabControl();
        tabVerify = new TabPage();
        tabBatch = new TabPage();
        tabVariables = new TabPage();
        tabLibrary = new TabPage();
        tabBenchmark = new TabPage();
        menuStrip.SuspendLayout();
        statusStrip.SuspendLayout();
        tabControl.SuspendLayout();
        SuspendLayout();
        //
        // menuStrip
        //
        menuStrip.Items.AddRange([fileMenu, toolsMenu, helpMenu]);
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new Size(1200, 28);
        menuStrip.TabIndex = 0;
        //
        // fileMenu
        //
        fileMenu.DropDownItems.AddRange([exitMenuItem]);
        fileMenu.Name = "fileMenu";
        fileMenu.Text = "文件(&F)";
        //
        // exitMenuItem
        //
        exitMenuItem.Name = "exitMenuItem";
        exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
        exitMenuItem.Text = "退出(&X)";
        exitMenuItem.Click += OnExitClick;
        //
        // toolsMenu
        //
        toolsMenu.DropDownItems.AddRange([clearCacheMenuItem]);
        toolsMenu.Name = "toolsMenu";
        toolsMenu.Text = "工具(&T)";
        //
        // clearCacheMenuItem
        //
        clearCacheMenuItem.Name = "clearCacheMenuItem";
        clearCacheMenuItem.ShortcutKeys = Keys.Control | Keys.F5;
        clearCacheMenuItem.Text = "清空公式缓存(&C)";
        clearCacheMenuItem.Click += OnClearCacheClick;
        //
        // helpMenu
        //
        helpMenu.DropDownItems.AddRange([aboutMenuItem]);
        helpMenu.Name = "helpMenu";
        helpMenu.Text = "帮助(&H)";
        //
        // aboutMenuItem
        //
        aboutMenuItem.Name = "aboutMenuItem";
        aboutMenuItem.Text = "关于(&A)";
        aboutMenuItem.Click += OnAboutClick;
        //
        // statusStrip
        //
        statusStrip.Items.AddRange([lblEngine, lblVersion]);
        statusStrip.Location = new Point(0, 738);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1200, 24);
        statusStrip.TabIndex = 2;
        //
        // lblEngine
        //
        lblEngine.Name = "lblEngine";
        lblEngine.Spring = true;
        lblEngine.TextAlign = ContentAlignment.MiddleLeft;
        lblEngine.Text = "就绪";
        //
        // lblVersion
        //
        lblVersion.Name = "lblVersion";
        lblVersion.Text = "Eval3 .NET 10 验算器 v2.0";
        //
        // tabControl
        //
        tabControl.Controls.Add(tabVerify);
        tabControl.Controls.Add(tabBatch);
        tabControl.Controls.Add(tabVariables);
        tabControl.Controls.Add(tabLibrary);
        tabControl.Controls.Add(tabBenchmark);
        tabControl.Dock = DockStyle.Fill;
        tabControl.Location = new Point(0, 28);
        tabControl.Name = "tabControl";
        tabControl.Padding = new Point(14, 6);
        tabControl.SelectedIndex = 0;
        tabControl.Size = new Size(1200, 710);
        tabControl.TabIndex = 1;
        //
        // tabVerify
        //
        tabVerify.Name = "tabVerify";
        tabVerify.Padding = new Padding(8);
        tabVerify.Text = "单条验算";
        //
        // tabBatch
        //
        tabBatch.Name = "tabBatch";
        tabBatch.Padding = new Padding(8);
        tabBatch.Text = "批量验算";
        //
        // tabVariables
        //
        tabVariables.Name = "tabVariables";
        tabVariables.Padding = new Padding(8);
        tabVariables.Text = "变量";
        //
        // tabLibrary
        //
        tabLibrary.Name = "tabLibrary";
        tabLibrary.Padding = new Padding(8);
        tabLibrary.Text = "函数库";
        //
        // tabBenchmark
        //
        tabBenchmark.Name = "tabBenchmark";
        tabBenchmark.Padding = new Padding(8);
        tabBenchmark.Text = "性能测试";
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1200, 762);
        Controls.Add(tabControl);
        Controls.Add(statusStrip);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        MinimumSize = new Size(1024, 680);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Eval3 公式验算器（.NET 10 / WinForms）";
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        tabControl.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
