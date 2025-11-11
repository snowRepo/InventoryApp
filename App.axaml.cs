using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using InventoryApp.ViewModels;
using InventoryApp.Views;
using InventoryApp.Data;
using InventoryApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InventoryApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            Resolver.SetServiceProvider(serviceProvider);

            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
                EnsureSchemaUpdated(db);
            }

            var login = new LoginWindow();
            desktop.MainWindow = login;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void EnsureSchemaUpdated(AppDbContext db)
    {
        // Add missing columns to Products if they don't exist (for existing DBs created before changes)
        var conn = db.Database.GetDbConnection();
        conn.Open();
        try
        {
            // Check if Sales table exists
            bool salesTableExists = false;
            using (var checkTable = conn.CreateCommand())
            {
                checkTable.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Sales'";
                using var reader = checkTable.ExecuteReader();
                salesTableExists = reader.Read();
            }

            // Create Sales table if it doesn't exist
            if (!salesTableExists)
            {
                using var createSales = conn.CreateCommand();
                createSales.CommandText = @"
                    CREATE TABLE Sales (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TransactionId TEXT NOT NULL,
                        ProductName TEXT NOT NULL,
                        Price DECIMAL(18,2) NOT NULL,
                        QuantitySold INTEGER NOT NULL,
                        TotalPrice DECIMAL(18,2) NOT NULL,
                        SaleDate TEXT NOT NULL
                    )";
                createSales.ExecuteNonQuery();

                // Create indexes
                using var createIndex1 = conn.CreateCommand();
                createIndex1.CommandText = "CREATE INDEX IX_Sales_TransactionId ON Sales (TransactionId)";
                createIndex1.ExecuteNonQuery();

                using var createIndex2 = conn.CreateCommand();
                createIndex2.CommandText = "CREATE INDEX IX_Sales_SaleDate ON Sales (SaleDate)";
                createIndex2.ExecuteNonQuery();
            }

            // Check Products table columns
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(Products)";
            var hasQuantity = false;
            var hasCategory = false;
            using (var reader = cmd.ExecuteReader())
            {
                var nameOrdinal = -1;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.GetName(i).Equals("name", System.StringComparison.OrdinalIgnoreCase))
                    {
                        nameOrdinal = i;
                        break;
                    }
                }
                while (reader.Read())
                {
                    var colName = reader.GetString(nameOrdinal);
                    if (string.Equals(colName, "Quantity", System.StringComparison.OrdinalIgnoreCase))
                    {
                        hasQuantity = true;
                    }
                    else if (string.Equals(colName, "Category", System.StringComparison.OrdinalIgnoreCase))
                    {
                        hasCategory = true;
                    }
                }
            }

            if (!hasQuantity)
            {
                using var alter = conn.CreateCommand();
                alter.CommandText = "ALTER TABLE Products ADD COLUMN Quantity INTEGER NOT NULL DEFAULT 0";
                alter.ExecuteNonQuery();
            }
            if (!hasCategory)
            {
                using var alter2 = conn.CreateCommand();
                alter2.CommandText = "ALTER TABLE Products ADD COLUMN Category TEXT NOT NULL DEFAULT ''";
                alter2.ExecuteNonQuery();
            }
        }
        finally
        {
            conn.Close();
        }
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Remove the DataAnnotations validator to prevent duplicate validation
        var dataValidationPlugins = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();
        foreach (var plugin in dataValidationPlugins)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IDeviceService, MockDeviceService>();
        
        // Register ViewModels
        services.AddTransient<ExternalSurfacesViewModel>();
        
        // Register other services as needed
    }
    
    // Simple service locator for ViewModel resolution
    public static class Resolver
    {
        private static IServiceProvider _serviceProvider;

        public static void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static T Resolve<T>() where T : class
        {
            return _serviceProvider.GetService<T>();
        }
    }
}