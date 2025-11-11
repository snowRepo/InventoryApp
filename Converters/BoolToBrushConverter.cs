using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Data.Converters;

namespace InventoryApp.Converters;

public sealed class BoolToBrushConverter : IValueConverter
{
    // ConverterParameter format: "#ActiveBrush|#InactiveBrush" or "Active|Inactive" named brushes
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string param || string.IsNullOrWhiteSpace(param))
            return AvaloniaProperty.UnsetValue;

        var parts = param.Split('|');
        if (parts.Length != 2)
            return AvaloniaProperty.UnsetValue;

        var isTrue = value is bool b && b;
        var brushStr = isTrue ? parts[0] : parts[1];
        try
        {
            return Brush.Parse(brushStr);
        }
        catch
        {
            return AvaloniaProperty.UnsetValue;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
