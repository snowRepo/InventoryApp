using System;
using System.Threading.Tasks;

namespace InventoryApp.Services
{
    public class MockDeviceService : IDeviceService
    {
        private bool _isConnected;
        private string _deviceName = "MOCK_DEVICE_001";
        private string _connectionStatus = "Disconnected";
        private readonly Random _random = new Random();

        public event EventHandler<DeviceStatusChangedEventArgs> StatusChanged;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    _connectionStatus = value ? "Connected" : "Disconnected";
                    StatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(value, _connectionStatus));
                }
            }
        }

        public string DeviceName => _deviceName;
        public string ConnectionStatus => _connectionStatus;

        public async Task<bool> CheckStatusOnlyAsync()
        {
            // Simulate device check without connecting
            await Task.Delay(300);
            bool isAvailable = _random.Next(10) > 1; // 80% success rate for finding a device
            _connectionStatus = isAvailable ? "Device available" : "No device found";
            return isAvailable;
        }

        public async Task<bool> CheckConnectionAsync()
        {
            // Simulate device check with 90% success rate when not connected
            await Task.Delay(500);
            if (!_isConnected)
            {
                bool success = _random.Next(10) > 0; // 90% success rate
                if (success)
                {
                    _connectionStatus = "Device found";
                    return true;
                }
                _connectionStatus = "No device found";
                return false;
            }
            return true;
        }

        public async Task<bool> ConnectAsync()
        {
            if (_isConnected) return true;
            
            // Simulate connection delay (1-2 seconds)
            await Task.Delay(_random.Next(1000, 2001));
            
            // 80% success rate for connection
            bool success = _random.Next(10) > 1;
            if (success)
            {
                IsConnected = true;
                _connectionStatus = "Connected successfully";
                return true;
            }
            
            _connectionStatus = "Connection failed";
            return false;
        }

        public async Task DisconnectAsync()
        {
            if (!_isConnected) return;
            
            // Simulate disconnection delay
            await Task.Delay(500);
            IsConnected = false;
            _connectionStatus = "Disconnected";
        }

        public async Task<string> ExecuteCommandAsync(string command)
        {
            if (!_isConnected)
                return "Error: Not connected to device";

            await Task.Delay(100); // Simulate command execution time
            
            return command.ToUpper() switch
            {
                "GET_STATUS" => "Status: OK\nBattery: 87%\nSignal: Strong",
                "GET_ID" => $"Device ID: {_deviceName}",
                "VERSION" => "Firmware: v1.2.3\nHardware: Rev B",
                _ => $"Unknown command: {command}"
            };
        }

        public async Task<string> GetDeviceInfoAsync()
        {
            if (!_isConnected)
                return "Error: Not connected to device";

            await Task.Delay(200); // Simulate device info retrieval
            
            return $"""
                   Device Information:
                   - Name: {_deviceName}
                   - Type: Mock Device
                   - Firmware: v1.2.3
                   - Hardware: Rev B
                   - Status: Connected
                   - Last Check: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                   """;
        }
    }
}
