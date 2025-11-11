using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace InventoryApp.ViewModels;

public partial class MasterPinViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pin = string.Empty;

    [ObservableProperty]
    private string _confirmPin = string.Empty;

    [ObservableProperty]
    private string _error = string.Empty;

    public event Action<string>? PinSubmitted;
    public event Action? Canceled;

    [RelayCommand]
    private void Ok()
    {
        Error = string.Empty;
        if (string.IsNullOrWhiteSpace(Pin) || Pin.Length < 4 || Pin.Length > 8 || !Pin.All(char.IsDigit))
        {
            Error = "PIN must be 4-8 digits";
            return;
        }
        if (Pin != ConfirmPin)
        {
            Error = "PINs do not match";
            return;
        }
        PinSubmitted?.Invoke(Pin);
    }

    [RelayCommand]
    private void Cancel()
    {
        Canceled?.Invoke();
    }
}
