using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryApp.Services;
using InventoryApp.Views;
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace InventoryApp.ViewModels;

public partial class ExternalSurfacesViewModel : ViewModelBase, IDisposable
{
    private readonly IDeviceService _deviceService;
    private bool _disposed;

    [ObservableProperty]
    private bool _isDeviceAvailable;

    [ObservableProperty]
    private string _deviceStatusText = "Checking device status...";

    [ObservableProperty]
    private string _consoleOutput = "Device console initialized. Ready to connect to external surfaces...\n";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _deviceName = "No device";

    public IRelayCommand CheckDeviceStatusCommand { get; }
    public IRelayCommand ScanForDevicesCommand { get; }
    public IRelayCommand ConnectDeviceCommand { get; }
    public IRelayCommand DisconnectDeviceCommand { get; }
    public IRelayCommand GetDeviceInfoCommand { get; }

    public ExternalSurfacesViewModel(IDeviceService deviceService)
    {
        _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
        _deviceService.StatusChanged += OnDeviceStatusChanged;

        // Initialize commands
        CheckDeviceStatusCommand = new RelayCommand(async () => await CheckDeviceStatusAsync());
        ScanForDevicesCommand = new RelayCommand(async () => await ScanForDevicesAsync());
        ConnectDeviceCommand = new RelayCommand(async () => await ConnectToDeviceAsync(), () => !IsBusy && !IsDeviceAvailable);
        DisconnectDeviceCommand = new RelayCommand(async () => await DisconnectDeviceAsync(), () => !IsBusy && IsDeviceAvailable);
        GetDeviceInfoCommand = new RelayCommand(async () => await GetDeviceInfoAsync(), () => !IsBusy && IsDeviceAvailable);

        // Initial device check
        CheckDeviceStatusCommand.Execute(null);
        
        // Update command states when busy status changes
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IsBusy) || e.PropertyName == nameof(IsDeviceAvailable))
            {
                ConnectDeviceCommand.NotifyCanExecuteChanged();
                DisconnectDeviceCommand.NotifyCanExecuteChanged();
                GetDeviceInfoCommand.NotifyCanExecuteChanged();
            }
        };
    }

    private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
    {
        IsDeviceAvailable = e.IsConnected;
        DeviceStatusText = e.StatusMessage;
        DeviceName = e.IsConnected ? _deviceService.DeviceName : "No device";
        
        if (e.IsConnected)
        {
            AddToConsole($"Device connected: {_deviceService.DeviceName}");
        }
        else
        {
            AddToConsole("Device disconnected");
        }
    }

    private async Task CheckDeviceStatusAsync()
    {
        if (IsBusy) return;
        
        try
        {
            IsBusy = true;
            await _deviceService.CheckStatusOnlyAsync();
            IsDeviceAvailable = _deviceService.IsConnected;
            DeviceName = _deviceService.IsConnected ? _deviceService.DeviceName : "No device";
            AddToConsole("Device status checked.");
        }
        catch (Exception ex)
        {
            AddToConsole($"Error checking device status: {ex.Message}", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ScanForDevicesAsync()
    {
        if (IsBusy) return;
        
        try
        {
            IsBusy = true;
            AddToConsole("Scanning for devices...");
            
            // Check if the mock device is available (without connecting)
            bool isAvailable = await _deviceService.CheckStatusOnlyAsync();
            
            if (isAvailable)
            {
                // Update device name but don't set IsDeviceAvailable to true yet
                DeviceName = "Mock Device (Available)";
                AddToConsole("✓ Mock device found and ready for connection");
                AddToConsole("   Click 'Connect' to establish a connection");
                
                // Enable the Connect button by setting IsDeviceAvailable to false (not connected yet)
                IsDeviceAvailable = false;
            }
            else
            {
                // Reset the UI if no device is found
                DeviceName = "No device found";
                IsDeviceAvailable = false;
                AddToConsole("✗ No devices found. Please try again.");
            }
        }
        catch (Exception ex)
        {
            AddToConsole($"Error scanning for devices: {ex.Message}", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ConnectToDeviceAsync()
    {
        if (IsBusy || _deviceService.IsConnected) return;
        
        try
        {
            IsBusy = true;
            AddToConsole("Connecting to device...");
            
            // Try to connect to the device
            bool connected = await _deviceService.ConnectAsync();
            
            if (connected)
            {
                // Only update UI if connection was successful
                IsDeviceAvailable = true;
                DeviceName = _deviceService.DeviceName;
                AddToConsole($"✓ Successfully connected to: {_deviceService.DeviceName}");
            }
            else
            {
                AddToConsole("Failed to connect to the device. Please try again.", true);
            }
        }
        catch (Exception ex)
        {
            AddToConsole($"Error connecting to device: {ex.Message}", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DisconnectDeviceAsync()
    {
        if (IsBusy || !IsDeviceAvailable) return;
        
        try
        {
            IsBusy = true;
            AddToConsole("Disconnecting device...");
            await _deviceService.DisconnectAsync();
            IsDeviceAvailable = false;
            AddToConsole("Device disconnected successfully.");
        }
        catch (Exception ex)
        {
            AddToConsole($"Error disconnecting device: {ex.Message}", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task GetDeviceInfoAsync()
    {
        if (IsBusy) return;
        
        try
        {
            // Check if the device is actually connected
            if (!_deviceService.IsConnected)
            {
                AddToConsole("Error: Not connected to any device. Please connect first.", true);
                return;
            }
            
            IsBusy = true;
            AddToConsole("Retrieving device information...");
            
            // Get device information
            string info = await _deviceService.GetDeviceInfoAsync();
            
            // Get the main window safely
            var mainWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            
            // Ensure we're on the UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    // Create and show the popup
                    var popup = new DeviceInfoPopup(
                        mainWindow?.IsActive == true ? mainWindow : null,
                        $"Device: {_deviceService.DeviceName}",
                        info
                    );
                    
                    AddToConsole("✓ Device information retrieved successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error showing device info popup: {ex.Message}");
                    AddToConsole($"Error showing device info: {ex.Message}", true);
                }
            });
        }
        catch (Exception ex)
        {
            AddToConsole($"Error getting device info: {ex.Message}", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AddToConsole(string message, bool isError = false)
    {
        string timestamp = $"[{DateTime.Now:HH:mm:ss}]";
        string formattedMessage = isError 
            ? $"{timestamp} ERROR: {message}" 
            : $"{timestamp} {message}";
            
        ConsoleOutput = $"{formattedMessage}\n{ConsoleOutput}";
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _deviceService.StatusChanged -= OnDeviceStatusChanged;
                if (_deviceService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _disposed = true;
        }
    }
    
    ~ExternalSurfacesViewModel()
    {
        Dispose(false);
    }
}
