namespace CondoSphereMobile.Views;

public partial class CompaniesPage : ContentPage
{
    public CompaniesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as ViewModels.CompaniesViewModel;
        vm?.LoadCompaniesCommand.Execute(null);
    }

    private async void OnBackToHomeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}