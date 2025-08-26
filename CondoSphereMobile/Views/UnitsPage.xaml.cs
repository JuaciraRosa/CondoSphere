using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class UnitsPage : ContentPage
{
    public UnitsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is UnitsViewModel vm &&
            vm.LoadUnitsCommand.CanExecute(null))
        {
            vm.LoadUnitsCommand.Execute(null);
        }
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}
