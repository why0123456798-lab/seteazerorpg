using System.Drawing.Drawing2D;

namespace RPGBattleMaker.Presentation.Controls;

public enum VectorIconKind
{
    Coin, Sword, Shield, Target, Heart, Dice, Recruit, Relic, Team, Check,
    Potion, Armor, Axe, Tome, Map, Star, Dragon, Eye, Sparkles, Question, Skull, Damage
}

public sealed class VectorIcon : Control
{
    private readonly VectorIconKind _kind;
    private readonly Color _iconColor;
    private readonly float _strokeWidth;

    public static Bitmap CreateBitmap(
        VectorIconKind kind,
        Color color,
        int size = 24,
        Color? backgroundColor = null)
    {
        using VectorIcon icon = new(kind, color, size);
        icon.BackColor = backgroundColor ?? Color.Transparent;

        Bitmap bitmap = new(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        icon.DrawToBitmap(bitmap, new Rectangle(0, 0, size, size));
        return bitmap;
    }

    public VectorIcon(VectorIconKind kind, Color color, int size = 24, float strokeWidth = 1.8f)
    {
        _kind = kind;
        _iconColor = color;
        _strokeWidth = strokeWidth;
        Size = new Size(size, size);
        TabStop = false;
        Margin = Padding.Empty;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor, true);

        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        float scale = Math.Min(ClientSize.Width, ClientSize.Height) / 24f;
        float ox = (ClientSize.Width - 24f * scale) / 2f;
        float oy = (ClientSize.Height - 24f * scale) / 2f;

        GraphicsState state = e.Graphics.Save();
        e.Graphics.TranslateTransform(ox, oy);
        e.Graphics.ScaleTransform(scale, scale);

        using Pen pen = new(_iconColor, _strokeWidth) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using SolidBrush brush = new(_iconColor);

        switch (_kind)
        {
            case VectorIconKind.Coin:
                e.Graphics.DrawEllipse(pen, 5, 5, 14, 14);
                e.Graphics.DrawEllipse(pen, 8, 8, 8, 8);
                break;

            case VectorIconKind.Sword:
                e.Graphics.DrawLine(pen, 6, 18, 18, 6);
                e.Graphics.DrawLine(pen, 5, 15, 9, 19);
                e.Graphics.DrawLine(pen, 7, 17, 11, 13);
                e.Graphics.DrawLine(pen, 16, 5, 19, 8);
                break;

            case VectorIconKind.Shield:
                using (GraphicsPath shield = new())
                {
                    shield.AddLines(new[]
                    {
                        new PointF(12, 4), new PointF(19, 7), new PointF(18, 14),
                        new PointF(12, 21), new PointF(6, 14), new PointF(5, 7), new PointF(12, 4)
                    });
                    e.Graphics.FillPath(brush, shield);
                }
                break;

            case VectorIconKind.Target:
                e.Graphics.DrawEllipse(pen, 4, 4, 16, 16);
                e.Graphics.DrawEllipse(pen, 8, 8, 8, 8);
                e.Graphics.FillEllipse(brush, 11, 11, 2, 2);
                break;

            case VectorIconKind.Heart:
                using (GraphicsPath heart = new())
                {
                    heart.StartFigure();
                    heart.AddBezier(new PointF(12, 20), new PointF(9, 17), new PointF(4, 14), new PointF(4, 9));
                    heart.AddBezier(new PointF(4, 5), new PointF(9, 4), new PointF(12, 8), new PointF(12, 8));
                    heart.AddBezier(new PointF(12, 8), new PointF(15, 4), new PointF(20, 5), new PointF(20, 9));
                    heart.AddBezier(new PointF(20, 14), new PointF(15, 17), new PointF(12, 20), new PointF(12, 20));
                    heart.CloseFigure();
                    e.Graphics.FillPath(brush, heart);
                }
                break;

            case VectorIconKind.Dice:
                e.Graphics.DrawPolygon(pen, new[]
                {
                    new PointF(6, 4), new PointF(18, 6), new PointF(20, 18),
                    new PointF(8, 20), new PointF(4, 8)
                });
                e.Graphics.FillEllipse(brush, 7, 7, 2, 2);
                e.Graphics.FillEllipse(brush, 15, 15, 2, 2);
                e.Graphics.FillEllipse(brush, 11, 11, 2, 2);
                break;

            case VectorIconKind.Recruit:
                e.Graphics.DrawEllipse(pen, 6, 4, 6, 6);
                e.Graphics.DrawEllipse(pen, 12, 4, 6, 6);
                e.Graphics.DrawArc(pen, 3, 10, 10, 10, 200, 140);
                e.Graphics.DrawArc(pen, 11, 10, 10, 10, 200, 140);
                break;

            case VectorIconKind.Relic:
                e.Graphics.DrawEllipse(pen, 4, 4, 16, 16);
                e.Graphics.DrawLine(pen, 6, 13, 17, 7);
                e.Graphics.DrawLine(pen, 7, 15, 14, 19);
                break;

            case VectorIconKind.Team:
                e.Graphics.DrawEllipse(pen, 9, 4, 6, 6);
                e.Graphics.DrawArc(pen, 5, 9, 14, 12, 200, 140);
                break;

            case VectorIconKind.Check:
                e.Graphics.DrawLines(pen, new[] { new PointF(4, 12), new PointF(9, 17), new PointF(20, 6) });
                break;

            case VectorIconKind.Potion:
                e.Graphics.DrawRectangle(pen, 9, 4, 6, 4);
                e.Graphics.DrawRectangle(pen, 6, 8, 12, 12);
                e.Graphics.DrawLine(pen, 8, 12, 16, 12);
                break;

            case VectorIconKind.Armor:
                e.Graphics.DrawPolygon(pen, new[]
                {
                    new PointF(7, 4), new PointF(17, 4), new PointF(20, 9),
                    new PointF(17, 19), new PointF(12, 21), new PointF(7, 19),
                    new PointF(4, 9), new PointF(7, 4)
                });
                break;

            case VectorIconKind.Axe:
                e.Graphics.DrawLine(pen, 7, 19, 17, 6);
                e.Graphics.DrawArc(pen, 9, 3, 10, 10, 270, 200);
                break;

            case VectorIconKind.Tome:
                e.Graphics.DrawRectangle(pen, 5, 5, 7, 14);
                e.Graphics.DrawRectangle(pen, 12, 5, 7, 14);
                e.Graphics.DrawLine(pen, 7, 9, 10, 9);
                e.Graphics.DrawLine(pen, 14, 9, 17, 9);
                break;

            case VectorIconKind.Map:
                e.Graphics.DrawPolygon(pen, new[]
                {
                    new PointF(4, 6), new PointF(9, 4), new PointF(15, 6), new PointF(20, 4),
                    new PointF(20, 18), new PointF(15, 20), new PointF(9, 18), new PointF(4, 20)
                });
                e.Graphics.DrawLine(pen, 9, 4, 9, 18);
                e.Graphics.DrawLine(pen, 15, 6, 15, 20);
                break;

            case VectorIconKind.Star:
                e.Graphics.FillPolygon(brush, CreateStarPoints());
                break;

            case VectorIconKind.Dragon:
                e.Graphics.DrawPolygon(pen, new[]
                {
                    new PointF(4, 17), new PointF(7, 11), new PointF(10, 12),
                    new PointF(12, 7), new PointF(15, 12), new PointF(20, 10),
                    new PointF(18, 16), new PointF(13, 20), new PointF(8, 20)
                });
                e.Graphics.FillEllipse(brush, 15, 12, 2, 2);
                break;

            case VectorIconKind.Eye:
                using (GraphicsPath eyePath = new())
                {
                    eyePath.AddBezier(new PointF(4, 12), new PointF(8, 6), new PointF(8, 6), new PointF(12, 6));
                    eyePath.AddBezier(new PointF(12, 6), new PointF(16, 6), new PointF(20, 12), new PointF(20, 12));
                    eyePath.AddBezier(new PointF(20, 12), new PointF(16, 18), new PointF(8, 18), new PointF(4, 12));
                    e.Graphics.DrawPath(pen, eyePath);
                }
                e.Graphics.FillEllipse(brush, 10, 10, 4, 4);
                break;

            case VectorIconKind.Sparkles:
                e.Graphics.FillPolygon(brush, new[]
                {
                    new PointF(12, 3), new PointF(13, 10), new PointF(20, 12),
                    new PointF(13, 14), new PointF(12, 21), new PointF(11, 14),
                    new PointF(4, 12), new PointF(11, 10)
                });
                break;

            case VectorIconKind.Question:
                e.Graphics.DrawArc(pen, 7, 4, 10, 8, 220, 220);
                e.Graphics.DrawLine(pen, 12, 12, 12, 16);
                e.Graphics.FillEllipse(brush, 11, 19, 2, 2);
                break;

            case VectorIconKind.Skull:
                e.Graphics.DrawEllipse(pen, 5, 4, 14, 13);
                e.Graphics.FillEllipse(brush, 8, 9, 3, 3);
                e.Graphics.FillEllipse(brush, 13, 9, 3, 3);
                e.Graphics.DrawLine(pen, 9, 16, 9, 20);
                e.Graphics.DrawLine(pen, 12, 16, 12, 20);
                e.Graphics.DrawLine(pen, 15, 16, 15, 20);
                break;

            case VectorIconKind.Damage:
                e.Graphics.FillPolygon(brush, new[]
                {
                    new PointF(12, 3), new PointF(14, 9), new PointF(20, 7),
                    new PointF(16, 12), new PointF(20, 17), new PointF(14, 15),
                    new PointF(12, 21), new PointF(10, 15), new PointF(4, 17),
                    new PointF(8, 12), new PointF(4, 7), new PointF(10, 9)
                });
                break;
        }

        e.Graphics.Restore(state);
    }

    private static PointF[] CreateStarPoints()
    {
        PointF[] points = new PointF[10];
        double step = Math.PI / 5;
        double start = -Math.PI / 2;

        for (int i = 0; i < points.Length; i++)
        {
            double radius = i % 2 == 0 ? 9 : 4;
            double angle = start + step * i;
            points[i] = new PointF(
                12 + (float)(Math.Cos(angle) * radius),
                12 + (float)(Math.Sin(angle) * radius));
        }

        return points;
    }
}
