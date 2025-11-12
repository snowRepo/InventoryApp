using InventoryApp.Services;

namespace InventoryApp.Helpers
{
    public static class CurrencyFormatter
    {
        public static string FormatCurrency(this decimal amount, UserSettings settings)
        {
            return $"{settings.CurrencySymbol}{amount:N2}";
        }

        public static string FormatCurrency(this decimal? amount, UserSettings settings)
        {
            return amount.HasValue ? $"{settings.CurrencySymbol}{amount.Value:N2}" : "";
        }
    }
}
