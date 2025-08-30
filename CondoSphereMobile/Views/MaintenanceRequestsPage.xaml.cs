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

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        // Rota absoluta para o Dashboard registrado no AppShell
        await Shell.Current.GoToAsync("///DashboardPage");
    }

}