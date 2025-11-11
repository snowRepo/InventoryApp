using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace InventoryApp.ViewModels
{
    public class DeviceInfoPopupViewModel : ViewModelBase
    {
        public string Title { get; set; }
        public string DeviceInfo { get; set; }
        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }
        public DeviceInfoPopupViewModel()
        {
            CloseCommand = new RelayCommand(() => CloseAction?.Invoke());
        }
    }
}
