using System;

namespace InventoryApp.Models;

public class Sale
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int QuantitySold { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime SaleDate { get; set; }
}
