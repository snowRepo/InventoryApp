using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace InventoryApp.ViewModels;

public partial class VerifyPinViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pin = string.Empty;

    [ObservableProperty]
    private string _error = string.Empty;

    public event Action<string>? PinSubmitted;
    public event Action? Canceled;

    [RelayCommand]
    private void Ok()
    {
        Error = string.Empty;
        if (string.IsNullOrWhiteSpace(Pin))
        {
            Error = "PIN is required";
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
