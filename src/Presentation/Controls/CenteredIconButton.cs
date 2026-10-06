namespace RPGBattleMaker.Presentation.Controls;

public sealed class CenteredIconButton : Button
{
    internal VectorIconKind IconKind = VectorIconKind.Coin;
    internal Color IconColor = Color.White;
    internal int IconSize = 18;
    internal int IconTextGap = 6;

    public CenteredIconButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        TextAlign = ContentAlignment.MiddleCenter;
        TabStop = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using SolidBrush backgroundBrush = new(BackColor);
        e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);

        if (Enabled)
        {
            using Bitmap icon = VectorIcon.CreateBitmap(
                IconKind,
                IconColor,
                IconSize,
                BackColor);

            Size textSize = TextRenderer.MeasureText(
                e.Graphics,
                Text,
                Font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding);

            int groupWidth = icon.Width + IconTextGap + textSize.Width;
            int startX = Math.Max(0, (ClientSize.Width - groupWidth) / 2);
            int iconY = Math.Max(0, (ClientSize.Height - icon.Height) / 2);
            int textX = startX + icon.Width + IconTextGap;
            int textY = Math.Max(0, (ClientSize.Height - textSize.Height) / 2);

            e.Graphics.DrawImageUnscaled(icon, startX, iconY);
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                new Point(textX, textY),
                ForeColor,
                TextFormatFlags.NoPadding);
        }
        else
        {
            using Bitmap icon = VectorIcon.CreateBitmap(
                IconKind,
                Color.FromArgb(150, IconColor),
                IconSize,
                BackColor);

            Size textSize = TextRenderer.MeasureText(
                e.Graphics,
                Text,
                Font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding);

            int groupWidth = icon.Width + IconTextGap + textSize.Width;
            int startX = Math.Max(0, (ClientSize.Width - groupWidth) / 2);
            int iconY = Math.Max(0, (ClientSize.Height - icon.Height) / 2);
            int textX = startX + icon.Width + IconTextGap;
            int textY = Math.Max(0, (ClientSize.Height - textSize.Height) / 2);

            e.Graphics.DrawImageUnscaled(icon, startX, iconY);
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                new Point(textX, textY),
                Color.FromArgb(160, ForeColor),
                TextFormatFlags.NoPadding);
        }
    }
}
