using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace InventoryApp.ViewModels;

public partial class ResetPasswordViewModel : ObservableObject
{
    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmNewPassword = string.Empty;

    [ObservableProperty]
    private string _error = string.Empty;

    public event Action<string>? SubmitRequested;
    public event Action? Canceled;

    [RelayCommand]
    private void Submit()
    {
        Error = string.Empty;
        if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 6)
        {
            Error = "Password must be at least 6 characters";
            return;
        }
        if (NewPassword != ConfirmNewPassword)
        {
            Error = "Passwords do not match";
            return;
        }
        SubmitRequested?.Invoke(NewPassword);
    }

    [RelayCommand]
    private void Cancel()
    {
        Canceled?.Invoke();
    }
}
