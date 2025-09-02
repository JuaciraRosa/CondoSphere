using CondoSphereMobile.Services;
using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class UnitsPage : ContentPage
{
    public UnitsPage()
    {
        InitializeComponent();

        // Quando a página aparece, disparamos o carregamento do VM:
        Appearing += async (_, __) =>
        {
            if (BindingContext is UnitsViewModel vm)
                await vm.LoadUnitsAsync();
        };
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//DashboardPage");
}