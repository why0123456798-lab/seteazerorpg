using System.Drawing.Drawing2D;
using System.Globalization;

namespace RPGBattleMaker.Presentation.Controls;

public sealed class D20RollControl : Control
{
    private readonly System.Windows.Forms.Timer animationTimer;
    private readonly Random random = Random.Shared;
    private DateTime animationStarted;
    private int targetValue;
    private int displayedValue;
    private bool rolling;
    private TaskCompletionSource<bool>? animationCompletion;

    public D20RollControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = ColorTranslator.FromHtml("#10131b");
        Dock = DockStyle.Fill;

        animationTimer = new System.Windows.Forms.Timer { Interval = 24 };
        animationTimer.Tick += (_, _) => TickAnimation();
    }

    public Task RollAsync(int value)
    {
        targetValue = Math.Clamp(value, 1, 20);
        displayedValue = random.Next(1, 21);
        animationStarted = DateTime.UtcNow;
        rolling = true;
        Visible = true;
        BringToFront();

        animationCompletion?.TrySetCanceled();
        animationCompletion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        animationTimer.Start();
        Invalidate();
        return animationCompletion.Task;
    }

    private void TickAnimation()
    {
        double elapsed = (DateTime.UtcNow - animationStarted).TotalMilliseconds;
        const double duration = 3000.0;
        double progress = Math.Clamp(elapsed / duration, 0.0, 1.0);

        displayedValue = progress < 0.82 ? random.Next(1, 21) : targetValue;
        Invalidate();

        if (progress >= 1.0)
        {
            animationTimer.Stop();
            rolling = false;
            displayedValue = targetValue;
            Invalidate();
            animationCompletion?.TrySetResult(true);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        float w = ClientSize.Width;
        float h = ClientSize.Height;

        using (var table = new LinearGradientBrush(
                   new RectangleF(0, 0, w, h),
                   ColorTranslator.FromHtml("#171c27"),
                   ColorTranslator.FromHtml("#090c12"), 90f))
            e.Graphics.FillRectangle(table, ClientRectangle);

        // Tabuleiro em perspectiva.
        using (var grid = new Pen(Color.FromArgb(20, 205, 190, 130), 1f))
        {
            for (int y = (int)(h * .48f); y < h; y += 42)
            {
                float p = (y - h * .48f) / (h * .52f);
                e.Graphics.DrawLine(grid, w * .10f - p * 45f, y, w * .90f + p * 45f, y);
            }

            for (int x = -300; x <= w + 300; x += 65)
                e.Graphics.DrawLine(grid, w / 2f + (x - w / 2f) * .15f, h * .48f, x, h);
        }

        using (var rim = new Pen(Color.FromArgb(90, 205, 170, 105), 2f))
            e.Graphics.DrawRoundedRectangle(rim,
                new RectangleF(w * .09f, h * .18f, w * .82f, h * .70f), 26f);

        double elapsed = rolling ? (DateTime.UtcNow - animationStarted).TotalMilliseconds : 3000;
        double progress = Math.Clamp(elapsed / 3000.0, 0.0, 1.0);

        // Queda + quicadas.
        double fall;
        if (progress < .70)
        {
            double p = progress / .70;
            fall = -1.0 + Math.Pow(p, 1.75) * 1.55;
        }
        else
        {
            double p = (progress - .70) / .30;
            fall = .55 - Math.Sin(p * Math.PI * 3.0) * (1.0 - p) * .28;
        }

        float cx = w / 2f;
        float baseY = h * .53f;
        float cy = baseY + (float)(fall * h * .20);

        double spinX = progress * Math.PI * 19.0;
        double spinY = progress * Math.PI * 15.0;
        double spinZ = progress * Math.PI * 13.0;

        float facing = (float)(.70 + .30 * Math.Abs(Math.Cos(spinY)));
        float depth = (float)(.72 + .28 * Math.Abs(Math.Sin(spinX)));
        float tilt = (float)Math.Sin(spinZ) * .20f;
        float radius = Math.Min(w, h) * .18f;

        float shadowScale = Math.Clamp(1.05f - Math.Abs((float)fall) * .45f, .45f, 1.1f);
        using (var shadow = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
            e.Graphics.FillEllipse(shadow, cx - radius * .90f * shadowScale,
                baseY + radius * .78f, radius * 1.80f * shadowScale, radius * .34f);

        if (rolling && progress < .68)
        {
            using var trail = new Pen(Color.FromArgb(35, 245, 215, 150), 4f);
            trail.StartCap = trail.EndCap = LineCap.Round;
            e.Graphics.DrawLine(trail, cx - radius * .25f, cy - radius * 1.35f,
                cx + radius * .18f, cy - radius * 2.5f);
        }

        DrawD20(e.Graphics, cx, cy, radius, facing, depth, tilt);

        using Font titleFont = new Font("Segoe UI Semibold", 15f, FontStyle.Bold);
        using Font subtitleFont = new Font("Segoe UI", 10.5f);
        DrawCenteredText(e.Graphics, rolling ? "ROLANDO D20" : "RESULTADO",
            titleFont, ColorTranslator.FromHtml("#f2d58c"), w / 2f, h * .10f);
        DrawCenteredText(e.Graphics,
            rolling ? "O destino está sendo decidido..." : $"A rolagem resultou em {displayedValue}",
            subtitleFont, ColorTranslator.FromHtml("#aeb7c7"), w / 2f, h * .87f);
    }

    private void DrawD20(Graphics g, float cx, float cy, float radius,
        float facing, float depth, float tilt)
    {
        PointF Rotate(PointF p)
        {
            float dx = p.X - cx, dy = p.Y - cy;
            float c = MathF.Cos(tilt), s = MathF.Sin(tilt);
            return new PointF(cx + dx * c - dy * s, cy + dx * s + dy * c);
        }

        PointF P(float x, float y) => Rotate(new PointF(cx + x * facing, cy + y * depth));

        PointF top = P(0, -radius);
        PointF ul = P(-radius * .76f, -radius * .30f);
        PointF ur = P(radius * .76f, -radius * .30f);
        PointF ll = P(-radius * .66f, radius * .64f);
        PointF lr = P(radius * .66f, radius * .64f);
        PointF bottom = P(0, radius);
        PointF center = P(0, 0);

        using (var shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            g.FillPolygon(shadow, new[]
            {
                new PointF(top.X + 7, top.Y + 10), new PointF(ur.X + 7, ur.Y + 10),
                new PointF(lr.X + 7, lr.Y + 10), new PointF(bottom.X + 7, bottom.Y + 10),
                new PointF(ll.X + 7, ll.Y + 10), new PointF(ul.X + 7, ul.Y + 10)
            });

        FillFace(g, new[] { top, ur, center }, "#d7a94f", "#fff1ad");
        FillFace(g, new[] { ur, lr, center }, "#9f6d27", "#e5b95e");
        FillFace(g, new[] { lr, bottom, center }, "#6f451e", "#b8792b");
        FillFace(g, new[] { bottom, ll, center }, "#59341d", "#9c5e2b");
        FillFace(g, new[] { ll, ul, center }, "#70431f", "#b47a35");
        FillFace(g, new[] { ul, top, center }, "#a9782b", "#e1b85e");

        using var outline = new Pen(Color.FromArgb(255, 255, 226, 150), Math.Max(2f, radius * .025f));
        outline.LineJoin = LineJoin.Round;
        g.DrawPolygon(outline, new[] { top, ur, lr, bottom, ll, ul });
        foreach (PointF p in new[] { top, ur, lr, bottom, ll, ul })
            g.DrawLine(outline, p, center);

        string number = displayedValue.ToString(CultureInfo.InvariantCulture);
        using Font numberFont = new Font("Segoe UI Black", Math.Max(24f, radius * .50f), FontStyle.Bold);
        SizeF ts = g.MeasureString(number, numberFont);

        using var ns = new SolidBrush(Color.FromArgb(150, 35, 18, 4));
        g.DrawString(number, numberFont, ns, center.X - ts.Width / 2f + 3, center.Y - ts.Height / 2f + 4);

        using var nb = new SolidBrush(Color.FromArgb(255, 255, 248, 218));
        g.DrawString(number, numberFont, nb, center.X - ts.Width / 2f, center.Y - ts.Height / 2f);

        using var shine = new SolidBrush(Color.FromArgb(75, 255, 255, 230));
        g.FillEllipse(shine, cx - radius * .28f, cy - radius * .58f, radius * .22f, radius * .12f);
    }

    private static void FillFace(Graphics g, PointF[] points, string darkHex, string lightHex)
    {
        float minX = points.Min(p => p.X), minY = points.Min(p => p.Y);
        float maxX = points.Max(p => p.X), maxY = points.Max(p => p.Y);

        using var brush = new LinearGradientBrush(
            new RectangleF(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY)),
            ColorTranslator.FromHtml(lightHex), ColorTranslator.FromHtml(darkHex), 135f);
        g.FillPolygon(brush, points);
    }

    private static void DrawCenteredText(Graphics g, string text, Font font, Color color, float x, float y)
    {
        SizeF size = g.MeasureString(text, font);
        using var brush = new SolidBrush(color);
        g.DrawString(text, font, brush, x - size.Width / 2f, y - size.Height / 2f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            animationTimer.Stop();
            animationTimer.Dispose();
            animationCompletion?.TrySetCanceled();
        }
        base.Dispose(disposing);
    }
}

internal static class GraphicsExtensions
{
    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, RectangleF rectangle, float radius)
    {
        float d = radius * 2f;
        using var path = new GraphicsPath();
        path.AddArc(rectangle.X, rectangle.Y, d, d, 180, 90);
        path.AddArc(rectangle.Right - d, rectangle.Y, d, d, 270, 90);
        path.AddArc(rectangle.Right - d, rectangle.Bottom - d, d, d, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        graphics.DrawPath(pen, path);
    }
}
