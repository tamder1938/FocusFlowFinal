using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class PlaceFormDialog : Window
{
    public PlaceFormDialog()
    {
        AvaloniaXamlLoader.Load(this);
        var list = this.FindControl<ListBox>("SuggestionsList");
        if (list != null)
            list.SelectionChanged += (_, e) =>
            {
                if (list.SelectedItem is string suggestion && DataContext is PlaceFormViewModel vm)
                {
                    vm.SelectSuggestionCommand.Execute(suggestion);
                    list.SelectedIndex = -1;
                }
            };
    }
}
