using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using InventoryApp.ViewModels;

namespace InventoryApp;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        var vmType = param.GetType();
        var vmFullName = vmType.FullName!;
        var viewFullName = vmFullName
            .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = vmType.Assembly.GetType(viewFullName) ?? Type.GetType(viewFullName);

        if (type != null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = param; // ensure bindings work even without implicit DataTemplate context
            return control;
        }
        
        return new TextBlock { Text = "Not Found: " + viewFullName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
