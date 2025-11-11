using Avalonia.Controls;
using Avalonia;
using System.Threading.Tasks;
using InventoryApp.Views;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.ViewModels;
using Avalonia.Interactivity;
using System;

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
                var product = new Product
                {
                    Name = result.Name,
                    Category = result.Category,
                    Sku = SanitizeSku(result.Sku),
                    UnitPrice = result.UnitPrice,
                    Quantity = result.Quantity,
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
        if (btn.DataContext is not Product product) return;

        var window = this.VisualRoot as Window;
        if (window is null) return;

        var dlg = new AddProductWindow();
        // Prefill existing values
        dlg.FindControl<TextBox>("NameBox")!.Text = product.Name;
        var categoryBox = dlg.FindControl<ComboBox>("CategoryBox");
        if (categoryBox != null)
        {
            categoryBox.SelectedItem = product.Category;
        }
        dlg.FindControl<TextBox>("PriceBox")!.Text = product.UnitPrice.ToString();
        dlg.FindControl<TextBox>("QuantityBox")!.Text = product.Quantity.ToString();
        dlg.FindControl<TextBox>("SkuBox")!.Text = product.Sku; // keep existing SKU

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
        if (sender is not Button btn) return;
        if (btn.DataContext is not Product product) return;

        var window = this.VisualRoot as Window;
        if (window is null) return;

        var confirm = new ConfirmDialog();
        var ok = await confirm.ShowAsync(window, $"Delete product '{product.Name}'?");
        if (!ok) return;

        try
        {
            using (var db = new AppDbContext())
            {
                var exists = await db.Products.FindAsync(product.Id);
                if (exists is null) return;
                db.Products.Remove(exists);
                await db.SaveChangesAsync();
            }

            await ShowInfoAsync(window, "Product deleted successfully.");
        }
        catch (Exception ex)
        {
            await ShowInfoAsync(window, $"Failed to delete product: {ex.Message}");
        }

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
            await vm.ReloadAsync();
        }
        // Refresh dashboard stats if accessible
        if (this.VisualRoot is Window win && win.DataContext is InventoryApp.ViewModels.MainWindowViewModel shell)
        {
            shell.DashboardVM.Refresh();
        }
    }
}
