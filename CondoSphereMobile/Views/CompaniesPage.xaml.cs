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

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        // Rota absoluta para o Dashboard registrado no AppShell
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}