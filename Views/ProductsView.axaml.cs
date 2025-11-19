using Avalonia.Controls;
using Avalonia;
using System.Threading.Tasks;
using InventoryApp.Views;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.ViewModels;
using Avalonia.Interactivity;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Views;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();
        // Temporary fallback to verify rendering even if XAML has issues
        if (Content is null)
        {
            Content = new TextBlock { Text = "ProductsView rendered (fallback)", Margin = new Thickness(12) };
        }
    }

    private async void OnAddProduct(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        if (window is null) return;

        var dlg = new AddProductWindow();
        var result = await dlg.ShowDialog<AddProductWindow.AddProductResult?>(window);
        if (result is null) return;

        try
        {
            using (var db = new AppDbContext())
            {
                // Generate a unique SKU if not provided
                string sku = !string.IsNullOrWhiteSpace(result.Sku) 
                    ? result.Sku 
                    : "P" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();

                // Ensure SKU is unique
                if (await db.Products.AnyAsync(p => p.Sku == sku))
                {
                    await ShowInfoAsync(window, "Error: A product with this SKU already exists.");
                    return;
                }

                var product = new Product
                {
                    Name = result.Name.Trim(),
                    Category = result.Category.Trim(),
                    Sku = sku,
                    UnitPrice = Math.Round(result.UnitPrice, 2),
                    Quantity = Math.Max(0, result.Quantity),
                    ExpiryDate = result.ExpiryDate
                };
                
                db.Products.Add(product);
                await db.SaveChangesAsync();
            }

            await ShowInfoAsync(window, "Product added successfully.");
        }
        catch (Exception ex)
        {
            await ShowInfoAsync(window, $"Failed to add product: {ex.Message}");
        }

        await RefreshProductsAndDashboardAsync();
    }

    private async void OnEditProduct(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not ProductViewModel productVm) return;
        var product = productVm.Product; // Get the underlying Product model

        var window = this.VisualRoot as Window;
        if (window is null) return;

        var dlg = new AddProductWindow();
        
        // Update the window title and button text for edit mode
        dlg.Title = "Edit Product";
        var addButton = dlg.FindControl<Button>("AddButton");
        if (addButton != null)
        {
            addButton.Content = "Update";
        }
        
        // Prefill existing values
        dlg.FindControl<TextBox>("NameBox")!.Text = product.Name;
        var categoryBox = dlg.FindControl<ComboBox>("CategoryBox");
        if (categoryBox != null)
        {
            categoryBox.SelectedItem = product.Category;
        }
        dlg.FindControl<TextBox>("PriceBox")!.Text = product.UnitPrice.ToString("F2");
        dlg.FindControl<TextBox>("QuantityBox")!.Text = product.Quantity.ToString();
        dlg.FindControl<TextBox>("SkuBox")!.Text = product.Sku; // keep existing SKU
        var expiryPicker = dlg.FindControl<DatePicker>("ExpiryDateBox");
        if (expiryPicker != null)
        {
            expiryPicker.SelectedDate = product.ExpiryDate.HasValue
                ? new DateTimeOffset(product.ExpiryDate.Value)
                : (DateTimeOffset?)null;
        }

        var result = await dlg.ShowDialog<AddProductWindow.AddProductResult?>(window);
        if (result is null) return;

        try
        {
            using (var db = new AppDbContext())
            {
                var exists = await db.Products.FindAsync(product.Id);
                if (exists is null) return;
                exists.Name = result.Name;
                exists.Category = result.Category;
                exists.UnitPrice = result.UnitPrice;
                exists.Quantity = result.Quantity;
                exists.ExpiryDate = result.ExpiryDate;
                string SanitizeSku(string s)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var ch in (s ?? string.Empty).ToUpperInvariant())
                    {
                        if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')) sb.Append(ch);
                        if (sb.Length >= 5) break;
                    }
                    return sb.ToString();
                }
                if (!string.IsNullOrWhiteSpace(result.Sku))
                {
                    exists.Sku = SanitizeSku(result.Sku);
                }
                await db.SaveChangesAsync();
            }

            await ShowInfoAsync(window, "Product updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowInfoAsync(window, $"Failed to update product: {ex.Message}");
        }

        await RefreshProductsAndDashboardAsync();
    }

    private async void OnDeleteProduct(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button btn) return;
            
            // Get the ProductViewModel from the DataContext
            var productVm = btn.DataContext as ProductViewModel;
            if (productVm == null) 
            {
                Console.WriteLine("Could not find product view model to delete");
                return;
            }

            var window = this.VisualRoot as Window;
            if (window is null) 
            {
                Console.WriteLine("Could not find parent window");
                return;
            }

            var confirm = new ConfirmDialog();
            var ok = await confirm.ShowAsync(window, $"Are you sure you want to delete '{productVm.Name}'?");
            if (!ok) return;

            using (var db = new AppDbContext())
            {
                var productToDelete = await db.Products.FindAsync(productVm.Id);
                if (productToDelete != null)
                {
                    db.Products.Remove(productToDelete);
                    await db.SaveChangesAsync();
                    await ShowInfoAsync(window, $"Product '{productVm.Name}' was successfully deleted.");
                }
                else
                {
                    await ShowInfoAsync(window, "Product not found. It may have been already deleted.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting product: {ex}");
            var window = this.VisualRoot as Window;
            if (window != null)
            {
                await ShowInfoAsync(window, $"Failed to delete product: {ex.Message}");
            }
            return;
        }

        // Refresh the products list and dashboard
        await RefreshProductsAndDashboardAsync();
    }

    private async Task ShowInfoAsync(Window owner, string message)
    {
        var info = new InfoDialog();
        await info.ShowAsync(owner, message);
    }

    private async Task RefreshProductsAndDashboardAsync()
    {
        if (DataContext is ProductsViewModel vm)
        {
            await vm.RefreshAsync();
        }

        // Refresh dashboard if available
        var mainWindow = this.VisualRoot as Window;
        if (mainWindow?.DataContext is MainWindowViewModel mainVm)
        {
            mainVm.DashboardVM.Refresh();
        }
    }
}
