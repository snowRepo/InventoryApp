using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.Services;

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

    public string CurrencySymbol { get; }
    
    public AddProductWindow()
    {
        var settingsService = App.Resolver.Resolve<UserSettingsService>();
        CurrencySymbol = settingsService.CurrentSettings.CurrencySymbol;
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
        // Don't generate SKU if the SKU box already has a value (user might be editing it)
        var skuBox = this.FindControl<TextBox>("SkuBox");
        if (skuBox?.Text?.Length > 0) return;

        var nameBox = this.FindControl<TextBox>("NameBox");
        var categoryBox = this.FindControl<ComboBox>("CategoryBox");
        var name = (nameBox?.Text ?? string.Empty).Trim();
        var category = categoryBox?.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
        {
            if (skuBox is not null) skuBox.Text = string.Empty;
            return;
        }
        
        // Generate a simple SKU based on category and name (first 2 chars of each)
        string sku = (category[..Math.Min(2, category.Length)] + 
                     name[..Math.Min(3, name.Length)])
                     .ToUpper();
                     
        // Remove non-alphanumeric characters
        sku = new string(sku.Where(c => char.IsLetterOrDigit(c)).ToArray());
        
        // Ensure we have at least 3 characters
        if (sku.Length < 3)
        {
            sku = sku.PadRight(3, 'X');
        }
        
        // Add a random 2-digit number to make it more unique
        var random = new Random();
        sku += random.Next(10, 100).ToString();
        
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

    private void OnPriceChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        
        // Don't process if this change was triggered by our own code
        if (textBox.Tag as string == "processing") return;
        
        try
        {
            // Mark that we're processing to prevent recursive calls
            textBox.Tag = "processing";
            
            var text = textBox.Text ?? string.Empty;
            
            // Remove any existing currency symbol to prevent duplication
            text = text.Replace(CurrencySymbol, "").Trim();
            
            // If the text is empty, just clear and return
            if (string.IsNullOrEmpty(text))
            {
                textBox.Text = string.Empty;
                return;
            }
            
            // Add the currency symbol at the start of the text
            var newText = $"{CurrencySymbol} {text.Trim()}";
            
            // Only update if the text has actually changed to prevent cursor jumping
            if (textBox.Text != newText)
            {
                // Save the cursor position
                var cursorPos = textBox.CaretIndex;
                
                // Update the text with the currency symbol
                textBox.Text = newText;
                
                // Restore the cursor position, adjusting for the added currency symbol
                textBox.CaretIndex = Math.Min(cursorPos + CurrencySymbol.Length + 1, textBox.Text?.Length ?? 0);
            }
        }
        finally
        {
            // Clear the processing flag
            textBox.Tag = null;
        }
    }

    private async void OnAdd(object? sender, RoutedEventArgs e)
    {
        try
        {
            var nameBox = this.FindControl<TextBox>("NameBox");
            var categoryBox = this.FindControl<ComboBox>("CategoryBox");
            var priceBox = this.FindControl<TextBox>("PriceBox");
            var quantityBox = this.FindControl<TextBox>("QuantityBox");
            var skuBox = this.FindControl<TextBox>("SkuBox");

            var name = nameBox?.Text?.Trim() ?? string.Empty;
            var category = categoryBox?.Text?.Trim() ?? string.Empty;
            var priceText = priceBox?.Text?.Replace(CurrencySymbol, "").Trim() ?? string.Empty;
            var qtyText = quantityBox?.Text?.Trim() ?? string.Empty;
            var skuText = skuBox?.Text?.Trim() ?? string.Empty;

            // Input validation
            if (string.IsNullOrWhiteSpace(name))
            {
                await ShowError("Please enter a product name");
                nameBox?.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                await ShowError("Please select or enter a category");
                categoryBox?.Focus();
                return;
            }

            if (!decimal.TryParse(priceText, System.Globalization.NumberStyles.Currency | System.Globalization.NumberStyles.AllowDecimalPoint, 
                               System.Globalization.CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                await ShowError("Please enter a valid price");
                priceBox?.SelectAll();
                priceBox?.Focus();
                return;
            }

            if (!int.TryParse(qtyText, out var qty) || qty < 0)
            {
                await ShowError("Please enter a valid quantity");
                quantityBox?.SelectAll();
                quantityBox?.Focus();
                return;
            }

            // Only generate SKU if empty and not already generated by GenerateSku()
            if (string.IsNullOrWhiteSpace(skuText))
            {
                // This should be handled by GenerateSku() which is called when name or category changes
                await ShowError("Please enter a valid SKU or let the system generate one by filling in the name and category");
                return;
            }

            var result = new AddProductResult
            {
                Name = name,
                Category = category,
                UnitPrice = Math.Round(price, 2), // Ensure we only keep 2 decimal places
                Quantity = Math.Max(0, qty),
                Sku = skuText
            };

            Close(result);
        }
        catch (Exception ex)
        {
            await ShowError($"An error occurred: {ex.Message}");
        }
    }

    private async Task ShowError(string message)
    {
        var dialog = new InfoDialog();
        await dialog.ShowAsync(this, $"Error: {message}");
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
