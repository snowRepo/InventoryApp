using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace InventoryApp.Views;

public partial class InfoDialog : Window
{
    private TaskCompletionSource<bool>? _tcs;

    public InfoDialog()
    {
        InitializeComponent();
    }

    public Task ShowAsync(Window owner, string message)
    {
        _tcs = new TaskCompletionSource<bool>();
        this.FindControl<TextBlock>("MessageText")!.Text = message;
        _ = this.ShowDialog(owner);
        return _tcs.Task;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        _tcs?.TrySetResult(true);
        Close();
    }
}
