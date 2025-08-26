using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class QuotasPage : ContentPage
{
    public QuotasPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is QuotasViewModel vm &&
            vm.LoadQuotasCommand.CanExecute(null))
        {
            vm.LoadQuotasCommand.Execute(null);
        }
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}
