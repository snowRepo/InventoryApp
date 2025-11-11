using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using InventoryApp.ViewModels;
using System;

namespace InventoryApp.Views
{
    public partial class DeviceInfoPopup : Window
    {
        public DeviceInfoPopup()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public DeviceInfoPopup(Window owner, string title, string deviceInfo) : this()
        {
            // Create and set the view model
            var viewModel = new DeviceInfoPopupViewModel
            {
                Title = title,
                DeviceInfo = deviceInfo,
                CloseAction = () => this.Close()
            };
            
            this.DataContext = viewModel;
            
            // Set the owner and show the window
            if (owner != null && owner.IsActive)
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                this.Show(owner);
            }
            else
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                this.Show();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
