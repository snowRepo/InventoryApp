using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using InventoryApp.ViewModels;
using InventoryApp.Data;
using InventoryApp.Models;
using System;
using System.Linq;

namespace InventoryApp.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
        InitializeFilters();
    }

    private void InitializeFilters()
    {
        // Populate month ComboBox
        var monthCombo = this.FindControl<ComboBox>("MonthComboBox");
        if (monthCombo != null)
        {
            var months = new[]
            {
                new { Value = 1, Name = "January" },
                new { Value = 2, Name = "February" },
                new { Value = 3, Name = "March" },
                new { Value = 4, Name = "April" },
                new { Value = 5, Name = "May" },
                new { Value = 6, Name = "June" },
                new { Value = 7, Name = "July" },
                new { Value = 8, Name = "August" },
                new { Value = 9, Name = "September" },
                new { Value = 10, Name = "October" },
                new { Value = 11, Name = "November" },
                new { Value = 12, Name = "December" }
            };
            monthCombo.ItemsSource = months;
            monthCombo.DisplayMemberBinding = new Avalonia.Data.Binding("Name");
        }

        // Populate year ComboBox with current year and past 5 years
        var yearCombo = this.FindControl<ComboBox>("YearComboBox");
        if (yearCombo != null)
        {
            var currentYear = DateTime.Now.Year;
            var years = Enumerable.Range(currentYear - 5, 6).Reverse().Select(y => y).ToList();
            yearCombo.ItemsSource = years;
        }
    }

    private void OnMonthChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not SalesViewModel vm) return;
        
        var monthCombo = sender as ComboBox;
        if (monthCombo?.SelectedItem != null)
        {
            dynamic selected = monthCombo.SelectedItem;
            vm.SelectedMonth = selected.Value;
        }
        else
        {
            vm.SelectedMonth = null;
        }
    }

    private async void OnNewSale(object? sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        if (window is null)
        {
            Console.WriteLine("ERROR: Window is null");
            return;
        }

        Console.WriteLine("Opening New Sale dialog...");
        var dlg = new NewSaleWindow();
        var result = await dlg.ShowDialog(window);
        
        Console.WriteLine($"Dialog closed. Result: {(result == null ? "NULL" : $"ProductId={result.ProductId}, Product={result.ProductName}, Qty={result.QuantitySold}")}");
        
        if (result is null)
        {
            Console.WriteLine("Result is null - user cancelled or validation failed");
            return;
        }

        try
        {
            Console.WriteLine($"Recording sale: Product={result.ProductName}, Qty={result.QuantitySold}, Price={result.Price}");
            
            using (var db = new AppDbContext())
            {
                // Generate transaction ID
                var transactionId = $"TXN-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
                Console.WriteLine($"Generated transaction ID: {transactionId}");

                // Create sale record
                var sale = new Sale
                {
                    TransactionId = transactionId,
                    ProductName = result.ProductName,
                    Price = result.Price,
                    QuantitySold = result.QuantitySold,
                    TotalPrice = result.Price * result.QuantitySold,
                    SaleDate = DateTime.Now
                };
                db.Sales.Add(sale);
                Console.WriteLine($"Sale added to context");

                // Update product quantity
                var product = await db.Products.FindAsync(result.ProductId);
                if (product != null)
                {
                    Console.WriteLine($"Found product: {product.Name}, Current Qty: {product.Quantity}");
                    product.Quantity -= result.QuantitySold;
                    Console.WriteLine($"Updated product quantity to: {product.Quantity}");
                }
                else
                {
                    Console.WriteLine($"ERROR: Product not found with ID: {result.ProductId}");
                }

                var changes = await db.SaveChangesAsync();
                Console.WriteLine($"SaveChanges returned: {changes} changes saved to database");
            }

            Console.WriteLine("Showing success message");
            await ShowInfoAsync(window, "Sale recorded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR recording sale: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            await ShowInfoAsync(window, $"Failed to record sale: {ex.Message}\n\nDetails: {ex.InnerException?.Message}");
        }

        Console.WriteLine("Refreshing sales and dashboard");
        await RefreshSalesAndDashboardAsync();
    }

    private async Task ShowInfoAsync(Window owner, string message)
    {
        var info = new InfoDialog();
        await info.ShowAsync(owner, message);
    }

    private async Task RefreshSalesAndDashboardAsync()
    {
        if (DataContext is SalesViewModel vm)
        {
            await vm.ReloadAsync();
        }
        // Refresh dashboard and products if accessible
        if (this.VisualRoot is Window win && win.DataContext is InventoryApp.ViewModels.MainWindowViewModel shell)
        {
            shell.DashboardVM.Refresh();
            await shell.ProductsVM.ReloadAsync();
            Console.WriteLine("Refreshed Dashboard and Products");
        }
    }
}
