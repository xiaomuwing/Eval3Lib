namespace Eval3.WinForms;

/// <summary>界面的统一字体与配色。</summary>
internal static class Theme
{
    public static readonly Font CodeFont = CreateFont("Consolas", 11F);
    public static readonly Font ResultFont = CreateFont("Consolas", 20F, FontStyle.Bold);
    public static readonly Font UiFont = CreateFont("Microsoft YaHei UI", 9F);
    public static readonly Font UiFontBold = CreateFont("Microsoft YaHei UI", 9F, FontStyle.Bold);
    public static readonly Font TitleFont = CreateFont("Microsoft YaHei UI", 10.5F, FontStyle.Bold);

    public static readonly Color Accent = Color.FromArgb(0, 120, 215);
    public static readonly Color OkColor = Color.FromArgb(16, 124, 16);
    public static readonly Color WarnColor = Color.FromArgb(196, 106, 0);
    public static readonly Color ErrorColor = Color.FromArgb(196, 43, 28);
    public static readonly Color GrayText = Color.FromArgb(96, 96, 96);
    public static readonly Color PanelBack = Color.FromArgb(248, 249, 250);

    private static Font CreateFont(string family, float size, FontStyle style = FontStyle.Regular)
    {
        try
        {
            var font = new Font(family, size, style);
            if (font.Name.Equals(family, StringComparison.OrdinalIgnoreCase))
            {
                return font;
            }

            font.Dispose();
        }
        catch (ArgumentException)
        {
            // 字体缺失时退回等宽字体
        }

        return new Font(FontFamily.GenericMonospace, size, style);
    }
}
