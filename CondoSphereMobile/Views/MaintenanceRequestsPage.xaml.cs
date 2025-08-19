using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class MaintenanceRequestsPage : ContentPage
{
    public MaintenanceRequestsPage()
    {
        InitializeComponent();
        Appearing += async (_, __) =>
        {
            if (BindingContext is MaintenanceRequestsViewModel vm)
                await vm.LoadAsync();
        };
    }

    private async void OnBackToDashboard(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }

    private async void OnGoDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }
}