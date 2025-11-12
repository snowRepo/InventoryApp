using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryApp.Services;
using System.Threading.Tasks;
using System;

namespace InventoryApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _error = string.Empty;

    [ObservableProperty]
    private bool _isSuccessMessage = false;

    public event Action<string>? LoginSucceeded;
    public event Action? ShowRegisterRequested;
    public event Action? ShowForgotPasswordRequested;

    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;
    }

    [RelayCommand]
    private async Task Login()
    {
        Error = string.Empty;
        var (ok, err, user) = await _auth.LoginAsync(Username, Password);
        if (!ok)
        {
            Error = err ?? "Login failed";
            return;
        }
        LoginSucceeded?.Invoke(Username);
    }

    [RelayCommand]
    private void NavigateToRegister()
    {
        Error = string.Empty;
        IsSuccessMessage = false;
        ShowRegisterRequested?.Invoke();
    }

    [RelayCommand]
    private void ForgotPassword()
    {
        Error = string.Empty;
        IsSuccessMessage = false;
        ShowForgotPasswordRequested?.Invoke();
    }
}
