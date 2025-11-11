using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace InventoryApp.Views;

public partial class ConfirmDialog : Window
{
    private TaskCompletionSource<bool>? _tcs;

    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public Task<bool> ShowAsync(Window owner, string message)
    {
        _tcs = new TaskCompletionSource<bool>();
        this.FindControl<TextBlock>("MessageText")!.Text = message;
        _ = this.ShowDialog(owner);
        return _tcs.Task;
    }

    private void OnYes(object? sender, RoutedEventArgs e)
    {
        _tcs?.TrySetResult(true);
        Close();
    }

    private void OnNo(object? sender, RoutedEventArgs e)
    {
        _tcs?.TrySetResult(false);
        Close();
    }
}
