using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using InventoryApp.Data;
using System.Collections.Generic;
using InventoryApp.Models;
using InventoryApp.Services;
using InventoryApp.Helpers;
using System.ComponentModel;

namespace InventoryApp.ViewModels;

public partial class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly string _username;
    private readonly UserSettingsService _settingsService;

    public string Username => _username;

    public event Action? LogoutRequested;

    public DashboardViewModel(string username, UserSettingsService settingsService)
    {
        _username = username;
        _settingsService = settingsService;
        _settingsService.SettingsChanged += OnSettingsChanged;
        Refresh();
    }

    private void OnSettingsChanged(object sender, EventArgs e)
    {
        OnPropertyChanged(nameof(FormattedInventoryValue));
        OnPropertyChanged(nameof(FormattedSalesToday));
        OnPropertyChanged(nameof(FormattedSalesAllTime));
    }

    public void Dispose()
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;
    }

    [RelayCommand]
    private void Logout()
    {
        LogoutRequested?.Invoke();
    }

    public int TotalProducts { get; private set; }
    public decimal InventoryValue { get; private set; }
    public decimal SalesToday { get; private set; }
    public decimal SalesAllTime { get; private set; }
    
    public string FormattedInventoryValue => InventoryValue.FormatCurrency(_settingsService.CurrentSettings);
    public string FormattedSalesToday => SalesToday.FormatCurrency(_settingsService.CurrentSettings);
    public string FormattedSalesAllTime => SalesAllTime.FormatCurrency(_settingsService.CurrentSettings);
    public IReadOnlyList<LowStockItem> LowStock { get; private set; } = Array.Empty<LowStockItem>();
    public bool HasLowStock { get; private set; }
    public bool NoLowStock => !HasLowStock;

    public void Refresh()
    {
        using var db = new AppDbContext();
        
        try
        {
            // Load all products into memory first
            var products = db.Products.ToList();
            var sales = db.Sales.ToList();
            
            // Update total products
            TotalProducts = products.Count;
            
            // Calculate inventory value in memory
            InventoryValue = Math.Round(products.Sum(p => p.UnitPrice * p.Quantity), 2);
            
            // Calculate sales in memory
            var today = DateTime.Today;
            SalesToday = sales
                .Where(s => s.SaleDate >= today)
                .Sum(s => s.TotalPrice);
                
            SalesAllTime = sales.Sum(s => s.TotalPrice);

            // Get low stock items
            var low = products
                .Where(p => p.Quantity <= 3)
                .OrderBy(p => p.Quantity)
                .ThenBy(p => p.Name)
                .Select(p => new LowStockItem(p.Name, p.UnitPrice, p.Quantity, _settingsService))
                .ToList();
                
            LowStock = low;
            HasLowStock = low.Count > 0;

            // Notify UI of all property changes
            OnPropertyChanged(nameof(TotalProducts));
            OnPropertyChanged(nameof(InventoryValue));
            OnPropertyChanged(nameof(FormattedInventoryValue));
            OnPropertyChanged(nameof(SalesToday));
            OnPropertyChanged(nameof(FormattedSalesToday));
            OnPropertyChanged(nameof(SalesAllTime));
            OnPropertyChanged(nameof(FormattedSalesAllTime));
            OnPropertyChanged(nameof(LowStock));
            OnPropertyChanged(nameof(HasLowStock));
            OnPropertyChanged(nameof(NoLowStock));
        }
        catch (Exception ex)
        {
            // Log the error or show a message
            Console.WriteLine($"Error refreshing dashboard: {ex}");
            // Re-throw to maintain the same behavior as before
            throw;
        }
    }
}

public class LowStockItem : ObservableObject
{
    private readonly UserSettingsService _settingsService;
    
    public string Name { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; }
    
    public string FormattedUnitPrice => UnitPrice.FormatCurrency(_settingsService.CurrentSettings);
    
    public LowStockItem(string name, decimal unitPrice, int quantity, UserSettingsService settingsService)
    {
        Name = name;
        UnitPrice = unitPrice;
        Quantity = quantity;
        _settingsService = settingsService;
    }
}
