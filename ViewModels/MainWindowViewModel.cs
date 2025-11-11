namespace InventoryApp.ViewModels;

using CommunityToolkit.Mvvm.Input;
using System;

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
    private void Logout()
    {
        LogoutRequested?.Invoke();
    }

    public MainWindowViewModel()
    {
        _username = string.Empty;
        ShowDashboardCommand = new RelayCommand(ActivateDashboard);
        ShowProductsCommand = new RelayCommand(ActivateProducts);
        ShowSalesCommand = new RelayCommand(ActivateSales);
        ShowExternalSurfacesCommand = new RelayCommand(ActivateExternalSurfaces);
        ShowSettingsCommand = new RelayCommand(ActivateSettings);

        DashboardVM = new DashboardViewModel(_username);
        ProductsVM = new ProductsViewModel();
        SalesVM = new SalesViewModel();
        
        // Use the service locator to resolve ExternalSurfacesViewModel with its dependencies
        ExternalSurfacesVM = App.Resolver.Resolve<ExternalSurfacesViewModel>();
        SettingsVM = new SettingsViewModel();
        
        ActivateDashboard();
    }

    public MainWindowViewModel(string username)
    {
        _username = username;
        ShowDashboardCommand = new RelayCommand(ActivateDashboard);
        ShowProductsCommand = new RelayCommand(ActivateProducts);
        ShowSalesCommand = new RelayCommand(ActivateSales);
        ShowExternalSurfacesCommand = new RelayCommand(ActivateExternalSurfaces);
        ShowSettingsCommand = new RelayCommand(ActivateSettings);

        DashboardVM = new DashboardViewModel(_username);
        ProductsVM = new ProductsViewModel();
        SalesVM = new SalesViewModel();
        
        // Use the service locator to resolve ExternalSurfacesViewModel with its dependencies
        ExternalSurfacesVM = App.Resolver.Resolve<ExternalSurfacesViewModel>();
        SettingsVM = new SettingsViewModel();
        
        ActivateDashboard();
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
