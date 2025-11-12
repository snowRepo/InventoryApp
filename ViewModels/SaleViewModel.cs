using InventoryApp.Models;
using InventoryApp.Services;
using InventoryApp.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;

namespace InventoryApp.ViewModels;

public class SaleViewModel : ObservableObject
{
    private readonly Sale _sale;
    private readonly UserSettingsService _settingsService;

    public int Id => _sale.Id;
    public string TransactionId => _sale.TransactionId;
    public string ProductName => _sale.ProductName;
    public decimal Price => _sale.Price;
    public string FormattedPrice => Price.FormatCurrency(_settingsService.CurrentSettings);
    public int QuantitySold => _sale.QuantitySold;
    public decimal TotalPrice => _sale.TotalPrice;
    public string FormattedTotalPrice => TotalPrice.FormatCurrency(_settingsService.CurrentSettings);
    public DateTime SaleDate => _sale.SaleDate;
    public string FormattedSaleDate => SaleDate.ToString("g", CultureInfo.CurrentCulture);

    public SaleViewModel(Sale sale, UserSettingsService settingsService)
    {
        _sale = sale;
        _settingsService = settingsService;
    }
}
