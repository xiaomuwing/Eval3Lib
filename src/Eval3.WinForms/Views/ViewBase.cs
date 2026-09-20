namespace Eval3.WinForms;

/// <summary>验算界面各功能页的基类。</summary>
internal abstract class ViewBase : UserControl
{
    private SplitContainer? _pendingSplit;
    private int _pendingDistance;
    private int _pendingPanel1Min;
    private int _pendingPanel2Min;

    protected ViewBase(EvaluationSession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    protected EvaluationSession Session { get; }

    /// <summary>页面被激活时调用。</summary>
    public virtual void OnActivated()
    {
    }

    /// <summary>
    /// 按逻辑像素设置分隔条位置：<see cref="SplitContainer"/> 在控件拿到最终尺寸前设置会按比例被拉伸，
    /// 因此延后到页面加载完成后再设置。
    /// </summary>
    protected void SetSplitterDistance(SplitContainer split, int logicalDistance, int panel1MinSize = 0, int panel2MinSize = 0)
    {
        _pendingSplit = split;
        _pendingDistance = logicalDistance;
        _pendingPanel1Min = panel1MinSize;
        _pendingPanel2Min = panel2MinSize;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_pendingSplit is not { } split)
        {
            return;
        }

        _pendingSplit = null;

        // 最小面板尺寸必须在控件拿到真实宽度后设置，否则构造期会因越界抛异常
        if (_pendingPanel1Min > 0)
        {
            split.Panel1MinSize = split.LogicalToDeviceUnits(_pendingPanel1Min);
        }

        if (_pendingPanel2Min > 0)
        {
            split.Panel2MinSize = Math.Min(split.LogicalToDeviceUnits(_pendingPanel2Min), Math.Max(0, split.Width - split.Panel1MinSize - split.SplitterWidth));
        }

        int maximum = Math.Max(split.Panel1MinSize, split.Width - split.Panel2MinSize - split.SplitterWidth);
        split.SplitterDistance = Math.Clamp(
            split.LogicalToDeviceUnits(_pendingDistance),
            split.Panel1MinSize,
            maximum);
    }
}
