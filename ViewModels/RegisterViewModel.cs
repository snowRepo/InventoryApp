using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryApp.Services;
using System.Threading.Tasks;
using System;

namespace InventoryApp.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly UserSettingsService _settingsService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _error = string.Empty;

    public event Func<Task<string?>>? RequestMasterPin; // returns pin or null if canceled
    public event Action? RegistrationCompleted; // success -> back to login
    public event Action? RegistrationCanceled; // canceled -> back to login

    public RegisterViewModel(IAuthService auth, UserSettingsService settingsService = null)
    {
        _auth = auth;
        _settingsService = settingsService ?? App.Resolver.Resolve<UserSettingsService>();
    }

    [RelayCommand]
    private async Task Register()
    {
        Error = string.Empty;
        if (string.IsNullOrWhiteSpace(Username)) { Error = "Username is required"; return; }
        if (string.IsNullOrEmpty(Password) || Password.Length < 6) { Error = "Password must be at least 6 characters"; return; }
        if (Password != ConfirmPassword) { Error = "Passwords do not match"; return; }

        if (RequestMasterPin is null)
        {
            Error = "Internal error: no PIN dialog";
            return;
        }
        var pin = await RequestMasterPin.Invoke();
        if (pin is null) return; // canceled

        var (ok, err) = await _auth.RegisterAsync(Username, Password, pin);
        if (!ok)
        {
            Error = err ?? "Registration failed";
            return;
        }
        // Reset currency settings to default for new account
        _settingsService.UpdateCurrency("USD");
        RegistrationCompleted?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        RegistrationCanceled?.Invoke();
    }
}
