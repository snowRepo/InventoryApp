using InventoryApp.Models;
using InventoryApp.Services;
using InventoryApp.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace InventoryApp.ViewModels;

public partial class ProductViewModel : ObservableObject, IDisposable
{
    private readonly Product _product;
    private readonly UserSettingsService _settingsService;
    
    // Expose the underlying Product model
    public Product Product => _product;

    public int Id => _product.Id;
    public string Sku => _product.Sku;
    public string Name => _product.Name;
    public string Category => _product.Category;
    public decimal UnitPrice => _product.UnitPrice;
    public string FormattedUnitPrice => UnitPrice.FormatCurrency(_settingsService.CurrentSettings);
    public int Quantity => _product.Quantity;
    public string FormattedTotalValue => (UnitPrice * Quantity).FormatCurrency(_settingsService.CurrentSettings);
    public DateTime? ExpiryDate => _product.ExpiryDate;
    public string ExpiryDateText => _product.ExpiryDate?.ToShortDateString() ?? string.Empty;

    public ProductViewModel(Product product, UserSettingsService settingsService)
    {
        _product = product;
        _settingsService = settingsService;
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object sender, EventArgs e)
    {
        OnPropertyChanged(nameof(FormattedUnitPrice));
        OnPropertyChanged(nameof(FormattedTotalValue));
    }

    public void Dispose()
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;
    }
}
