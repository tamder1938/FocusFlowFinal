using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;

namespace FocusFlowFinal.Views;

public class DeleteModeWindow : Window
{
    public string DeleteResult { get; private set; } = "Cancel";

    public DeleteModeWindow()
    {
        Title = "Удаление повторения";
        Width = 360;
        Height = 270; // ИСПРАВЛЕНИЕ: Увеличена высота, чтобы влезли все элементы
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Background = Brush.Parse("#F9FAFB");

        var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 10 };

        var textBlock = new TextBlock
        {
            Text = "Это повторяющееся событие. Выберите режим удаления:",
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeight.Bold,
            FontSize = 13,
            Foreground = Brush.Parse("#111827"),
            Margin = new Avalonia.Thickness(0, 0, 0, 6)
        };

        var btnOnlyThis = new Button { Content = "Удалить только этот день", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, Padding = new Avalonia.Thickness(10), Background = Brushes.White, BorderBrush = Brush.Parse("#D1D5DB"), BorderThickness = new Avalonia.Thickness(1) };
        btnOnlyThis.Click += (s, e) => { DeleteResult = "OnlyThis"; Close(); };

        var btnCustom = new Button { Content = "Выбрать конкретные дни серии...", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, Padding = new Avalonia.Thickness(10), Background = Brushes.White, BorderBrush = Brush.Parse("#D1D5DB"), BorderThickness = new Avalonia.Thickness(1) };
        btnCustom.Click += (s, e) => { DeleteResult = "Custom"; Close(); };

        // Делаем кнопку удаления серии красной для наглядности
        var btnAll = new Button { Content = "Удалить всю серию полностью", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, Padding = new Avalonia.Thickness(10), Background = Brush.Parse("#FEE2E2"), Foreground = Brush.Parse("#B91C1C"), FontWeight = FontWeight.SemiBold };
        btnAll.Click += (s, e) => { DeleteResult = "All"; Close(); };

        var btnCancel = new Button { Content = "Отмена", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, Padding = new Avalonia.Thickness(8), Background = Brush.Parse("#E5E7EB") };
        btnCancel.Click += (s, e) => { DeleteResult = "Cancel"; Close(); };

        stackPanel.Children.Add(textBlock);
        stackPanel.Children.Add(btnOnlyThis);
        stackPanel.Children.Add(btnCustom);
        stackPanel.Children.Add(btnAll);
        stackPanel.Children.Add(btnCancel);

        Content = stackPanel;
    }
}
