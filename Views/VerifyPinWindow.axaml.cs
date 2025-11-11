using Avalonia.Controls;
using InventoryApp.ViewModels;

namespace InventoryApp.Views;

public partial class VerifyPinWindow : Window
{
    public VerifyPinWindow()
    {
        InitializeComponent();
        DataContext = new VerifyPinViewModel();
    }
}
