using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.Services;
using InventoryApp.Views;
using System.Collections.ObjectModel;
using System.Linq;

namespace InventoryApp.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly Window? _window;
    private readonly UserSettingsService _settingsService;
    private readonly IAuthService _authService;
    private readonly string _username;
    
    public ObservableCollection<string> AvailableCurrencies { get; } = new()
    {
        "USD - US Dollar ($)",
        "GBP - British Pound (£)",
        "CNY - Chinese Yuan (¥)",
        "GHS - Ghanaian Cedi (GH₵)",
        "NGN - Nigerian Naira (₦)"
    };

    [ObservableProperty]
    private string _selectedCurrency;

    public SettingsViewModel(Window? window, UserSettingsService settingsService, string username, IAuthService authService = null)
    {
        _window = window;
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _authService = authService ?? App.Resolver.Resolve<IAuthService>();
        _username = username ?? throw new ArgumentNullException(nameof(username));
        _selectedCurrency = AvailableCurrencies.FirstOrDefault(c => 
            c.StartsWith(_settingsService.CurrentSettings.Currency)) ?? AvailableCurrencies[0];
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        var currencyCode = SelectedCurrency.Split(' ')[0];
        _settingsService.UpdateCurrency(currencyCode);
        
        // Show success message with close button
        await ShowInfo("Settings saved successfully");
    }
    
    private async Task ShowError(string message)
    {
        var dialog = new InfoDialog();
        await dialog.ShowAsync(_window, message);
    }
    
    private async Task ShowInfo(string message)
    {
        var dialog = new InfoDialog();
        await dialog.ShowAsync(_window, message);
    }
    
    [RelayCommand]
    private async Task DeleteAccount()
    {
        if (string.IsNullOrEmpty(_username))
        {
            await ShowError("You are not logged in. Please log in and try again.");
            return;
        }

        // Verify the user still exists in the database
        using var db = new AppDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == _username);
        if (user == null)
        {
            await ShowError("User account not found. You may have been logged out.");
            // Try to get the main window to logout
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel mainVm)
            {
                mainVm.Logout();
            }
            return;
        }

        // Create password input dialog
        var passwordBox = new TextBox 
        { 
            Watermark = "Enter your password to confirm",
            PasswordChar = '•',
            Margin = new Thickness(0, 10, 0, 10)
        };

        // Declare the dialog variable before creating the window
        Window? deleteAccountDialog = null;
        
        // Create a local function to close the dialog
        void CloseDialog() => deleteAccountDialog?.Close();

        // Create the window
        deleteAccountDialog = new Window
        {
            Title = "Delete Account",
            Width = 400,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Children =
                {
                    new TextBlock 
                    { 
                        Text = "This will permanently delete your account and all associated data.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 10)
                    },
                    new TextBlock 
                    { 
                        Text = "To confirm, please enter your password:",
                        Margin = new Thickness(0, 0, 0, 5)
                    },
                    passwordBox,
                    new TextBlock 
                    { 
                        Text = "WARNING: This action cannot be undone!",
                        Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Red),
                        Margin = new Thickness(0, 10, 0, 0)
                    },
                    new StackPanel 
                    { 
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 10,
                        Margin = new Thickness(0, 20, 0, 0),
                        Children =
                        {
                            new Button { 
                                Content = "Cancel",
                                Command = new RelayCommand(CloseDialog)
                            },
                            new Button { 
                                Content = "Delete My Account", 
                                Classes = { "danger" },
                                Command = new RelayCommand(async () => 
                                {
                                    var password = passwordBox.Text;
                                    if (string.IsNullOrWhiteSpace(password))
                                    {
                                        await ShowError("Please enter your password");
                                        return;
                                    }

                                    using var db = new AppDbContext();
                                    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == _username);
                                    if (user == null)
                                    {
                                        await ShowError("User not found");
                                        return;
                                    }

                                    // Verify password
                                    if (_authService == null)
                                    {
                                        await ShowError("Authentication service is not available. Please restart the application.");
                                        return;
                                    }

                                    try
                                    {
                                        var (ok, error, _) = await _authService.LoginAsync(_username, password);
                                        if (!ok)
                                        {
                                            await ShowError(error ?? "Incorrect password. Please try again.");
                                            return;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await ShowError($"An error occurred during authentication: {ex.Message}");
                                        return;
                                    }

                                    // Show final confirmation
                                    var confirmDialog = new ConfirmDialog();
                                    var confirm = await confirmDialog.ShowAsync(deleteAccountDialog, 
                                        "Are you absolutely sure? This will permanently delete your account and all data. This cannot be undone!");
                                    
                                    if (!confirm) return;

                                    // Get the main window and its view model before showing the dialog
                                    var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                                    if (mainWindow?.DataContext is not MainWindowViewModel mainVm)
                                    {
                                        await ShowError("Could not access application state. Please try again.");
                                        return;
                                    }

                                    // Perform the deletion in a background task to keep the UI responsive
                                    await Task.Run(async () => {
                                        using var transaction = await db.Database.BeginTransactionAsync();
                                        try
                                        {
                                            // Load and delete sales
                                            var sales = await db.Sales.ToListAsync();
                                            db.Sales.RemoveRange(sales);
                                            
                                            // Load and delete products
                                            var products = await db.Products.ToListAsync();
                                            db.Products.RemoveRange(products);
                                            
                                            // Delete user
                                            db.Users.Remove(user);
                                            
                                            await db.SaveChangesAsync();
                                            await transaction.CommitAsync();
                                            
                                            // Switch back to the UI thread before showing messages or closing windows
                                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                                            {
                                                // Show success message
                                                await ShowInfo("Your account and all data have been permanently deleted.");
                                                
                                                // Close the dialog and trigger logout
                                                CloseDialog();
                                                mainVm.Logout();
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            try { await transaction.RollbackAsync(); } catch { }
                                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                                            {
                                                await ShowError($"An error occurred while deleting your account: {ex.Message}");
                                            });
                                        }
                                    });
                                })
                            }
                        }
                    }
                }
            }
        };

        if (_window != null)
        {
            await deleteAccountDialog.ShowDialog(_window);
        }
    }
}
