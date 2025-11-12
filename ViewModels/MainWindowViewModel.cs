namespace InventoryApp.ViewModels;

using CommunityToolkit.Mvvm.Input;
using System;
using Avalonia.Controls;
using InventoryApp.Services;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly string _username;
    public string Username => _username;
    // Render concrete VMs directly in the view
    public DashboardViewModel DashboardVM { get; private set; } = null!;
    public ProductsViewModel ProductsVM { get; private set; } = null!;
    public SalesViewModel SalesVM { get; private set; } = null!;
    public ExternalSurfacesViewModel ExternalSurfacesVM { get; private set; } = null!;
    public SettingsViewModel SettingsVM { get; private set; } = null!;

    public bool IsDashboardActive { get; private set; }
    public bool IsProductsActive { get; private set; }
    public bool IsSalesActive { get; private set; }
    public bool IsExternalSurfacesActive { get; private set; }
    public bool IsSettingsActive { get; private set; }

    public IRelayCommand ShowDashboardCommand { get; }
    public IRelayCommand ShowProductsCommand { get; }
    public IRelayCommand ShowSalesCommand { get; }
    public IRelayCommand ShowExternalSurfacesCommand { get; }
    public IRelayCommand ShowSettingsCommand { get; }

    public event Action? LogoutRequested;
    
    [RelayCommand]
    public void Logout()
    {
        LogoutRequested?.Invoke();
    }

    private Window? _window;

    private readonly UserSettingsService _settingsService;

    private readonly IAuthService _authService;

    public MainWindowViewModel() : this(string.Empty) { }

    public MainWindowViewModel(string username, Window? window = null)
    {
        _window = window;
        _username = username;
        
        // Get services
        _settingsService = App.Resolver.Resolve<UserSettingsService>();
        _authService = App.Resolver.Resolve<IAuthService>();
        
        // Initialize commands
        ShowDashboardCommand = new RelayCommand(ActivateDashboard);
        ShowProductsCommand = new RelayCommand(ActivateProducts);
        ShowSalesCommand = new RelayCommand(ActivateSales);
        ShowExternalSurfacesCommand = new RelayCommand(ActivateExternalSurfaces);
        ShowSettingsCommand = new RelayCommand(ActivateSettings);

        // Initialize view models with required services
        DashboardVM = new DashboardViewModel(_username, _settingsService);
        ProductsVM = new ProductsViewModel(_settingsService);
        SalesVM = new SalesViewModel(_settingsService);
        
        // Subscribe to product updates to refresh dashboard
        ProductsVM.ProductsUpdated += (s, e) => DashboardVM.Refresh();
        
        // Use the service locator to resolve ViewModels with their dependencies
        ExternalSurfacesVM = App.Resolver.Resolve<ExternalSurfacesViewModel>();
        SettingsVM = new SettingsViewModel(_window, _settingsService, _username, _authService);
        
        ActivateDashboard();
    }
    
    // Method to set the window reference after construction if needed
    public void SetWindow(Window window)
    {
        _window = window;
        // Reinitialize SettingsViewModel with the window reference and existing services
        if (SettingsVM != null)
        {
            SettingsVM = new SettingsViewModel(_window, _settingsService, _username, _authService);
        }
    }
    
    private void ActivateDashboard()
    {
        IsDashboardActive = true;
        IsProductsActive = false;
        IsSalesActive = false;
        IsExternalSurfacesActive = false;
        IsSettingsActive = false;
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsProductsActive));
        OnPropertyChanged(nameof(IsSalesActive));
        OnPropertyChanged(nameof(IsExternalSurfacesActive));
        OnPropertyChanged(nameof(IsSettingsActive));
        DashboardVM.LogoutRequested -= OnLogoutRequested;
        DashboardVM.LogoutRequested += OnLogoutRequested;
    }

    private void ActivateProducts()
    {
        IsDashboardActive = false;
        IsProductsActive = true;
        IsSalesActive = false;
        IsExternalSurfacesActive = false;
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsProductsActive));
        OnPropertyChanged(nameof(IsSalesActive));
        OnPropertyChanged(nameof(IsExternalSurfacesActive));
    }

    private void ActivateSales()
    {
        IsDashboardActive = false;
        IsProductsActive = false;
        IsSalesActive = true;
        IsExternalSurfacesActive = false;
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsProductsActive));
        OnPropertyChanged(nameof(IsSalesActive));
        OnPropertyChanged(nameof(IsExternalSurfacesActive));
    }

    private void ActivateExternalSurfaces()
    {
        IsDashboardActive = false;
        IsProductsActive = false;
        IsSalesActive = false;
        IsExternalSurfacesActive = true;
        IsSettingsActive = false;
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsProductsActive));
        OnPropertyChanged(nameof(IsSalesActive));
        OnPropertyChanged(nameof(IsExternalSurfacesActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    private void ActivateSettings()
    {
        IsDashboardActive = false;
        IsProductsActive = false;
        IsSalesActive = false;
        IsExternalSurfacesActive = false;
        IsSettingsActive = true;
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsProductsActive));
        OnPropertyChanged(nameof(IsSalesActive));
        OnPropertyChanged(nameof(IsExternalSurfacesActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    private void OnLogoutRequested() => LogoutRequested?.Invoke();
}
