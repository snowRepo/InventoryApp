using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.ViewModels;

public partial class SalesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _search = string.Empty;

    [ObservableProperty]
    private int? _selectedMonth;

    [ObservableProperty]
    private int? _selectedYear;

    [ObservableProperty]
    private int _page = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _noResults;

    public ObservableCollection<Sale> Items { get; } = new();

    public int TotalPages => TotalCount == 0 ? 1 : (TotalCount + PageSize - 1) / PageSize;

    public SalesViewModel()
    {
        _ = ReloadAsync();
    }

    partial void OnSearchChanged(string value)
    {
        Page = 1;
        _ = ReloadAsync();
    }

    partial void OnSelectedMonthChanged(int? value)
    {
        Page = 1;
        _ = ReloadAsync();
    }

    partial void OnSelectedYearChanged(int? value)
    {
        Page = 1;
        _ = ReloadAsync();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (Page < TotalPages)
        {
            Page++;
            await ReloadAsync();
        }
    }

    [RelayCommand]
    private async Task PrevPage()
    {
        if (Page > 1)
        {
            Page--;
            await ReloadAsync();
        }
    }

    public async Task ReloadAsync()
    {
        using var db = new AppDbContext();
        
        var query = db.Sales.AsQueryable();

        // Search filter (case-insensitive)
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim().ToLower();
            query = query.Where(sale => 
                sale.TransactionId.ToLower().Contains(s) || 
                sale.ProductName.ToLower().Contains(s));
        }

        // Month/Year filter
        if (SelectedMonth.HasValue && SelectedYear.HasValue)
        {
            query = query.Where(sale => 
                sale.SaleDate.Month == SelectedMonth.Value && 
                sale.SaleDate.Year == SelectedYear.Value);
        }
        else if (SelectedMonth.HasValue)
        {
            query = query.Where(sale => sale.SaleDate.Month == SelectedMonth.Value);
        }
        else if (SelectedYear.HasValue)
        {
            query = query.Where(sale => sale.SaleDate.Year == SelectedYear.Value);
        }

        // Count
        TotalCount = await query.CountAsync();

        // Paginate and order by date descending (newest first)
        var items = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        HasResults = Items.Count > 0;
        NoResults = Items.Count == 0;

        OnPropertyChanged(nameof(TotalPages));
    }
}
