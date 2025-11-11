using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using InventoryApp.Data;

namespace InventoryApp.Views;

public partial class AddProductWindow : Window, INotifyPropertyChanged
{
    private List<string> _categories = new();
    private string? _selectedCategory;
    
    public List<string> Categories 
    { 
        get => _categories;
        private set 
        {
            if (Equals(value, _categories)) return;
            _categories = value ?? new List<string>();
            OnPropertyChanged();
        }
    }
    
    public string? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged(nameof(SelectedCategory));
            GenerateSku();
        }
    }
    
    public new event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public AddProductWindow()
    {
        InitializeComponent();
        LoadCategories();
        
        // Set up data context
        this.DataContext = this;
        
        // Leave SKU empty until both Name and Category are provided
        var skuBox = this.FindControl<TextBox>("SkuBox");
        if (skuBox is not null) skuBox.Text = string.Empty;
    }
    
    private void LoadCategories()
    {
        try
        {
            using var db = new AppDbContext();
            Categories = db.Products
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading categories: {ex.Message}");
            Categories = new List<string>();
        }
    }

    private void GenerateSku()
    {
        var nameBox = this.FindControl<TextBox>("NameBox");
        var categoryBox = this.FindControl<ComboBox>("CategoryBox");
        var name = (nameBox?.Text ?? string.Empty).Trim();
        var category = categoryBox?.Text?.Trim() ?? string.Empty;
        var skuBox = this.FindControl<TextBox>("SkuBox");

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
        {
            if (skuBox is not null) skuBox.Text = string.Empty;
            return;
        }
        
        // Build SKU from Category + Name, alphanumeric only, uppercase, max 5 chars
        string Filter(string s)
        {
            var chars = new System.Text.StringBuilder();
            foreach (var ch in s.ToUpperInvariant())
            {
                if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')) chars.Append(ch);
            }
            return chars.ToString();
        }
        
        var combined = Filter(category + name);
        var sku = combined.Length > 5 ? combined.Substring(0, 5) : combined;
        if (skuBox is not null) skuBox.Text = sku;
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnInputChanged(object? sender, TextChangedEventArgs e)
    {
        GenerateSku();
    }
    
    private void OnCategoryTextChanged(object? sender, TextChangedEventArgs e)
    {
        GenerateSku();
    }

    private void OnAdd(object? sender, RoutedEventArgs e)
    {
        var nameBox = this.FindControl<TextBox>("NameBox");
        var categoryBox = this.FindControl<ComboBox>("CategoryBox");
        var priceBox = this.FindControl<TextBox>("PriceBox");
        var quantityBox = this.FindControl<TextBox>("QuantityBox");
        var skuBox = this.FindControl<TextBox>("SkuBox");

        var name = nameBox?.Text?.Trim() ?? string.Empty;
        var category = categoryBox?.Text?.Trim() ?? string.Empty;
        var priceText = priceBox?.Text ?? string.Empty;
        var qtyText = quantityBox?.Text ?? string.Empty;
        var skuText = skuBox?.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name)) { return; }
        if (string.IsNullOrWhiteSpace(category)) { return; }
        if (!decimal.TryParse(priceText, out var price)) { return; }
        if (!int.TryParse(qtyText, out var qty)) { return; }

        if (string.IsNullOrWhiteSpace(skuText))
        {
            // Generate SKU if still empty and inputs are valid (max 5, alnum)
            string Filter(string s)
            {
                var chars = new System.Text.StringBuilder();
                foreach (var ch in s.ToUpperInvariant())
                {
                    if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')) chars.Append(ch);
                }
                return chars.ToString();
            }
            var combined = Filter(category + name);
            skuText = combined.Length > 5 ? combined.Substring(0, 5) : combined;
        }

        var result = new AddProductResult
        {
            Name = name.Trim(),
            Category = category.Trim(),
            UnitPrice = price,
            Quantity = Math.Max(0, qty),
            Sku = skuText.Trim(),
        };

        Close(result);
    }

    public class AddProductResult
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}
