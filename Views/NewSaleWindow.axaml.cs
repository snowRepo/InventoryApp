using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Views;

public partial class NewSaleWindow : Window
{
    private TaskCompletionSource<NewSaleResult?>? _tcs;
    private Product? _selectedProduct;

    public record NewSaleResult(int ProductId, string ProductName, decimal Price, int QuantitySold);

    public NewSaleWindow()
    {
        InitializeComponent();
        this.FindControl<TextBox>("QuantityBox")!.TextChanged += OnQuantityChanged;
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
            this.FindControl<TextBox>("PriceBox")!.Text = _selectedProduct.UnitPrice.ToString("F2");
            this.FindControl<TextBox>("AvailableQuantityBox")!.Text = _selectedProduct.Quantity.ToString();
            this.FindControl<TextBox>("TotalPriceBox")!.Text = "0.00";
            this.FindControl<TextBox>("QuantityBox")!.Text = string.Empty;
        }

        this.FindControl<Border>("ProductResultsBorder")!.IsVisible = false;
        this.FindControl<TextBox>("ProductSearchBox")!.Text = string.Empty;
    }

    private void OnQuantityChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (_selectedProduct is null) return;

        var quantityBox = sender as TextBox;
        if (quantityBox is null) return;

        if (int.TryParse(quantityBox.Text, out int qty) && qty > 0)
        {
            var total = _selectedProduct.UnitPrice * qty;
            this.FindControl<TextBox>("TotalPriceBox")!.Text = total.ToString("F2");
        }
        else
        {
            this.FindControl<TextBox>("TotalPriceBox")!.Text = "0.00";
        }
    }

    private async void OnRecordSale(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("OnRecordSale called");
        
        if (_selectedProduct is null)
        {
            Console.WriteLine("ERROR: No product selected");
            await ShowErrorAsync("Please select a product first.");
            return;
        }

        Console.WriteLine($"Selected product: {_selectedProduct.Name} (ID: {_selectedProduct.Id})");

        var quantityText = this.FindControl<TextBox>("QuantityBox")!.Text;
        Console.WriteLine($"Quantity text: '{quantityText}'");
        
        if (!int.TryParse(quantityText, out int quantity) || quantity <= 0)
        {
            Console.WriteLine($"ERROR: Invalid quantity: '{quantityText}'");
            await ShowErrorAsync("Please enter a valid quantity.");
            return;
        }

        Console.WriteLine($"Parsed quantity: {quantity}, Available: {_selectedProduct.Quantity}");

        if (quantity > _selectedProduct.Quantity)
        {
            Console.WriteLine($"ERROR: Quantity {quantity} exceeds available {_selectedProduct.Quantity}");
            await ShowErrorAsync($"Quantity exceeds available stock. Available: {_selectedProduct.Quantity}");
            return;
        }

        Console.WriteLine("Creating NewSaleResult...");
        var result = new NewSaleResult(
            _selectedProduct.Id,
            _selectedProduct.Name,
            _selectedProduct.UnitPrice,
            quantity
        );

        Console.WriteLine($"Setting result and closing dialog: ProductId={result.ProductId}, Product={result.ProductName}, Qty={result.QuantitySold}");
        _tcs?.TrySetResult(result);
        Close();
    }

    private async Task ShowErrorAsync(string message)
    {
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
