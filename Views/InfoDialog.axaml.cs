using System;
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
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    public Task ShowAsync(Window owner, string message)
    {
        _tcs = new TaskCompletionSource<bool>();
        this.FindControl<TextBlock>("MessageText")!.Text = message;
        
        // Show the dialog as a modal dialog
        this.ShowDialog(owner);
        
        return _tcs.Task;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        _tcs?.TrySetResult(true);
        Close();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _tcs?.TrySetResult(true); // Ensure we always complete the task when closed
    }
}
