namespace RPGBattleMaker.Presentation.Controls;

public sealed class RarityCard : Panel
{
    internal Color BorderColor = Color.White;
    internal int BorderThickness = 2;

    public RarityCard()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);

        Margin = Padding.Empty;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using Pen pen = new(BorderColor, BorderThickness);
        float offset = BorderThickness / 2f;

        e.Graphics.DrawRectangle(
            pen,
            offset,
            offset,
            Math.Max(0, ClientSize.Width - BorderThickness),
            Math.Max(0, ClientSize.Height - BorderThickness));
    }
}
