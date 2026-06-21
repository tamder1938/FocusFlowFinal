using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Views.Controls;

// ── Точка на линейном графике ─────────────────────────────────────────
public record MoodChartPoint(DateTime Date, int Level);

// ── Линейный график настроения ─────────────────────────────────────────
public class MoodLineChartControl : Control
{
    private static readonly string[] _colors = ["#DC4F4F", "#E08A3C", "#8AB7D9", "#A8D88A", "#5FB87A"];

    public static readonly StyledProperty<IList<MoodChartPoint>?> PointsProperty =
        AvaloniaProperty.Register<MoodLineChartControl, IList<MoodChartPoint>?>(nameof(Points));

    public IList<MoodChartPoint>? Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    static MoodLineChartControl() => AffectsRender<MoodLineChartControl>(PointsProperty);

    public override void Render(DrawingContext ctx)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w < 20 || h < 20) return;

        const double padL = 30, padR = 12, padT = 10, padB = 28;
        double cw = w - padL - padR;
        double ch = h - padT - padB;

        // Фоновые линии сетки (уровни 1–5)
        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#E5E7EB")), 0.7);
        for (int lvl = 1; lvl <= 5; lvl++)
        {
            double y = padT + ch - (lvl - 1) * ch / 4.0;
            ctx.DrawLine(gridPen, new Point(padL, y), new Point(w - padR, y));
            // Метка уровня
            var ft = new FormattedText(lvl.ToString(), System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, Typeface.Default, 9,
                new SolidColorBrush(Color.Parse("#9CA3AF")));
            ctx.DrawText(ft, new Point(2, y - ft.Height / 2));
        }

        var pts = Points;
        if (pts == null || pts.Count == 0) return;

        // Вычисляем экранные координаты
        var sorted = pts.OrderBy(p => p.Date).ToList();
        var minDate = sorted[0].Date;
        var maxDate = sorted[^1].Date;
        double rangeD = Math.Max((maxDate - minDate).TotalDays, 1);

        var screen = sorted.Select(p =>
        {
            double x = padL + (p.Date - minDate).TotalDays / rangeD * cw;
            double y = padT + ch - (p.Level - 1) * ch / 4.0;
            return (Pt: new Point(x, y), p.Level);
        }).ToList();

        // Линия
        if (screen.Count >= 2)
        {
            var geo = new StreamGeometry();
            using (var gc = geo.Open())
            {
                gc.BeginFigure(screen[0].Pt, false);
                for (int i = 1; i < screen.Count; i++) gc.LineTo(screen[i].Pt);
                gc.EndFigure(false);
            }
            ctx.DrawGeometry(null, new Pen(new SolidColorBrush(Color.Parse("#3B82F6")), 2), geo);
        }

        // Точки (цветные кружки)
        foreach (var (pt, lvl) in screen)
        {
            var brush = new SolidColorBrush(Color.Parse(_colors[lvl - 1]));
            ctx.DrawEllipse(brush, null, pt, 5, 5);
            ctx.DrawEllipse(null, new Pen(Brushes.White, 1.5), pt, 5, 5);
        }

        // Метки дат по оси X
        if (sorted.Count >= 2)
        {
            int step = Math.Max(1, sorted.Count / 5);
            for (int i = 0; i < sorted.Count; i += step)
            {
                var (pt, _) = screen[i];
                var label = sorted[i].Date.ToString("dd.MM");
                var ft = new FormattedText(label, System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, Typeface.Default, 9,
                    new SolidColorBrush(Color.Parse("#9CA3AF")));
                ctx.DrawText(ft, new Point(pt.X - ft.Width / 2, h - padB + 4));
            }
        }
    }
}

// ── Донат-чарт распределения настроений ───────────────────────────────
public class MoodDonutControl : Control
{
    public static readonly StyledProperty<IList<MoodDistributionItem>?> ItemsProperty =
        AvaloniaProperty.Register<MoodDonutControl, IList<MoodDistributionItem>?>(nameof(Items));

    public IList<MoodDistributionItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    static MoodDonutControl() => AffectsRender<MoodDonutControl>(ItemsProperty);

    private static Point AnglePt(Point center, double r, double deg)
    {
        double rad = (deg - 90) * Math.PI / 180;
        return new Point(center.X + r * Math.Cos(rad), center.Y + r * Math.Sin(rad));
    }

    public override void Render(DrawingContext ctx)
    {
        var items = Items;
        if (items == null) return;

        double w = Bounds.Width, h = Bounds.Height;
        double r = Math.Min(w, h) / 2.0 - 6;
        if (r < 10) return;
        double ri = r * 0.52;
        var center = new Point(w / 2, h / 2);

        int total = items.Sum(i => i.Count);
        if (total == 0)
        {
            // Серый кружок если нет данных
            ctx.DrawEllipse(new SolidColorBrush(Color.Parse("#E5E7EB")), null, center, r, r);
            ctx.DrawEllipse(new SolidColorBrush(Color.Parse("#F9FAFB")), null, center, ri, ri);
            return;
        }

        double startAngle = 0;
        foreach (var item in items.Where(i => i.Count > 0))
        {
            double sweep = item.Percentage / 100.0 * 360.0;
            double endAngle = startAngle + sweep;
            bool large = sweep > 180;

            var outerStart = AnglePt(center, r, startAngle);
            var outerEnd   = AnglePt(center, r, endAngle);
            var innerEnd   = AnglePt(center, ri, endAngle);
            var innerStart = AnglePt(center, ri, startAngle);

            var geo = new StreamGeometry();
            using (var gc = geo.Open())
            {
                gc.BeginFigure(outerStart, true);
                gc.ArcTo(outerEnd, new Size(r, r), 0, large, SweepDirection.Clockwise);
                gc.LineTo(innerEnd);
                gc.ArcTo(innerStart, new Size(ri, ri), 0, large, SweepDirection.CounterClockwise);
                gc.EndFigure(true);
            }
            ctx.DrawGeometry(new SolidColorBrush(Color.Parse(item.Color)), null, geo);
            startAngle = endAngle;
        }

        // Дырка (белый круг в центре)
        var bg = Application.Current?.Resources.TryGetResource("CardBackgroundBrush", null, out var res) == true
            ? res as IBrush : null;
        ctx.DrawEllipse(bg ?? Brushes.White, null, center, ri - 1, ri - 1);

        // Число в центре
        var ft = new FormattedText(total.ToString(),
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI", weight: FontWeight.Bold), 18,
            new SolidColorBrush(Color.Parse("#111827")));
        ctx.DrawText(ft, new Point(center.X - ft.Width / 2, center.Y - ft.Height / 2));
    }
}
