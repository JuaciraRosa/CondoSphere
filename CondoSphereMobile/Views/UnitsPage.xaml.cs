using CondoSphereMobile.Services;
using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class UnitsPage : ContentPage
{
    bool _loadedOnce;

    public UnitsPage()
    {
        InitializeComponent();
      
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loadedOnce) return;
        _loadedOnce = true;

        if (BindingContext is UnitsViewModel vm)
            await vm.LoadUnitsAsync();
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//DashboardPage");
}
