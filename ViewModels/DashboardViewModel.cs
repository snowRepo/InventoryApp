using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using InventoryApp.Data;
using System.Collections.Generic;
using InventoryApp.Models;

namespace InventoryApp.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    public string Username { get; }

    public event Action? LogoutRequested;

    public DashboardViewModel(string username)
    {
        Username = username;
        Refresh();
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
    public IReadOnlyList<LowStockItem> LowStock { get; private set; } = Array.Empty<LowStockItem>();
    public bool HasLowStock { get; private set; }
    public bool NoLowStock => !HasLowStock;

    public void Refresh()
    {
        using var db = new AppDbContext();
        TotalProducts = db.Products.Count();
        // SQLite can't translate Sum over decimal in some cases; sum as double and cast back
        var inv = db.Products.Select(p => (double)p.UnitPrice * p.Quantity).Sum();
        InventoryValue = (decimal)inv;
        
        // Calculate sales
        var today = DateTime.Today;
        var salesTodaySum = db.Sales
            .Where(s => s.SaleDate >= today)
            .Select(s => (double)s.TotalPrice)
            .Sum();
        SalesToday = (decimal)salesTodaySum;
        
        var salesAllTimeSum = db.Sales
            .Select(s => (double)s.TotalPrice)
            .Sum();
        SalesAllTime = (decimal)salesAllTimeSum;

        var low = db.Products
            .Where(p => p.Quantity <= 3)
            .OrderBy(p => p.Quantity)
            .ThenBy(p => p.Name)
            .Select(p => new LowStockItem(p.Name, p.UnitPrice, p.Quantity))
            .ToList();
        LowStock = low;
        HasLowStock = LowStock.Count > 0;

        OnPropertyChanged(nameof(TotalProducts));
        OnPropertyChanged(nameof(InventoryValue));
        OnPropertyChanged(nameof(SalesToday));
        OnPropertyChanged(nameof(SalesAllTime));
        OnPropertyChanged(nameof(LowStock));
        OnPropertyChanged(nameof(HasLowStock));
        OnPropertyChanged(nameof(NoLowStock));
    }
}

public readonly record struct LowStockItem(string Name, decimal UnitPrice, int Quantity);
