using Avalonia.Controls;
using InventoryApp.ViewModels;

namespace InventoryApp.Views;

public partial class ResetPasswordWindow : Window
{
    public ResetPasswordWindow()
    {
        InitializeComponent();
        DataContext = new ResetPasswordViewModel();
    }
}
