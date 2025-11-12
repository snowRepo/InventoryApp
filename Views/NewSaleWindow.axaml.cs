using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Services;

namespace InventoryApp.Views;

public partial class NewSaleWindow : Window
{
    private TaskCompletionSource<NewSaleResult?>? _tcs;
    private Product? _selectedProduct;

    public record NewSaleResult(int ProductId, string ProductName, decimal Price, int QuantitySold);

    public string CurrencySymbol { get; }

    public NewSaleWindow()
    {
        InitializeComponent();
        this.FindControl<TextBox>("QuantityBox")!.TextChanged += OnQuantityChanged;
        
        // Set up data context
        this.DataContext = this;
        
        // Get the current currency settings
        var settingsService = App.Resolver.Resolve<UserSettingsService>();
        
        // Only set currency symbol if it's not the default (empty)
        if (!string.IsNullOrEmpty(settingsService.CurrentSettings.CurrencySymbol) && 
            settingsService.CurrentSettings.CurrencySymbol != "$")
        {
            CurrencySymbol = settingsService.CurrentSettings.CurrencySymbol;
        }
        else
        {
            // Default to empty string if no currency is set
            CurrencySymbol = string.Empty;
        }
    }

    public new Task<NewSaleResult?> ShowDialog(Window owner)
    {
        _tcs = new TaskCompletionSource<NewSaleResult?>();
        _ = base.ShowDialog(owner);
        return _tcs.Task;
    }

    private async void OnProductSearchChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        var searchBox = sender as TextBox;
        if (searchBox is null) return;

        var searchText = searchBox.Text?.Trim() ?? string.Empty;
        var resultsList = this.FindControl<ListBox>("ProductResultsList")!;
        var resultsBorder = this.FindControl<Border>("ProductResultsBorder")!;
        var noProductsText = this.FindControl<TextBlock>("NoProductsText")!;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            resultsBorder.IsVisible = false;
            resultsList.ItemsSource = null;
            noProductsText.IsVisible = false;
            return;
        }

        using var db = new AppDbContext();
        var products = await db.Products
            .Where(p => p.Name.ToLower().Contains(searchText.ToLower()) && p.Quantity > 0)
            .Take(10)
            .ToListAsync();
        
        if (products.Any())
        {
            var items = products.Select(p => new { p.Id, p.Name, p.UnitPrice, p.Quantity, Display = $"{p.Name} - {p.UnitPrice:F2} (Qty: {p.Quantity})" }).ToList();
            resultsList.ItemsSource = items;
            resultsList.DisplayMemberBinding = new Avalonia.Data.Binding("Display");
            resultsList.IsVisible = true;
            noProductsText.IsVisible = false;
            resultsBorder.IsVisible = true;
        }
        else
        {
            resultsList.ItemsSource = null;
            resultsList.IsVisible = false;
            noProductsText.IsVisible = true;
            resultsBorder.IsVisible = true;
        }
    }

    private void UpdatePriceBox(decimal price)
    {
        var priceBox = this.FindControl<TextBox>("PriceBox");
        if (priceBox != null)
        {
            priceBox.Text = string.IsNullOrEmpty(CurrencySymbol) 
                ? $"{price:F2}" 
                : $"{CurrencySymbol} {price:F2}";
        }
    }

    private void UpdateTotalPriceBox(decimal total)
    {
        var totalPriceBox = this.FindControl<TextBox>("TotalPriceBox");
        if (totalPriceBox != null)
        {
            totalPriceBox.Text = string.IsNullOrEmpty(CurrencySymbol)
                ? $"{total:F2}"
                : $"{CurrencySymbol} {total:F2}";
        }
    }

    private async void OnProductSelected(object? sender, SelectionChangedEventArgs e)
    {
        var resultsList = sender as ListBox;
        if (resultsList?.SelectedItem is null) return;

        dynamic selected = resultsList.SelectedItem;
        int productId = selected.Id;

        using var db = new AppDbContext();
        _selectedProduct = await db.Products.FindAsync(productId);

        if (_selectedProduct != null)
        {
            this.FindControl<TextBox>("SelectedProductBox")!.Text = _selectedProduct.Name;
            UpdatePriceBox(_selectedProduct.UnitPrice);
            this.FindControl<TextBox>("AvailableQuantityBox")!.Text = _selectedProduct.Quantity.ToString();
            UpdateTotalPriceBox(0);
            this.FindControl<TextBox>("QuantityBox")!.Text = string.Empty;
        }

        this.FindControl<Border>("ProductResultsBorder")!.IsVisible = false;
        this.FindControl<TextBox>("ProductSearchBox")!.Text = string.Empty;
    }

    private void OnQuantityChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (_selectedProduct is null) return;

        var quantityBox = sender as TextBox;
        if (quantityBox is null || !int.TryParse(quantityBox.Text, out int quantity) || quantity <= 0)
        {
            UpdateTotalPriceBox(0);
            return;
        }

        var total = _selectedProduct.UnitPrice * quantity;
        UpdateTotalPriceBox(total);
    }

    private async void OnRecordSale(object? sender, RoutedEventArgs e)
    {
        if (_selectedProduct is null)
        {
            await ShowError("Please select a product");
            return;
        }

        var quantityBox = this.FindControl<TextBox>("QuantityBox")!;
        if (!int.TryParse(quantityBox.Text, out int quantity) || quantity <= 0)
        {
            await ShowError("Please enter a valid quantity");
            return;
        }

        if (quantity > _selectedProduct.Quantity)
        {
            await ShowError($"Not enough stock. Only {_selectedProduct.Quantity} available");
            return;
        }

        _tcs?.SetResult(new NewSaleResult(
            _selectedProduct.Id,
            _selectedProduct.Name,
            _selectedProduct.UnitPrice,
            quantity
        ));

        Close();
    }

    private async Task ShowError(string message)
    {
        var dialog = new InfoDialog();
        await dialog.ShowAsync(this, message);
        var owner = this.Owner as Window;
        if (owner is null) return;
        
        var info = new InfoDialog();
        await info.ShowAsync(owner, message);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("OnCancel called - user cancelled dialog");
        _tcs?.TrySetResult(null);
        Close();
    }
}
