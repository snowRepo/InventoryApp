using System;
using System.Threading.Tasks;

namespace InventoryApp.Services;

public interface IDeviceService
{
    event EventHandler<DeviceStatusChangedEventArgs> StatusChanged;
    
    bool IsConnected { get; }
    string DeviceName { get; }
    string ConnectionStatus { get; }
    
    Task<bool> CheckConnectionAsync();
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<string> ExecuteCommandAsync(string command);
    Task<string> GetDeviceInfoAsync();
    
    /// <summary>
    /// Checks if a device is available without establishing a connection
    /// </summary>
    /// <returns>True if device is available, false otherwise</returns>
    Task<bool> CheckStatusOnlyAsync();
}

public class DeviceStatusChangedEventArgs : EventArgs
{
    public bool IsConnected { get; }
    public string StatusMessage { get; }
    public DateTime Timestamp { get; } = DateTime.Now;

    public DeviceStatusChangedEventArgs(bool isConnected, string statusMessage)
    {
        IsConnected = isConnected;
        StatusMessage = statusMessage;
    }
}
