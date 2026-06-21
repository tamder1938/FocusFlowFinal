using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FocusFlowFinal.Models.Media;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Views.Controls;

public class MediaDonutControl : Control
{
    public static readonly StyledProperty<List<MediaTypeCount>?> DataProperty =
        AvaloniaProperty.Register<MediaDonutControl, List<MediaTypeCount>?>(nameof(Data));

    public List<MediaTypeCount>? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    static MediaDonutControl()
    {
        AffectsRender<MediaDonutControl>(DataProperty);
    }

    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);

        var items = Data;
        if (items == null || items.Count == 0) return;

        int total = 0;
        foreach (var it in items) total += it.Count;
        if (total == 0) return;

        double cx = Bounds.Width  / 2;
        double cy = Bounds.Height / 2;
        double r  = Math.Min(cx, cy) - 4;
        double ri = r * 0.52;

        double angle = -Math.PI / 2;

        foreach (var it in items)
        {
            if (it.Count == 0) continue;
            double sweep = 2 * Math.PI * it.Count / total;
            Color  color = Color.Parse(it.Color);
            DrawArc(ctx, cx, cy, r, ri, angle, sweep, new SolidColorBrush(color));
            angle += sweep;
        }

        // Центр — общее число
        var ft = new FormattedText(
            total.ToString(),
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            15, Brushes.Gray);
        ctx.DrawText(ft, new Point(cx - ft.Width / 2, cy - ft.Height / 2));
    }

    private static void DrawArc(DrawingContext ctx, double cx, double cy,
        double r, double ri, double startAngle, double sweepAngle, IBrush brush)
    {
        if (sweepAngle < 0.001) return;

        var geo = new StreamGeometry();
        using var gc = geo.Open();

        var p1 = Pt(cx, cy, r,  startAngle);
        var p2 = Pt(cx, cy, r,  startAngle + sweepAngle);
        var p3 = Pt(cx, cy, ri, startAngle + sweepAngle);
        var p4 = Pt(cx, cy, ri, startAngle);

        bool large = sweepAngle > Math.PI;

        gc.BeginFigure(p1, true);
        gc.ArcTo(p2, new Size(r, r),  0, large, SweepDirection.Clockwise);
        gc.LineTo(p3);
        gc.ArcTo(p4, new Size(ri, ri), 0, large, SweepDirection.CounterClockwise);
        gc.EndFigure(true);

        ctx.DrawGeometry(brush, null, geo);
    }

    private static Point Pt(double cx, double cy, double r, double a)
        => new(cx + r * Math.Cos(a), cy + r * Math.Sin(a));
}
