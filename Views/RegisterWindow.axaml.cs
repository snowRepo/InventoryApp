using Avalonia.Controls;
using InventoryApp.ViewModels;
using InventoryApp.Services;
using System;
using System.Threading.Tasks;

namespace InventoryApp.Views;

public partial class RegisterWindow : Window
{
    private readonly IAuthService _auth;

    public RegisterWindow() : this(new AuthService()) {}

    public RegisterWindow(IAuthService auth)
    {
        _auth = auth;
        InitializeComponent();

        var vm = new RegisterViewModel(_auth);
        vm.RequestMasterPin += OnRequestMasterPinAsync;
        vm.RegistrationCompleted += OnRegistrationCompleted;
        vm.RegistrationCanceled += OnRegistrationCanceled;
        DataContext = vm;
    }

    private async Task<string?> OnRequestMasterPinAsync()
    {
        var dlg = new MasterPinWindow();
        string? result = null;
        var tcs = new TaskCompletionSource<string?>();

        if (dlg.DataContext is MasterPinViewModel mvm)
        {
            mvm.PinSubmitted += pin => { result = pin; tcs.TrySetResult(result); dlg.Close(); };
            mvm.Canceled += () => { result = null; tcs.TrySetResult(result); dlg.Close(); };
        }

        await dlg.ShowDialog(this);
        return await tcs.Task;
    }

    private void OnRegistrationCompleted()
    {
        Close(true);
    }

    private void OnRegistrationCanceled()
    {
        Close(false);
    }
}
