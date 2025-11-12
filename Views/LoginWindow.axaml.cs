using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using InventoryApp.ViewModels;
using InventoryApp.Services;
using System;
using System.Threading.Tasks;

namespace InventoryApp.Views;

public partial class LoginWindow : Window
{
    private readonly IAuthService _auth;

    public LoginWindow() : this(new AuthService()) {}

    public LoginWindow(IAuthService auth)
    {
        _auth = auth;
        InitializeComponent();

        var vm = new LoginViewModel(_auth);
        vm.LoginSucceeded += OnLoginSucceeded;
        vm.ShowRegisterRequested += OnShowRegisterRequested;
        vm.ShowForgotPasswordRequested += OnShowForgotPasswordRequested;
        DataContext = vm;

        // Password is bound via TextBox with PasswordChar in XAML
    }

    private async void OnShowRegisterRequested()
    {
        var regWin = new RegisterWindow(_auth);
        var result = await regWin.ShowDialog<bool?>(this);
        if (DataContext is LoginViewModel vm && result == true)
        {
            vm.Error = "Account created successfully. Please login.";
            vm.IsSuccessMessage = true;
        }
    }

    private void OnLoginSucceeded(string username)
    {
        var main = new MainWindow();
        
        // Resolve required services
        var settingsService = App.Resolver.Resolve<UserSettingsService>();
        var authService = App.Resolver.Resolve<IAuthService>();
        
        // Create main view model with required services
        var mainVm = new MainWindowViewModel(username, main);
        main.DataContext = mainVm;
        
        // Set the window reference after creation
        mainVm.SetWindow(main);

        // Set the main window in the application lifetime
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = main;
        }

        mainVm.LogoutRequested += () =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                var login = new LoginWindow(_auth);
                desktopLifetime.MainWindow = login;
                login.Show();
                main.Close();
            }
        };

        main.Show();
        Close();
    }

    private async void OnShowForgotPasswordRequested()
    {
        if (DataContext is not LoginViewModel vm) return;
        vm.Error = string.Empty;

        var username = vm.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            vm.Error = "Enter your username first";
            return;
        }

        // Step 1: ask for master PIN
        var pin = await ShowVerifyPinDialogAsync();
        if (pin is null) return; // canceled

        var (okPin, errPin) = await _auth.VerifyMasterPinAsync(username, pin);
        if (!okPin)
        {
            vm.Error = errPin ?? "Invalid PIN";
            return;
        }

        // Step 2: ask for new password
        var newPwd = await ShowResetPasswordDialogAsync();
        if (newPwd is null) return; // canceled

        var (okReset, errReset) = await _auth.ResetPasswordAsync(username, newPwd);
        if (!okReset)
        {
            vm.Error = errReset ?? "Failed to reset password";
            return;
        }

        vm.Error = "Password reset successful. Please login.";
    }

    private async Task<string?> ShowVerifyPinDialogAsync()
    {
        var dlg = new VerifyPinWindow();
        string? result = null;
        var tcs = new TaskCompletionSource<string?>();

        if (dlg.DataContext is VerifyPinViewModel mvm)
        {
            mvm.PinSubmitted += pin => { result = pin; tcs.TrySetResult(result); dlg.Close(); };
            mvm.Canceled += () => { result = null; tcs.TrySetResult(result); dlg.Close(); };
        }

        await dlg.ShowDialog(this);
        return await tcs.Task;
    }

    private async Task<string?> ShowResetPasswordDialogAsync()
    {
        var dlg = new ResetPasswordWindow();
        string? result = null;
        var tcs = new TaskCompletionSource<string?>();

        if (dlg.DataContext is ResetPasswordViewModel rvm)
        {
            rvm.SubmitRequested += pwd => { result = pwd; tcs.TrySetResult(result); dlg.Close(); };
            rvm.Canceled += () => { result = null; tcs.TrySetResult(result); dlg.Close(); };
        }

        await dlg.ShowDialog(this);
        return await tcs.Task;
    }
}
