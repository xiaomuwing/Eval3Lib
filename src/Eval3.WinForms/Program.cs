using System.Drawing.Imaging;

namespace Eval3.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        string? renderTarget = ReadOption(args, "--render");
        if (renderTarget is not null)
        {
            RenderScreenshots(renderTarget, ReadOption(args, "--tab"));
            return;
        }

        Application.Run(new MainForm());
    }

    private static string? ReadOption(string[] args, string name)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                int separator = args[i].IndexOf('=');
                if (separator > 0)
                {
                    return args[i][(separator + 1)..];
                }

                return i + 1 < args.Length ? args[i + 1] : string.Empty;
            }
        }

        return null;
    }

    /// <summary>
    /// 离屏渲染界面截图（<c>--render 目录 --tab 0,1,2</c>），用于自动化验收界面布局。
    /// </summary>
    private static void RenderScreenshots(string target, string? tabOption)
    {
        int[] tabs = (tabOption ?? "0,1,2,3,4")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(text => int.TryParse(text, out int value) ? value : 0)
            .Distinct()
            .ToArray();

        string fullPath = Path.GetFullPath(target);
        bool singleFile = tabs.Length == 1 && Path.HasExtension(fullPath);
        string directory = singleFile ? Path.GetDirectoryName(fullPath)! : fullPath;
        Directory.CreateDirectory(directory);
        string extension = Path.GetExtension(fullPath) is { Length: > 0 } ext ? ext : ".png";
        string baseName = singleFile ? Path.GetFileNameWithoutExtension(fullPath) : "Eval3";

        foreach (int tab in tabs)
        {
            string file = singleFile ? fullPath : Path.Combine(directory, $"{baseName}_{tab}{extension}");
            using var form = new MainForm { Size = new Size(1400, 920) };
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-4000, 0);
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();
            form.SelectTab(tab);
            form.Refresh();
            Application.DoEvents();
            // 等待首次进入页签时自动执行的验算 / 性能测试完成
            for (int i = 0; i < 30; i++)
            {
                Application.DoEvents();
                Thread.Sleep(100);
            }

            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
            bitmap.Save(file, ImageFormat.Png);
            form.Close();
            Console.WriteLine(file);
        }
    }
}
