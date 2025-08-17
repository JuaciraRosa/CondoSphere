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
}