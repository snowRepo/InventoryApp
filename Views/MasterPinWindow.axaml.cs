using Avalonia.Controls;
using InventoryApp.ViewModels;

namespace InventoryApp.Views;

public partial class MasterPinWindow : Window
{
    public MasterPinWindow()
    {
        InitializeComponent();
        DataContext = new MasterPinViewModel();
    }
}
