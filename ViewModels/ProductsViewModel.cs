using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryApp.ViewModels;

public partial class ProductsViewModel : ViewModelBase
{
    public ObservableCollection<Product> Items { get; } = new();

    [ObservableProperty]
    private string _search = string.Empty;

    [ObservableProperty]
    private decimal? _minPrice;

    [ObservableProperty]
    private decimal? _maxPrice;

    [ObservableProperty]
    private bool _lowStockOnly;

    [ObservableProperty]
    private int _sortIndex = 0; // 0: A-Z, 1: Z-A, 2: Low stock

    [ObservableProperty]
    private int _page = 1;

    [ObservableProperty]
    private int _pageSize = 50;

    [ObservableProperty]
    private int _totalCount;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));

    public bool HasResults { get; private set; }
    public bool NoResults => !HasResults;

    public ProductsViewModel()
    {
        SortIndex = 0; // default A - Z
        _ = RefreshAsync();
    }

    public async Task ReloadAsync()
    {
        await RefreshAsync();
    }

    partial void OnSortIndexChanged(int value)
    {
        _ = RefreshAsync();
    }

    partial void OnSearchChanged(string value)
    {
        Page = 1;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        using var db = new AppDbContext();
        var q = db.Products.AsNoTracking().AsQueryable();

        var s = (Search ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(s))
        {
            q = q.Where(p => EF.Functions.Like(p.Name, $"%{s}%") || EF.Functions.Like(p.Sku, $"%{s}%"));
        }
        if (MinPrice.HasValue)
        {
            q = q.Where(p => p.UnitPrice >= MinPrice.Value);
        }
        if (MaxPrice.HasValue)
        {
            q = q.Where(p => p.UnitPrice <= MaxPrice.Value);
        }
        if (LowStockOnly)
        {
            q = q.Where(p => p.Quantity <= 3);
        }

        TotalCount = await q.CountAsync();
        OnPropertyChanged(nameof(TotalPages));

        // Apply ordering
        q = SortIndex switch
        {
            0 => q.OrderBy(p => p.Name),                         // A-Z
            1 => q.OrderByDescending(p => p.Name),               // Z-A
            2 => q.OrderBy(p => p.Quantity).ThenBy(p => p.Name), // Low stock
            _ => q.OrderBy(p => p.Name)                          // Default: A-Z
        };

        var skip = Math.Max(0, (Page - 1) * PageSize);
        var pageItems = await q.Skip(skip).Take(PageSize).ToListAsync();

        Items.Clear();
        foreach (var it in pageItems)
            Items.Add(it);

        OnPropertyChanged(nameof(TotalCount));
        HasResults = TotalCount > 0;
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(NoResults));
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (Page < TotalPages)
        {
            Page++;
            await RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (Page > 1)
        {
            Page--;
            await RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        Page = 1;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        Search = string.Empty;
        MinPrice = null;
        MaxPrice = null;
        LowStockOnly = false;
        Page = 1;
        await RefreshAsync();
    }
}
